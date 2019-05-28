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
using System.Threading;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;

namespace BatteryPerserve
{   
    public partial class BatteryOptimizer : Form
    {
        [Serializable()]
        public struct Settings_BatteryOptimizer
        {           
            public bool AutoConnect;
            public bool StartMinimized;
            public string LastCom;
            public bool OptimizeSchedule;
            public DateTime StartChargeTime;
            public DateTime StopChargeTime;
            public decimal BatteryRangeMin;
            public decimal BatteryRangeMax;
        }

        private delegate void SafeCallDelegate(string text, string text2); //UpdatePowerInfo
        private delegate void SafeCallDelegate2(string text, bool warning); //OpenPort
        //private SerialPort SP0;
        private string LastConnectedCom;
        private PowerStatus Pwr_Info;
        private Thread Pwr_Watching;
        private Thread Com_OpenCheck;
        private bool Watch_Pwr; //Pwr_Watching Thread
        private bool Watch_OpenCheck; //Com_OpenCheck
        private bool Pwr_Control;
        

        public BatteryOptimizer()
        {
            InitializeComponent();
            //Check if first time ever running program
            RegistryKey key1 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer");
            if (key1 == null /*|| key1.GetValue("FirstEverRun") == null*/) 
                InitializeRegistry();
            else
                key1.Close();

            InitialRegistryCheck(); //Check Settings in Registry

            Find_Coms();
            SP1 = new SerialPort();

            if (Com_Selection.Items.Count == 0)
                MessageBox.Show("Please turn on Bluetooth.");
            else
            {              
                if (Program_Settings.CheckedIndices.Contains(1) == true) //Auto Connect
                {
                    if (LastConnectedCom == "")
                        MessageBox.Show("Auto Connect could not work because there is no previous connection.");
                    else
                    {
                        button_OptmizeBattery_Click();
                        Watch_OpenCheck = true;
                        Com_OpenCheck = new Thread(KeepOpenPort);
                        Com_OpenCheck.Start();
                        //OpenPort(LastConnectedCom);
                    }
                }
            }
            
            Pwr_Control = true;
            Watch_Pwr = true;
            Pwr_Watching = new Thread(PowerWatch);
            Pwr_Watching.Start();
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
            //Auto Connect & Start Optimizing
            if (BatOpSettings.AutoConnect == true)
                Program_Settings.SetItemChecked(1, true);
            //Start Minimized
            if (BatOpSettings.StartMinimized == true)
            {
                Program_Settings.SetItemChecked(2, true);
                this.WindowState = FormWindowState.Minimized;
            }
            //Last Com connected to
            LastConnectedCom = BatOpSettings.LastCom;
            //Optimize Charge Schedule
            if (BatOpSettings.OptimizeSchedule == true)
                checkBox_OptimizeChargeTime.Checked = true;
            //Charge Start Stop times
            Battery_OptimizeChargeTime.Value = BatOpSettings.StartChargeTime;
            Battery_NormalChargeTime.Value = BatOpSettings.StopChargeTime;
            //Optimal Battery Range
            BatteryMin.Value = BatOpSettings.BatteryRangeMin;
            BatteryMax.Value = BatOpSettings.BatteryRangeMax;

        } //END InitialRegistryCheck

        private void InitializeRegistry()
        {
            Settings_BatteryOptimizer InitialSettings = new Settings_BatteryOptimizer
            {
                AutoConnect = false,
                StartMinimized = false,
                LastCom = "",
                OptimizeSchedule = false,
                StartChargeTime = Battery_OptimizeChargeTime.Value,
                StopChargeTime = Battery_NormalChargeTime.Value,
                BatteryRangeMin = BatteryMin.Value,
                BatteryRangeMax = BatteryMax.Value
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

        private void Find_Coms()
        {           
            foreach(string i in SerialPort.GetPortNames())
            {
                Com_Selection.Items.Add(i);
            }           

        } //END Find_Coms

        private void Com_Con_Dis_Click(object sender, EventArgs e)
        {
            if (Com_Con_Dis.Text == "Connect")
                OpenPort(Com_Selection.SelectedItem.ToString(), true);
            else //Disconnect
                ClosePort();                
          
        } //END Com_Con_Dis

        private void KeepOpenPort()
        {
            while (Watch_OpenCheck) //Check later to see if can just try to open after connection lost, perhaps use windows form serialport
            {
                
                if (SP1.IsOpen == false)
                {
                    OpenPort(LastConnectedCom, false);                   
                }
                Thread.Sleep(1000);
            }
        } //END KeepOpenPort

        private void OpenPort(string Com_Name, bool warning) //Test later to see if needs a bool to see if in main thread or not
        {
            if (Com_Con_Dis.InvokeRequired)
            {
                var d = new SafeCallDelegate2(OpenPort);
                Invoke(d, new object[] { Com_Name, warning });
            }
            else
            {
                try
                {
                    SP1.PortName = Com_Name; //name
                    SP1.BaudRate = 9600; //baudrate
                    SP1.Open(); //open serial port
                                //MessageBox.Show("Port Opened Successfully !");

                    LastConnectedCom = Com_Name;
                    Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
                    BatOpSettings.LastCom = Com_Name;
                    SaveSettings(BatOpSettings);

                    Com_Con_Dis.Text = "Disconnect";
                    button_OptmizeBattery.Enabled = true;
                }
                catch
                {
                    if (warning)
                        MessageBox.Show("Could Not Open Specified Port! Make sure device is on.");
                }
            }

            
        } //END OpenPort

        private void ClearAllGpio()
        {
            SP1.DiscardInBuffer(); //discard input buffer
            SP1.Write("gpio writeall 00" + "\r"); //writing "gpio writeall xx" command to serial port //Clearing all gpio's
            System.Threading.Thread.Sleep(200); //system sleep
            SP1.DiscardOutBuffer(); //discard output buffer
        } //END ClearAllGpio

        private void ClosePort()
        {
            try
            {
                ClearAllGpio();
                SP1.Close(); //close serial port
                //MessageBox.Show("Port Closed Successfully !");
                Com_Con_Dis.Text = "Connect";
                button_OptmizeBattery.Enabled = false;
            }
            catch
            {
                MessageBox.Show("Could Not Close Specified Port !");
            }

        } //END ClosePort

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
            while (Watch_Pwr) //While true
            {
                //Update Battery Info
                Pwr_Info = SystemInformation.PowerStatus; //Get Power Status
                                               
                if (Pwr_Info.PowerLineStatus.ToString() == "Online") 
                    UpdatePowerInfo((Pwr_Info.BatteryLifePercent * 100).ToString() + "%", "Charging");              
                else
                    UpdatePowerInfo((Pwr_Info.BatteryLifePercent * 100).ToString() + "%", "Dis-Charging");
                

                //Check if able to optimize battery
                if (button_OptmizeBattery.Text == "ON")
                {
                    //Check if Optimize checkbox is checked and also if it is time to always stay charged
                    if (CheckOptimizationSchedule()) 
                    {

                        if (SP1.IsOpen) //Check Port Open && not
                        {
                            if (Pwr_Info.BatteryLifePercent >= (float)BatteryMax.Value / 100)
                            { //combine if stmt's
                                if (Pwr_Info.PowerLineStatus.ToString() == "Online")
                                {
                                    SP1.DiscardInBuffer(); //discard input buffer
                                    SP1.Write("gpio set " + "0" + "\r"); //writing "gpio set X" command to serial port
                                    System.Threading.Thread.Sleep(200); //system sleep
                                    SP1.DiscardOutBuffer(); //discard output buffer
                                }

                            }
                            else if (Pwr_Info.BatteryLifePercent <= (float)BatteryMin.Value / 100)
                            {
                                if (Pwr_Info.PowerLineStatus.ToString() == "Offline")
                                {
                                    SP1.DiscardInBuffer(); //discard input buffer
                                    SP1.Write("gpio clear " + "0" + "\r"); //writing "gpio clear X" command to serial port
                                    System.Threading.Thread.Sleep(200); //system sleep
                                    SP1.DiscardOutBuffer(); //discard output buffer
                                }

                            }
                        } //END Check Port

                    } //END Check Charge times
                    else
                        ClearAllGpio();

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
                BatOpSettings.OptimizeSchedule = true;
            }
            else //true
            {
                Battery_OptimizeChargeTime.Enabled = false;
                Battery_NormalChargeTime.Enabled = false;
                BatOpSettings.OptimizeSchedule = false;
            }

            SaveSettings(BatOpSettings);
        } //END CheckBox

        private void button_DefaultBatteryRange_Click(object sender, EventArgs e)
        {
            BatteryMin.Value = 40;
            BatteryMax.Value = 60;
            Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
            BatOpSettings.BatteryRangeMin = BatteryMin.Value;
            BatOpSettings.BatteryRangeMax = BatteryMax.Value;
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
                ClearAllGpio();
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
                ClearAllGpio();
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
                    BatOpSettings.AutoConnect = true;
                    Watch_OpenCheck = true;
                    Com_OpenCheck = new Thread(KeepOpenPort);
                    Com_OpenCheck.Start();
                }
                else //Checked, being removed
                {
                    BatOpSettings.AutoConnect = false;
                    Watch_OpenCheck = false;
                    if (Com_OpenCheck != null && Com_OpenCheck.IsAlive)
                        Com_OpenCheck.Abort();
                }

                SaveSettings(BatOpSettings);

            }
            else if (Program_Settings.SelectedIndex == 2)
            {
                Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();

                if (Program_Settings.CheckedIndices.Contains(2) == false) //Not checked, being checked               
                    BatOpSettings.StartMinimized = true;
                else //Checked, being removed
                    BatOpSettings.StartMinimized = false;

                SaveSettings(BatOpSettings);
            }

        } //END Program_Settings_ItemCheck

        private void Battery_OptimizeChargeTime_ValueChanged(object sender, EventArgs e)
        {
            Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
            BatOpSettings.StartChargeTime = Battery_OptimizeChargeTime.Value;
            SaveSettings(BatOpSettings);
        } //END Battery_OptimizeChargeTime_ValueChanged

        private void Battery_NormalChargeTime_ValueChanged(object sender, EventArgs e)
        {
            Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
            BatOpSettings.StopChargeTime = Battery_NormalChargeTime.Value;
            SaveSettings(BatOpSettings);
        } //END Battery_NormalChargeTime_ValueChanged

        private void BatteryMin_ValueChanged(object sender, EventArgs e)
        {
            Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
            BatOpSettings.BatteryRangeMin = BatteryMin.Value;
            SaveSettings(BatOpSettings);
        } //END BatteryMin_ValueChanged

        private void BatteryMax_ValueChanged(object sender, EventArgs e)
        {
            Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
            BatOpSettings.BatteryRangeMax = BatteryMax.Value;
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
