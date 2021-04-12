using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Management.Automation;

namespace BatteryPerserve
{
	//Helper Classes:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	[Serializable()]
	public struct Settings_BatteryOptimizer
	{
		//Form information:
		public bool auto_connect;
		public bool start_minimized;
		public bool optimize_schedule;
		public DateTime start_charge_time;
		public DateTime stop_charge_time;
		public decimal battery_range_min;
		public decimal battery_range_max;
		//Device information:
		public string device_name;
		public DeviceStatus device_satus;
		public byte[] device_id;
	}

	public struct EncDec_Resources
	{
		public AesGcm256 gcm256;
		public byte[] aes_key;// = Encoding.UTF8.GetBytes( "89mUHZXCu94IbwUxMdSNiNCGw9OyLyeu" );
		public string[] associated_data;
	}

	//Enums::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	public enum DeviceStatus
	{
		NO_DEVICE = 0,
		SEARCHING,
		FOUND,
		CONNECTED,
		LOST_CONNECTION
	};

	public enum ProgramSettingsCheckbox
	{
		StartProgramBoot = 0,
		AutoConnectStartOptim,
		StartMinimized
	};

	public enum WaitPeriods
	{
		TimeOut = 50000,
		WiFiWait = 10000,
		StatusWait = 2000
	};

	public enum DeviceRelay
	{
		Relay_ON = 0x01,
		Relay_OFF = 0x02
	};


	//BO_Core::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
	public partial class BatteryOptimizer
	{
		//Class Data>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

		//Delegates:
		private delegate void SafeCallDelegate( string percent,
												System.Drawing.Color percentage_fore_colour,
												string charge_status,
												System.Drawing.Color charge_fore_colour ); //UpdatePowerInfo
		private delegate void SafeCallDelegate2( DeviceStatus status ); //UpdateDeviceStatus
		private delegate void SafeCallDelegate3( bool set ); //Set Error Provider, true - set, false - reset

		//Threads:
		private Thread thread_search_for_device = null;

		//Timers:
		private System.Timers.Timer timer_watch_power;
		private System.Timers.Timer timer_watch_connection;

		//Mutex's:
		private Mutex mutex_bo_device;

		//Forms:
		private BO_Form_WiFiProfiles form_wifi_profiles;

		//Packets:
		private BO_Packets packet_manager;

		//Collector:
		private FieldType[] field_types;
		private UInt64 field_types_size = 6;
		private Collector packet_collector;

		//For Encryption/Decryption:
		public EncDec_Resources encdec_resources;

		//Misc & Device:
		private byte relay_status; //State of the relay: 0x01 - Open, 0x02 - Close
		private DeviceStatus device_status;
		private string wifi_profile_sent;
		private bool watch_power;
		private bool allow_search;


		//Battery/Power:
		private string bp_powerline_hold; //The power line status to hold. Incase of unexpected charging/discharging changes
		private PowerStatus bp_power_info;


		//Class Functions>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		public void BO_Constrcutor()
		{
			//UDP
			bo_connection = new BO_Connection();
			BO_Connection_Initialize();
			UDP_SetupReceive();

			//WIFI Profile form:
			form_wifi_profiles = new BO_Form_WiFiProfiles();

			//Packet manager:
			packet_manager = new BO_Packets();

			//Collector:
			field_types = new FieldType[field_types_size];
			field_types[0] = FieldType.BYTE4;
			field_types[1] = FieldType.BYTE1;
			field_types[2] = FieldType.BYTE16;
			field_types[3] = FieldType.BYTE2_VAR_SIZE;
			field_types[4] = FieldType.BYTE4;
			field_types[5] = FieldType.BYTE1_CHECKSUM;
			packet_collector = new Collector( packet_manager.prefix,
												packet_manager.prefix_cs,
												0,
												field_types,
												field_types_size,
												4 );

			//Encryption/Decryption:
			encdec_resources = new EncDec_Resources();
			encdec_resources.gcm256 = new AesGcm256();
			encdec_resources.aes_key = Encoding.UTF8.GetBytes( "89mUHZXCu94IbwUxMdSNiNCGw9OyLyeu" );
			encdec_resources.associated_data = new string[10];
			encdec_resources.associated_data[0] = "RelayPacket";
			encdec_resources.associated_data[1] = "WifiPacket";
			encdec_resources.associated_data[2] = "NamePacket";
			encdec_resources.associated_data[3] = "ResponsePacket";
			encdec_resources.associated_data[4] = "StatusPacket";

			//Device:
			relay_status = 0x01;
			wifi_profile_sent = "";


			//Events/Timers::
			SystemEvents.PowerModeChanged += OnPowerChange;
			//Timers:
			timer_watch_power = new System.Timers.Timer();
			timer_watch_power.Interval = 30000;
			timer_watch_power.Elapsed += WatchPower_Event;
			timer_watch_power.Enabled = true;

			timer_watch_connection = new System.Timers.Timer();
			timer_watch_connection.Interval = 5000;
			timer_watch_connection.Elapsed += WatchConnection_Event;

			//Mutex's:
			mutex_bo_device = new Mutex();


			//Misc::
			watch_power = true;
			allow_search = true;
			//Get Power status:
			CheckPowerStatus();
			//Assume current power line status is what we should hold:
			bp_powerline_hold = bp_power_info.PowerLineStatus.ToString();


			//Check if first time ever running program:
			RegistryKey key1 = Registry.CurrentUser.OpenSubKey( "SOFTWARE\\BatteryOptimizer" );
			if (key1 == null /*|| key1.GetValue("FirstEverRun") == null*/)
				InitializeRegistry();
			else
				key1.Close();

			//Check Settings in Registry:
			InitialRegistryCheck();

		} //END BO_Constrcutor


		//Registry functions>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		private void InitialRegistryCheck()
		{
			//Start Program at Boot:
			RegistryKey key1 = Registry.CurrentUser.OpenSubKey( "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run" );
			if (key1.GetValue( "BatteryOptimizer" ) != null)
				Program_Settings.SetItemChecked( 0, true );
			key1.Close();

			//Rest of Settings:
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			if (null == BatOpSettings.device_id || 0 == BatOpSettings.battery_range_max)
			{
				InitializeRegistry();
				BatOpSettings = RetrieveSettings();
			}

			//Auto Connect & Start Optimizing:
			if (BatOpSettings.auto_connect == true)
			{
				Program_Settings.SetItemChecked( 1, true );

				button_search_for_device_Click();
				button_OptmizeBattery_Click();
			}

			//Start Minimized:
			if (BatOpSettings.start_minimized == true)
			{
				Program_Settings.SetItemChecked( 2, true );
				this.WindowState = FormWindowState.Minimized;
			}

			//Optimize Charge Schedule:
			if (BatOpSettings.optimize_schedule == true)
				checkBox_OptimizeChargeTime.Checked = true;

			//Charge Start Stop times:
			Battery_OptimizeChargeTime.Value = BatOpSettings.start_charge_time;
			Battery_NormalChargeTime.Value = BatOpSettings.stop_charge_time;

			//Optimal Battery Range:
			BatteryMin.Value = BatOpSettings.battery_range_min;
			BatteryMax.Value = BatOpSettings.battery_range_max;

			//Device:
			UpdateDeviceStatus( BatOpSettings.device_satus );

		} //END InitialRegistryCheck


		private void InitializeRegistry()
		{
			Settings_BatteryOptimizer InitialSettings = new Settings_BatteryOptimizer
			{
				auto_connect = false,
				start_minimized = false,
				optimize_schedule = false,
				start_charge_time = Battery_OptimizeChargeTime.Value,
				stop_charge_time = Battery_NormalChargeTime.Value,
				battery_range_min = BatteryMin.Value,
				battery_range_max = BatteryMax.Value,
				device_name = "",
				device_satus = DeviceStatus.NO_DEVICE,
				device_id = encdec_resources.gcm256.NewIv()
			};

			RegistryKey key1 = Registry.CurrentUser.CreateSubKey( "SOFTWARE\\BatteryOptimizer" );
			key1.Close();
			SaveSettings( InitialSettings );

		} //END InitializeRegistry


		private void SaveSettings( Settings_BatteryOptimizer BatOpSettings )
		{
			string s_InitialSettings;
			using (var stringwriter = new System.IO.StringWriter())
			{
				var serializer = new XmlSerializer( BatOpSettings.GetType() );
				serializer.Serialize( stringwriter, BatOpSettings );
				s_InitialSettings = stringwriter.ToString();
			};

			RegistryKey key1 = Registry.CurrentUser.OpenSubKey( "SOFTWARE\\BatteryOptimizer", true );
			key1.SetValue( "Settings", s_InitialSettings, RegistryValueKind.String );
			key1.Close();
		} //END SaveSettings


		private Settings_BatteryOptimizer RetrieveSettings()
		{
			RegistryKey key2 = Registry.CurrentUser.OpenSubKey( "SOFTWARE\\BatteryOptimizer" );
			string s_BatOpSettings = (string)key2.GetValue( "Settings" );

			using (var stringReader = new System.IO.StringReader( s_BatOpSettings ))
			{
				var serializer = new XmlSerializer( typeof( Settings_BatteryOptimizer ) );
				return (Settings_BatteryOptimizer)serializer.Deserialize( stringReader );
			};

		} //END RetrieveSettings


		private void AddToStartup()
		{
			//Add
			RegistryKey key = Registry.CurrentUser.OpenSubKey( "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true );
			key.SetValue( "BatteryOptimizer", @"" + Directory.GetCurrentDirectory() + "\\BatteryPerserve.exe" ); //get current folder
			key.Close();
		} //END AddToStartup


		private void RemoveFromStartup()
		{
			//Remove
			RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey( "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true );
			key.DeleteValue( "BatteryOptimizer", false );
			key.Close();
		} //END RemoveFromStartup




		//Main Functions>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		private void WatchConnection_Event( Object source, System.Timers.ElapsedEventArgs e )
		{
			CheckConnection();
		} //END WatchConnection_Event


		private void CheckConnection()
		{
			mutex_bo_device.WaitOne();

			UDP_SendToClient( packet_manager.msg_type_status );

			Thread.Sleep( (int)WaitPeriods.StatusWait );

			if (DeviceStatus.SEARCHING != device_status &&
				bo_connection.received_response == false) //Didn't receive response packet, lost connection.

			{
				UpdateDeviceStatus( DeviceStatus.LOST_CONNECTION );
			}

			//Check power status well were at it:
			CheckPowerStatus();

			mutex_bo_device.ReleaseMutex();

		} //END CheckConnection


		private void SearchForDevice( Object stateinfo )
		{
			if (true == allow_search)
			{
				UInt32 timer = 0;
				DeviceStatus temp_status = device_status;

				SetErrorProvider_DeviceStatus( false );

				//Initial Connection, Send WiFi info:
				while (	true == allow_search &&
						DeviceStatus.NO_DEVICE == temp_status &&
						timer <= (UInt32)WaitPeriods.TimeOut)
				{
					UpdateDeviceStatus( DeviceStatus.SEARCHING );

					UDP_SendToClient( packet_manager.msg_type_wifi );
					Thread.Sleep( (int)WaitPeriods.WiFiWait );
					temp_status = device_status;

					timer += (UInt32)WaitPeriods.WiFiWait;
				}

				timer = 0;
				temp_status = device_status;

				//Establish connection with device:
				while (	true == allow_search &&
						DeviceStatus.CONNECTED != temp_status &&
						timer <= (UInt32)WaitPeriods.TimeOut)
				{
					UpdateDeviceStatus( DeviceStatus.SEARCHING );
					CheckConnection();
					temp_status = device_status;

					timer += (UInt32)WaitPeriods.StatusWait;
				}

				//Set flags:
				Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();

				if (DeviceStatus.CONNECTED != device_status)
				{
					if (BatOpSettings.auto_connect == true)
						SetErrorProvider_DeviceStatus( true );

					UpdateDeviceStatus( DeviceStatus.LOST_CONNECTION );
				}
				else if (DeviceStatus.CONNECTED == device_status)
				{
					timer_watch_connection.Enabled = true;
				}
			}

		} //END SearchForDevice


		private void UpdateDeviceStatus( DeviceStatus status )
		{
			if (textBox_device_status.InvokeRequired)
			{
				var d = new SafeCallDelegate2( UpdateDeviceStatus );
				Invoke( d, new object[] { status } );
			}
			else
			{
				Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
				device_status = status;

				if (DeviceStatus.NO_DEVICE == status)
				{
					textBox_device_status.Text = "No Device";
					//Save Status:
					BatOpSettings.device_satus = status;
					SaveSettings( BatOpSettings );
					button_search_for_device.Enabled = true;
					button_search_for_device.Text = "Search For Device";
				}
				else if (DeviceStatus.SEARCHING == status)
				{
					textBox_device_status.Text = "Searching";
					//button_search_for_device.Enabled = false;
					button_search_for_device.Text = "Cancel Search";
				}
				else if (DeviceStatus.FOUND == status)
				{
					textBox_device_status.Text = "Device info Stored";
					//Save Status:
					BatOpSettings.device_satus = status;
					SaveSettings( BatOpSettings );
					if (null != wifi_profile_sent && "" != wifi_profile_sent)
					{
						button_search_for_device.Enabled = false;
						PowerShell ps_connect_wifi = PowerShell.Create();
						ps_connect_wifi.AddScript( "netsh wlan connect " + wifi_profile_sent );
						var results = ps_connect_wifi.Invoke();

						wifi_profile_sent = "";

					}
					else
					{
						button_search_for_device.Enabled = true;
						button_search_for_device.Text = "Connect to Device";
					}
				}
				else if (DeviceStatus.CONNECTED == status)
				{
					textBox_device_status.Text = "Connected";
					button_search_for_device.Enabled = false;
					button_search_for_device.Text = "Disabled";
				}
				else if (DeviceStatus.LOST_CONNECTION == status)
				{
					textBox_device_status.Text = "Lost Connection";
					button_search_for_device.Enabled = true;
					button_search_for_device.Text = "Connect to Device";
					bo_connection.latest_packet_sent = "";
					//Auto Connect & Start Optimizing
					if (BatOpSettings.auto_connect == true)
					{
						button_search_for_device_Click();
					}

				}


			}


		} //END UpdateDeviceStatus


		private void SetErrorProvider_DeviceStatus( bool set )
		{
			if (textBox_device_status.InvokeRequired)
			{
				var d = new SafeCallDelegate3( SetErrorProvider_DeviceStatus );
				Invoke( d, new object[] { set } );
			}
			else
			{
				if (true == set)
					errorProvider_device_status.SetError( textBox_device_status, "Could not auto connect!" );
				else
					errorProvider_device_status.SetError( textBox_device_status, String.Empty );
			}
		} //END SetErrorProvider_DeviceStatus




		//Power Functions>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
		private void UpdatePowerStatus( string percent,
										System.Drawing.Color percentage_fore_colour,
										string charge_status,
										System.Drawing.Color charge_fore_colour )
		{
			if (Battery_Percentage.InvokeRequired || Battery_LineStatus.InvokeRequired)
			{
				var d = new SafeCallDelegate( UpdatePowerStatus );
				Invoke( d, new object[] {	percent,
											percentage_fore_colour,
											charge_status,
											charge_fore_colour } );
			}
			else
			{
				Battery_Percentage.Text = percent;
				Battery_Percentage.ForeColor = percentage_fore_colour;
				Battery_LineStatus.Text = charge_status;
				Battery_LineStatus.ForeColor = charge_fore_colour;
			}

		} //END UpdatePowerStatus


		private void CheckPowerStatus()
		{
			//Update Battery Info
			bp_power_info = SystemInformation.PowerStatus; //Get Power Status
			string percent, charge_status;
			System.Drawing.Color percentage_fore_colour, charge_fore_colour;

			if (bp_power_info.PowerLineStatus.ToString() == "Online")
			{
				charge_status = "Charging";
				charge_fore_colour = System.Drawing.Color.Lime;
				//relay_status = (byte)DeviceRelay.Relay_ON;
			}
			else
			{
				charge_status = "Dis-Charging";
				charge_fore_colour = System.Drawing.Color.Yellow;
				//relay_status = (byte)DeviceRelay.Relay_OFF;
			}

			percent = (bp_power_info.BatteryLifePercent * 100).ToString() + "%";

			if (bp_power_info.BatteryLifePercent <= 0.15)
			{
				percentage_fore_colour = System.Drawing.Color.Red;
			}
			else if (bp_power_info.BatteryLifePercent <= 0.30)
			{
				percentage_fore_colour = System.Drawing.Color.Orange;
			}
			else
			{
				percentage_fore_colour = System.Drawing.Color.Lime;
			}

			UpdatePowerStatus(	percent,
								percentage_fore_colour,
								charge_status,
								charge_fore_colour);

		} //END CheckPowerStatus


		private void WatchPower_Event( Object source, System.Timers.ElapsedEventArgs e )
		{
			if (watch_power == true)
				CheckPower();

		} //END PowerWatch_Event


		private bool CheckOptimizationSchedule()
		{
			//Check if Optimize checkbox is checked and also if it is time to always stay charged
			if (checkBox_OptimizeChargeTime.Checked == false ||

				(Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay &&
				Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay) ||

				(Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay &&
				Battery_NormalChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay &&
				Battery_OptimizeChargeTime.Value.TimeOfDay >= Battery_NormalChargeTime.Value.TimeOfDay) ||

				(Battery_OptimizeChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay &&
				Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay &&
				Battery_OptimizeChargeTime.Value.TimeOfDay >= Battery_NormalChargeTime.Value.TimeOfDay))
				return true;
			else
				return false;
		} //END CheckOptimizationSchedule


		private void CheckPower()
		{
			//Check power status before:
			CheckPowerStatus();

			//byte temp_relay_status = relay_status;
			string temp_power_line_status;

			//Check if able to optimize battery &&
			//Check if Optimize checkbox is checked and also if it is time to always stay charged
			if (device_status == DeviceStatus.CONNECTED &&
				button_OptmizeBattery.Text == "ON" &&
				CheckOptimizationSchedule() == true)
			{
				temp_power_line_status = bp_power_info.PowerLineStatus.ToString();

				//Stop charging:
				if (//Trigger a discharge:
					(bp_power_info.BatteryLifePercent >= (float)BatteryMax.Value / 100 &&
					temp_power_line_status == "Online") ||
					//Hold the discharge as long as we are greater than min:
					(bp_power_info.BatteryLifePercent >= (float)BatteryMin.Value / 100 &&
					bp_powerline_hold == "Offline"))
				{
					relay_status = (byte)DeviceRelay.Relay_OFF;
					bp_powerline_hold = "Offline";
				}
				//Start charging:
				else if (	//Trigger a charge:
							(bp_power_info.BatteryLifePercent <= (float)BatteryMin.Value / 100 &&
							temp_power_line_status == "Offline") ||
							//Hold the charge as long as we are less than max:
							(bp_power_info.BatteryLifePercent <= (float)BatteryMax.Value &&
							bp_powerline_hold == "Online"))
				{
					relay_status = (byte)DeviceRelay.Relay_ON;
					bp_powerline_hold = "Online";
				}


				//if (relay_status != temp_relay_status)
				//{
				mutex_bo_device.WaitOne();

				//Send out relay status:
				UDP_SendToClient( packet_manager.msg_type_relay );

				//Give some time for response packet to be received:
				Thread.Sleep( (int)WaitPeriods.StatusWait );

				if (bo_connection.received_response == false) //Didn't receive response packet, lost connection.
				{
					UpdateDeviceStatus( DeviceStatus.LOST_CONNECTION );
				}

				//Check power status after:
				CheckPowerStatus();

				mutex_bo_device.ReleaseMutex();

				//} //END relay status changed
			}
		} //END CheckPower


	} //END Class
}
