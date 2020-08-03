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
        //Class Helper Classes -----------------------------------------------------------
        [Serializable()]
        public struct Settings_BatteryOptimizer
        {           
            public bool auto_connect;
            public bool start_minimized;
            //public string LastCom;
            public bool optimize_schedule;
            public DateTime start_charge_time;
            public DateTime stop_charge_time;
            public decimal battery_range_min;
            public decimal battery_range_max;
        }

        public class BP_Packet1 //Total bytes: 64 
        {
            private uint prefix; // 0xF678 //4 bytes
            public string ssid; // 30 bytes
            public string pass; // 30 bytes

            public BP_Packet1()
            {
                prefix = 0xF678;
            }

            public BP_Packet1( string p_ssid , string p_pass )
            {
                prefix = 0xF678;
                ssid = p_ssid;
                pass = p_pass;
            }

            public byte[] extract()
            {
                byte[] packet = new byte[64];
                byte[] b_prefix = BitConverter.GetBytes( prefix );
                byte[] b_ssid = System.Text.Encoding.ASCII.GetBytes( ssid );
                byte[] b_pass = System.Text.Encoding.ASCII.GetBytes( pass );

                int x = 0;

                for (int y = 0; y < b_prefix.Length; y++, x++)
                {
                    packet[x] = b_prefix[y];
                }

                for (int y = 0; y < b_ssid.Length; y++, x++)
                {
                    packet[x] = b_ssid[y];
                }

                x = 34;

                for (int y = 0; y < b_pass.Length; y++, x++)
                {
                    packet[x] = b_pass[y];
                }

                return packet;
            }
        } //END class BP_Packet1

        public class BP_Packet2 //Total bytes: 5 
        {
            public uint prefix; // 0xF123 //4 bytes
            public bool relay; // 0 - Open, 1 - Close
        } //END class BP_Packet2


        //Class Data ---------------------------------------------------------------------
        private delegate void SafeCallDelegate(string text, string text2); //UpdatePowerInfo
        private delegate void SafeCallDelegate2(string text, bool warning); //OpenPort

        private PowerStatus bp_power_info;
        private WifiProfiles bp_profiles_form;

        private UdpClient bp_udp_server;
        private IPEndPoint bp_end_point;
        private TcpListener bp_tcp_server;
        private const int bp_listen_port = 8000;
        private bool bp_search_device;



        private Thread bp_watch_power_thread;
        private Thread bp_find_device_thread;
        //private Thread port_opening;

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

            bp_profiles_form = new WifiProfiles();

            bp_find_device_thread = new Thread( Find_Device );
            bp_find_device_thread.Start();
            //Find_Device();


            //bp_power_control = true;
            //bp_watch_power = true;
            //bp_watch_power_thread = new Thread(PowerWatch);
            //bp_watch_power_thread.Start();
        } //END Constructor

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

        private void Find_Device()
        {
            Find_Device_Status.Text = "Searching for a Battery Optimizer Device";

            Thread.Sleep( 1000 );
            bp_profiles_form.ShowDialog();
            bp_profiles_form.Activate();
            List<string> wifi_profile = bp_profiles_form.wp_profile;
            BP_Packet1 wifi = new BP_Packet1( wifi_profile[0], wifi_profile[1] );
            byte[] wifi_packet = wifi.extract();




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

                        if (false) //Check Port Open && not
                        {
                            if (bp_power_info.BatteryLifePercent >= (float)BatteryMax.Value / 100)
                            { //combine if stmt's
                                if (bp_power_info.PowerLineStatus.ToString() == "Online")
                                {

                                }

                            }
                            else if (bp_power_info.BatteryLifePercent <= (float)BatteryMin.Value / 100)
                            {
                                if (bp_power_info.PowerLineStatus.ToString() == "Offline")
                                {

                                }

                            }
                        } //END Check Port

                    } //END Check Charge times
                    //else
                        //ClearAllGpio();

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

        private void SP1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show("Error from port");
        }
    } //END Class
} //END Namespace
