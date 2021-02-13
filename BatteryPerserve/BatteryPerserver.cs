using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Management.Automation;


namespace BatteryPerserve
{
	public partial class BatteryOptimizer : Form
	{
		//Class Data ---------------------------------------------------------------------

		//Delegates:
		private delegate void SafeCallDelegate(string text, string text2); //UpdatePowerInfo
		private delegate void SafeCallDelegate2( DeviceStatus status); //UpdateDeviceStatus
		private delegate void SafeCallDelegate3( bool set ); //Set Error Provider, true - set, false - reset

		//Threads:
		//private Thread thread_watch_power = null;
		private Thread thread_search_for_device = null;

		//Timers:
		private System.Timers.Timer timer_watch_power;

		//Forms:
		private WifiProfiles form_wifi_profiles;

		//Packets:
		private BP_Packets packet_manager;

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
		private PowerStatus bp_power_info;



		//Class Functions ---------------------------------------------------------------------
		public BatteryOptimizer()
		{
			InitializeComponent();

			//UDP
			UDP_Initialize_Client();
			UDP_SetupReceive();

			//WIFI Profile form:
			form_wifi_profiles = new WifiProfiles();

			//Packet manager:
			packet_manager = new BP_Packets();

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


			//Events/Timers:
			SystemEvents.PowerModeChanged += OnPowerChange;
			//Timers:
			timer_watch_power = new System.Timers.Timer();
			timer_watch_power.Interval = 30000;
			timer_watch_power.Elapsed += PowerWatch_Event;
			timer_watch_power.Enabled = true;

			//Misc:
			watch_power = true;
			allow_search = true;


			//Check if first time ever running program:
			RegistryKey key1 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer");
			if (key1 == null /*|| key1.GetValue("FirstEverRun") == null*/)
				InitializeRegistry();
			else
				key1.Close();

			//Check Settings in Registry:
			InitialRegistryCheck();

		} //END Constructor


		//Registry functions::::::::::::::::::::::::::::::::::::::
		private void InitialRegistryCheck()
		{
			//Start Program at Boot
			RegistryKey key1 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
			if (key1.GetValue("BatteryOptimizer") != null)
				Program_Settings.SetItemChecked(0, true);
			key1.Close();

			//Rest of Settings
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			if (BatOpSettings.battery_range_max == 0)
			{
				InitializeRegistry();
				BatOpSettings = RetrieveSettings();
			}


			//Auto Connect & Start Optimizing
			if (BatOpSettings.auto_connect == true)
			{
				Program_Settings.SetItemChecked( 1, true );

				button_search_for_device_Click();
				button_OptmizeBattery_Click();
			}


			//Start Minimized
			if (BatOpSettings.start_minimized == true)
			{
				Program_Settings.SetItemChecked(2, true);
				this.WindowState = FormWindowState.Minimized;
			}

			//Optimize Charge Schedule
			if (BatOpSettings.optimize_schedule == true)
				checkBox_OptimizeChargeTime.Checked = true;

			//Charge Start Stop times
			Battery_OptimizeChargeTime.Value = BatOpSettings.start_charge_time;
			Battery_NormalChargeTime.Value = BatOpSettings.stop_charge_time;

			//Optimal Battery Range
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
				device_satus = DeviceStatus.NO_DEVICE
			};

			RegistryKey key1 = Registry.CurrentUser.CreateSubKey("SOFTWARE\\BatteryOptimizer");
			key1.Close();
			SaveSettings(InitialSettings);

		} //END InitializeRegistry


		private void SaveSettings(Settings_BatteryOptimizer BatOpSettings)
		{
			string s_InitialSettings;
			using (var stringwriter = new System.IO.StringWriter())
			{
				var serializer = new XmlSerializer(BatOpSettings.GetType());
				serializer.Serialize(stringwriter, BatOpSettings);
				s_InitialSettings = stringwriter.ToString();
			};

			RegistryKey key1 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
			key1.SetValue("Settings", s_InitialSettings, RegistryValueKind.String);
			key1.Close();
		} //END SaveSettings


		private Settings_BatteryOptimizer RetrieveSettings()
		{
			RegistryKey key2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer");
			string s_BatOpSettings = (string)key2.GetValue("Settings");

			using (var stringReader = new System.IO.StringReader(s_BatOpSettings))
			{
				var serializer = new XmlSerializer(typeof(Settings_BatteryOptimizer));
				return (Settings_BatteryOptimizer)serializer.Deserialize(stringReader);
			};

		} //END RetrieveSettings


		//Main Functions::::::::::::::::::::::::::::::::::::::
		private void SearchForDevice(Object stateinfo)
		{
			if (true == allow_search)
			{
				SetErrorProvider_DeviceStatus( false );
				Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
				DeviceStatus temp_status = BatOpSettings.device_satus;
				UpdateDeviceStatus( DeviceStatus.SEARCHING );
				UInt32 timer = 0;

				while (true == allow_search &&
						DeviceStatus.NO_DEVICE == temp_status &&
						timer <= 50000)
				{
					UDP_SendToClient( packet_manager.msg_type_wifi );
					Thread.Sleep( 10000 );
					timer += 10000;
					if (DeviceStatus.SEARCHING != device_status)
					{
						temp_status = device_status;
					}
				}

				if (true == allow_search)
				{
					timer = 0;
					BatOpSettings = RetrieveSettings();
					temp_status = BatOpSettings.device_satus;
					UpdateDeviceStatus( DeviceStatus.SEARCHING );

					while (DeviceStatus.FOUND == temp_status && timer <= 50000)
					{
						UDP_SendToClient( packet_manager.msg_type_status );
						Thread.Sleep( 10000 );
						timer += 10000;
						if (DeviceStatus.SEARCHING != device_status)
						{
							temp_status = device_status;
						}
					}

					if (DeviceStatus.CONNECTED != device_status)
					{
						UpdateDeviceStatus( temp_status );
						if (BatOpSettings.auto_connect == true)
							SetErrorProvider_DeviceStatus( true );
					}
				}
			}

		} //END Find_Device

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
					button_search_for_device.Enabled = false;
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
				}
				else if (DeviceStatus.LOST_CONNECTION == status)
				{
					textBox_device_status.Text = "Lost Connection";
					button_search_for_device.Enabled = true;
					button_search_for_device.Text = "Connect to Device";
					latest_packet_sent = "";
					//Auto Connect & Start Optimizing
					if (BatOpSettings.auto_connect == true)
					{
						button_search_for_device_Click();
					}

				}


			}


		} //END UpdateDeviceStatus


		//Power Functions:::::::::::::::::::::::::::::::::::::
		private void OnPowerChange(object s, PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case PowerModes.Resume:
					Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
					watch_power = true;
					allow_search = true;
					//Auto Connect & Start Optimizing
					if (BatOpSettings.auto_connect == true)
					{
						button_search_for_device_Click();
					}
					break;
				case PowerModes.Suspend:
					watch_power = false;
					allow_search = false;
					if (DeviceStatus.CONNECTED == device_status)
						UpdateDeviceStatus( DeviceStatus.LOST_CONNECTION );
					break;

			}
		} //END OnPowerChange


		private void UpdatePowerInfo(string percent, string charge_status)
		{
			if (Battery_Percentage.InvokeRequired || Battery_LineStatus.InvokeRequired)
			{
				var d = new SafeCallDelegate(UpdatePowerInfo);
				Invoke(d, new object[] { percent, charge_status });
			}
			else
			{
				Battery_Percentage.Text = percent;
				Battery_LineStatus.Text = charge_status;
			}

		} //END UpdatePowerInfo


		private bool CheckOptimizationSchedule()
		{
			//Check if Optimize checkbox is checked and also if it is time to always stay charged
			if (	checkBox_OptimizeChargeTime.Checked == false ||

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


		private void PowerWatch_Event( Object source, System.Timers.ElapsedEventArgs e )
		{
			if (true == watch_power)
			{
				PowerWatch();
			}

		} //END PowerWatch_Event


		private void PowerWatch()
		{
			//Update Battery Info
			bp_power_info = SystemInformation.PowerStatus; //Get Power Status

			if (bp_power_info.PowerLineStatus.ToString() == "Online")
				UpdatePowerInfo((bp_power_info.BatteryLifePercent * 100).ToString() + "%", "Charging");
			else
				UpdatePowerInfo((bp_power_info.BatteryLifePercent * 100).ToString() + "%", "Dis-Charging");


			if (device_status == DeviceStatus.CONNECTED)
			{
				//Check if able to optimize battery
				if (button_OptmizeBattery.Text == "ON")
				{
					//Check if Optimize checkbox is checked and also if it is time to always stay charged
					if (CheckOptimizationSchedule())
					{
						if (bp_power_info.BatteryLifePercent >= (float)BatteryMax.Value / 100)
						{ //combine if stmt's
							if (bp_power_info.PowerLineStatus.ToString() == "Online") //Stop charging
							{
								relay_status = 0x02;
							}

						}
						else if (bp_power_info.BatteryLifePercent <= (float)BatteryMin.Value / 100) //Start charging
						{
							if (bp_power_info.PowerLineStatus.ToString() == "Offline")
							{
								relay_status = 0x01;
							}

						}

					} //END Check Charge times


					UDP_SendToClient( packet_manager.msg_type_relay );
				} //END Checking Optimizaion active
				//else
				//{
				//	//Turn relay back on
				//	relay_status = 0x01;
				//	UDP_SendToClient( packet_manager.msg_type_relay );
				//}

				Thread.Sleep( 1000 ); //Give some time for response packet to be received

				if (latest_packet_sent != "") //Didn't receive response packet, lost connection
				{
					UpdateDeviceStatus( DeviceStatus.LOST_CONNECTION );
				}

			}

		} //END PowerWatch


		//Form Functions::::::::::::::::::::::::::::::::::::
		private void checkBox_OptimizeChargeTime_CheckedChanged(object sender, EventArgs e)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();

			if (Battery_OptimizeChargeTime.Enabled == false)
			{
				Battery_OptimizeChargeTime.Enabled = true;
				Battery_NormalChargeTime.Enabled = true;
				BatOpSettings.optimize_schedule = true;
			}
			else //true
			{
				Battery_OptimizeChargeTime.Enabled = false;
				Battery_NormalChargeTime.Enabled = false;
				BatOpSettings.optimize_schedule = false;
			}

			SaveSettings(BatOpSettings);
		} //END CheckBox


		private void button_DefaultBatteryRange_Click(object sender, EventArgs e)
		{
			BatteryMin.Value = 40;
			BatteryMax.Value = 60;
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			BatOpSettings.battery_range_min = BatteryMin.Value;
			BatOpSettings.battery_range_max = BatteryMax.Value;
			SaveSettings(BatOpSettings);

		} //END Defaults


		private void button_OptmizeBattery_Click(object sender, EventArgs e)
		{
			button_OptmizeBattery_Click();
		} //END Optimize Battery


		private void button_OptmizeBattery_Click() //For Simulating click
		{
			if (button_OptmizeBattery.Text == "OFF")
			{
				button_OptmizeBattery.Text = "ON";
				button_OptmizeBattery.BackColor = System.Drawing.Color.Lime;
			}
			else
			{
				button_OptmizeBattery.Text = "OFF";
				button_OptmizeBattery.BackColor = System.Drawing.Color.Red;
				//Turn relay back on
				relay_status = 0x01;
				UDP_SendToClient( packet_manager.msg_type_relay );
			}
		} //END Optimize Battery


		private void AddToStartup()
		{
			//Add
			RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			key.SetValue("BatteryOptimizer", @"" + Directory.GetCurrentDirectory() + "\\BatteryPerserve.exe"); //get current folder
			key.Close();
		} //END AddToStartup


		private void RemoveFromStartup()
		{
			//Remove
			RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
			key.DeleteValue("BatteryOptimizer", false);
			key.Close();
		} //END RemoveFromStartup


		private void Program_Settings_ItemCheck(object sender, ItemCheckEventArgs e)
		{

			if (Program_Settings.SelectedIndex == 0) //Start program at boot
			{
				if (Program_Settings.CheckedIndices.Contains(0) == false) //Not checked, being checked
					AddToStartup();
				else //Checked, being removed
					RemoveFromStartup();
			}
			else if (Program_Settings.SelectedIndex == 1) //Auto Connect & Start Optimizing
			{
				Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();

				if (Program_Settings.CheckedIndices.Contains(1) == false) //Not checked, being checked
				{
					BatOpSettings.auto_connect = true;
					//Watch_OpenCheck = true;

				}
				else //Checked, being removed
				{
					BatOpSettings.auto_connect = false;
					//Watch_OpenCheck = false;

				}

				SaveSettings(BatOpSettings);

			}
			else if (Program_Settings.SelectedIndex == 2)
			{
				Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();

				if (Program_Settings.CheckedIndices.Contains(2) == false) //Not checked, being checked
					BatOpSettings.start_minimized = true;
				else //Checked, being removed
					BatOpSettings.start_minimized = false;

				SaveSettings(BatOpSettings);
			}

		} //END Program_Settings_ItemCheck


		private void Battery_OptimizeChargeTime_ValueChanged(object sender, EventArgs e)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			BatOpSettings.start_charge_time = Battery_OptimizeChargeTime.Value;
			SaveSettings(BatOpSettings);
		} //END Battery_OptimizeChargeTime_ValueChanged


		private void Battery_NormalChargeTime_ValueChanged(object sender, EventArgs e)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			BatOpSettings.stop_charge_time = Battery_NormalChargeTime.Value;
			SaveSettings(BatOpSettings);
		} //END Battery_NormalChargeTime_ValueChanged


		private void BatteryMin_ValueChanged(object sender, EventArgs e)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			BatOpSettings.battery_range_min = BatteryMin.Value;
			SaveSettings(BatOpSettings);
		} //END BatteryMin_ValueChanged


		private void BatteryMax_ValueChanged(object sender, EventArgs e)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			BatOpSettings.battery_range_max = BatteryMax.Value;
			SaveSettings(BatOpSettings);
		} //END BatteryMax_ValueChanged


		private void BatteryOptimizer_Resize(object sender, EventArgs e)
		{
			if (this.WindowState == FormWindowState.Minimized)
			{
				Hide();
				notifyIcon1.Visible = true;
			}
		} //END BatteryOptimizer_Resize


		private void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			Show();
			this.WindowState = FormWindowState.Normal;
			notifyIcon1.Visible = false;
		} //END notifyIcon1_DoubleClick


		private void button_search_for_device_Click( object sender, EventArgs e )
		{
			button_search_for_device_Click();
		} //END button_search_for_device_Click

		private void button_search_for_device_Click() //For Simulating click
		{
			if (DeviceStatus.CONNECTED != device_status)
			{
				allow_search = true;
				ThreadPool.QueueUserWorkItem( SearchForDevice );
			}

		} //END button_search_for_device_Click


		private void button_restore_no_device_Click( object sender, EventArgs e )
		{
			UpdateDeviceStatus( DeviceStatus.NO_DEVICE );
			allow_search = false;
		} //END button_restore_no_device_Click


		private void BatteryOptimizer_FormClosed( object sender, FormClosedEventArgs e )
		{
			//Set Thread bools to false:
			watch_power = false;

			if (null != thread_search_for_device && true == thread_search_for_device.IsAlive)
			{
				thread_search_for_device.Join();
			}


		} //END BatteryOptimizer_FormClosed


	} //END Class
} //END Namespace
