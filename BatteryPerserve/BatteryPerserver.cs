using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
		private delegate void SafeCallDelegate2(string text, bool warning); //OpenPort

		//Threads:
		private Thread bp_watch_power_thread;
		//private Thread bp_find_device_thread;
		private Thread bp_tcp_listening;
		private Thread bp_tcp_talker_thread;

		//Relay:
		private byte bp_relay; //State of the relay: 0 - Open, 1 - Close
		private bool bp_send_relay;


		//WIFI Profile form:	
		private WifiProfiles bp_profiles_form;

		//private UdpClient bp_udp_server;
		//private IPEndPoint bp_end_point;


		//TCP Server:
		public static ManualResetEvent tcpClientConnected;
		private TcpListener bp_tcp_server;		
		private TcpClient bp_client_device;
		private IPAddress bp_local_ip;
		private const int bp_listen_port = 8000;
		private bool bp_listen_for_tcp;
		private bool bp_have_client;

		//AES:
		private string bp_aes_key_str;
		private byte[] bp_aes_key;
		private string bp_aes_iv_str;
		private byte[] bp_aes_iv;


		//Battery/Power:
		private PowerStatus bp_power_info;

		private bool bp_watch_power; //Pwr_Watching Thread
		private bool bp_power_control;

		//Class Functions ----------------------------------------------------------------
		public BatteryOptimizer()
		{
			InitializeComponent();
			SystemEvents.PowerModeChanged += OnPowerChange;
			//Check if first time ever running program
			RegistryKey key1 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer");
			if (key1 == null /*|| key1.GetValue("FirstEverRun") == null*/) 
				InitializeRegistry();
			else
				key1.Close();

			InitialRegistryCheck(); //Check Settings in Registry

			//Start UDP server to listen in for the Battery Opetimizer device:     
			//bp_udp_server = new UdpClient(bp_listen_port);
			//bp_end_point = new IPEndPoint(IPAddress.Any, bp_listen_port);
			//bp_search_device = true;

			//TCP:
			tcpClientConnected = new ManualResetEvent( false );
			bp_local_ip = IPAddress.Any;
			//bp_local_ip = IPAddress.Parse( "127.0.0.1" );//new IPAddress( Encoding.UTF8.GetBytes( "127.0.0.1" ) );
			bp_tcp_server = new TcpListener( bp_local_ip, bp_listen_port );
			bp_tcp_server.Start();


			//Set AES:
			bp_aes_key_str = "qbEUMjP4qiMhLmGJ";
			bp_aes_key = Encoding.UTF8.GetBytes( bp_aes_key_str );
			bp_aes_iv_str = "BvgETwTVstHNzsfY";
			bp_aes_iv = Encoding.UTF8.GetBytes( bp_aes_iv_str );

			//WIFI Profile form:
			//bp_profiles_form = new WifiProfiles();


			//Relay:
			bp_send_relay = false;


			//Thread Starts:
			bp_listen_for_tcp = true;
			bp_tcp_listening = new Thread( new ThreadStart( StartTCPListening ) );
			bp_tcp_listening.Start();

			//Find device:
			//bp_find_device_thread = new Thread( Find_Device );
			//bp_find_device_thread.Start();
			//Find_Device();


			//bp_power_control = true;
			//bp_watch_power = true;
			//bp_watch_power_thread = new Thread(PowerWatch);
			//bp_watch_power_thread.Start();
		} //END Constructor

		
		//Registry functions
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
				Program_Settings.SetItemChecked(1, true);

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

		} //END InitialRegistryCheck

		private void InitializeRegistry()
		{
			Settings_BatteryOptimizer InitialSettings = new Settings_BatteryOptimizer
			{
				auto_connect = false,
				start_minimized = false,
				//LastCom = "",
				optimize_schedule = false,
				start_charge_time = Battery_OptimizeChargeTime.Value,
				stop_charge_time = Battery_NormalChargeTime.Value,
				battery_range_min = BatteryMin.Value,
				battery_range_max = BatteryMax.Value
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


		//TCP functions
		private void StartTCPListening()
		{
			while (true == bp_listen_for_tcp)
			{
				if (false == bp_have_client)
				{
					tcpClientConnected.Reset();

					bp_tcp_server.BeginAcceptTcpClient( new AsyncCallback( FoundTCPCLient ), bp_tcp_server );

					tcpClientConnected.WaitOne();
				}
			}
			
		} //END StartTCPListening


		private void FoundTCPCLient( IAsyncResult ar )
		{
			bp_have_client = true;

			//Collect client:
			TcpListener listener = (TcpListener)ar.AsyncState;

			if (null != bp_tcp_listening)
			{
				bp_client_device = listener.EndAcceptTcpClient( ar );

				//Start Talker Thread:
				bp_tcp_talker_thread = new Thread( new ThreadStart( TCPTalker ) );
				bp_tcp_talker_thread.Start();
			}

			tcpClientConnected.Set();
		} //END FoundTCPCLient


		private void TCPTalker()
		{
			bp_send_relay = true;
			while (true == bp_client_device.Connected && true == bp_have_client)
			{
				if (bp_client_device.Available > 0)
				{
					NetworkStream stream = bp_client_device.GetStream();
					byte[] received = new byte[200];

					stream.Read( received, 0, received.Length );
					//See if device needs a WIFI profile, if so then send wifi profile

				}


				//Send Device info to open/close:
				if (true == bp_send_relay)
				{
					//NetworkStream stream = bp_client_device.GetStream();
					//BP_Relay_Packet relay_packet = new BP_Relay_Packet();
					//byte[] packet2 = relay_packet.extract( bp_relay, bp_aes_key, bp_aes_iv );


					//int ps2_size = packet2[1] << 4 | packet2[2];
					//byte[] encrypted_data = new byte[ps2_size];
					//for (int x = 0, y = 3; y < packet2.Length; x++, y++)
					//	encrypted_data[x] = packet2[y];

					BP_Packets packet_manager = new BP_Packets();

					byte[] relay_packet = packet_manager.BuildRelayPacket( 0x01 );



					//BP_Helper helper = new BP_Helper();
					//AesGcm256 gcm256 = new AesGcm256();
					//byte[] to_encrypt;
					//string test_data = "AES test data 1234 test. This is long data string test of a message with a higher byte size.. This is the end of the message1234";
					//to_encrypt = Encoding.ASCII.GetBytes( test_data );

					//byte[] encrypted_data = gcm256.Encrypt( to_encrypt, bp_aes_key, bp_aes_iv, null );


					//byte[] decrypted_data = gcm256.Decrypt( encrypted_data, bp_aes_key, bp_aes_iv, null );

					//string str_dec_data = Encoding.UTF8.GetString( decrypted_data );


					//stream.Write( packet2, 0, packet2.Length ); //Send

					bp_send_relay = false;
				}

			}

			bp_client_device = null;
		} //END TCPTalker




		private void Find_Device()
		{
			Find_Device_Status.Text = "Searching for a Battery Optimizer Device";

			Thread.Sleep( 1000 );
			bp_profiles_form.ShowDialog();
			bp_profiles_form.Activate();
			List<string> wifi_profile = bp_profiles_form.wp_profile;

			//BP_WIFI_Packet wifi = new BP_WIFI_Packet( );
			//byte[] wifi_packet = wifi.extract( wifi_profile[0], wifi_profile[1], bp_aes_key );




			//while (bp_search_device == true)
			//{
			//    byte[] received = bp_udp_server.Receive(ref bp_end_point);
			//    BP_Packet2 rec_data = new BP_Packet2();
			//    int rec_len = received.Length;
			//    int rec_ind = 0;

			//    while (rec_ind > rec_len)
			//    {
			//        rec_data.prefix |= received[rec_ind];

			//        if (rec_data.prefix == 0xF123)
			//        {
			//            rec_data.relay = BitConverter.ToBoolean(received, rec_ind + 1);
			//            BP_Packet1 send_data = new BP_Packet1();
			//            send_data.prefix = 0xF678;
			//            //send_data.SSID = 


			//            var results = bp_collect_all_wifi_profiles.Invoke();


			//        }
			//        else
			//        {
			//            //rec_data.Prefix <<= 1;
			//        }


			//    }


			//}


			//foreach(string i in SerialPort.GetPortNames())
			//{
			//    Com_Selection.Items.Add(i);
			//}           

		} //END Find_Device





		private void OnPowerChange(object s, PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case PowerModes.Resume:
					//SP1.Close(); //Closes the port upon laptop waking up, so that the port can be reopened           
					break;
				case PowerModes.Suspend:
					break;
			}
		} //END OnPowerChange

		private void KeepOpenPort()
		{
			//while (Watch_OpenCheck) //Check later to see if can just try to open after connection lost, perhaps use windows form serialport
			{               
				//if (SP1.IsOpen == false)
				//    OpenPort(LastConnectedCom, false);                   

				Thread.Sleep(1000);
			}
		} //END KeepOpenPort



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
			if (checkBox_OptimizeChargeTime.Checked == false || (Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay && Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay) || (Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay && Battery_NormalChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay && Battery_OptimizeChargeTime.Value.TimeOfDay >= Battery_NormalChargeTime.Value.TimeOfDay) || (Battery_OptimizeChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay && Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay && Battery_OptimizeChargeTime.Value.TimeOfDay >= Battery_NormalChargeTime.Value.TimeOfDay))
				return true;
			else
				return false;
		} //END CheckOptimizationSchedule

		private void PowerWatch()
		{
			while (bp_watch_power) //While true
			{
				//Update Battery Info
				bp_power_info = SystemInformation.PowerStatus; //Get Power Status
											   
				if (bp_power_info.PowerLineStatus.ToString() == "Online") 
					UpdatePowerInfo((bp_power_info.BatteryLifePercent * 100).ToString() + "%", "Charging");              
				else
					UpdatePowerInfo((bp_power_info.BatteryLifePercent * 100).ToString() + "%", "Dis-Charging");
				

				//Check if able to optimize battery
				if (button_OptmizeBattery.Text == "ON")
				{
					//Check if Optimize checkbox is checked and also if it is time to always stay charged
					if (CheckOptimizationSchedule()) 
					{
						if (bp_power_info.BatteryLifePercent >= (float)BatteryMax.Value / 100)
						{ //combine if stmt's
							if (bp_power_info.PowerLineStatus.ToString() == "Online")
							{
								bp_relay = 0x00;
							}

						}
						else if (bp_power_info.BatteryLifePercent <= (float)BatteryMin.Value / 100)
						{
							if (bp_power_info.PowerLineStatus.ToString() == "Offline")
							{
								bp_relay = 0x01;
							}

						}

					} //END Check Charge times
					else
						bp_relay = 0x00;

				} //END Checking Optimizaion active
				Thread.Sleep(1000);
			}//END Loop


		} //END PowerWatch

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
			if (button_OptmizeBattery.Text == "OFF")
			{
				button_OptmizeBattery.Text = "ON";
				button_OptmizeBattery.BackColor = System.Drawing.Color.Lime;
			}
			else
			{
				button_OptmizeBattery.Text = "OFF";
				button_OptmizeBattery.BackColor = System.Drawing.Color.Red;
				//clear pin
			}               
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
				//clear pin
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


	} //END Class
} //END Namespace
