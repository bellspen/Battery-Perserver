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
            public string LastCom;
            public bool OptimizeSchedule;
            public DateTime StartChargeTime;
            public DateTime StopChargeTime;
            public decimal BatteryRangeMin;
            public decimal BatteryRangeMax;
        }

        private delegate void SafeCallDelegate(string text, string text2);
        private SerialPort SP0;
        private string LastConnectedCom;
        private PowerStatus Pwr_Info;
        private Thread Pwr_Watching;
        private bool Watch_Pwr;
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
            SP0 = new SerialPort();
          
            if (Program_Settings.CheckedIndices.Contains(1) == true) //Auto Connect
            {
                button_OptmizeBattery_Click();
                OpenPort(LastConnectedCom);
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
                OpenPort(Com_Selection.SelectedItem.ToString());
            else //Disconnect
                ClosePort();                
          
        } //END Com_Con_Dis

        private void OpenPort(string Com_Name)
        {
            try
            {
                SP0.PortName = Com_Name; //name
                SP0.BaudRate = 9600; //baudrate
                SP0.Open(); //open serial port
                //MessageBox.Show("Port Opened Successfully !");

                LastConnectedCom = Com_Name;
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
                Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");
                BatOpSettings.LastCom = Com_Name;
                key.SetValue("Settings", BatOpSettings);
                key.Close();

                Com_Con_Dis.Text = "Disconnect";
                button_OptmizeBattery.Enabled = true;
            }
            catch
            {
                MessageBox.Show("Could Not Open Specified Port! Make sure device is on.");
            }

        } //END OpenPort

        private void ClearAllGpio()
        {
            SP0.DiscardInBuffer(); //discard input buffer
            SP0.Write("gpio writeall 00" + "\r"); //writing "gpio writeall xx" command to serial port //Clearing all gpio's
            System.Threading.Thread.Sleep(200); //system sleep
            SP0.DiscardOutBuffer(); //discard output buffer
        }

        private void ClosePort()
        {
            try
            {
                ClearAllGpio();
                SP0.Close(); //close serial port
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
            if (checkBox_OptimizeChargeTime.Checked == false || (Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay && Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay) || (Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay && Battery_NormalChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay && Battery_OptimizeChargeTime.Value.TimeOfDay >= Battery_NormalChargeTime.Value.TimeOfDay) || (Battery_OptimizeChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay && Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay))
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

                        if (SP0.IsOpen) //Check Port Open && not
                        {
                            if (Pwr_Info.BatteryLifePercent >= (float)BatteryMax.Value / 100)
                            { //combine if stmt's
                                if (Pwr_Info.PowerLineStatus.ToString() == "Online")
                                {
                                    SP0.DiscardInBuffer(); //discard input buffer
                                    SP0.Write("gpio set " + "0" + "\r"); //writing "gpio set X" command to serial port
                                    System.Threading.Thread.Sleep(200); //system sleep
                                    SP0.DiscardOutBuffer(); //discard output buffer
                                }

                            }
                            else if (Pwr_Info.BatteryLifePercent <= (float)BatteryMin.Value / 100)
                            {
                                if (Pwr_Info.PowerLineStatus.ToString() == "Offline")
                                {
                                    SP0.DiscardInBuffer(); //discard input buffer
                                    SP0.Write("gpio clear " + "0" + "\r"); //writing "gpio clear X" command to serial port
                                    System.Threading.Thread.Sleep(200); //system sleep
                                    SP0.DiscardOutBuffer(); //discard output buffer
                                }

                            }
                        } //END Check Port

                    } //END Check Charge times
                    else
                        ClearAllGpio();

                } //END Checking Optimizaion active

            }//END Loop


        } //END PowerWatch

        private void checkBox_OptimizeChargeTime_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
            Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");

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

            key.SetValue("Settings", BatOpSettings);
            key.Close();
        } //END CheckBox

        private void button_DefaultBatteryRange_Click(object sender, EventArgs e)
        {
            BatteryMin.Value = 40;
            BatteryMax.Value = 60;
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
            Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");
            BatOpSettings.BatteryRangeMin = BatteryMin.Value;
            BatOpSettings.BatteryRangeMax = BatteryMax.Value;
            key.SetValue("Settings", BatOpSettings);
            key.Close();

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
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
                Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");

                if (Program_Settings.CheckedIndices.Contains(1) == false) //Not checked, being checked               
                    BatOpSettings.AutoConnect = true;
                else //Checked, being removed
                    BatOpSettings.AutoConnect = false;

                key.SetValue("Settings", BatOpSettings);
                key.Close();

            }

        } //END Program_Settings_ItemCheck

        private void Battery_OptimizeChargeTime_ValueChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
            Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");
            BatOpSettings.StartChargeTime = Battery_OptimizeChargeTime.Value;
            key.SetValue("Settings", BatOpSettings);
            key.Close();
        }

        private void Battery_NormalChargeTime_ValueChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
            Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");
            BatOpSettings.StopChargeTime = Battery_NormalChargeTime.Value;
            key.SetValue("Settings", BatOpSettings);
            key.Close();
        }

        private void BatteryMin_ValueChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
            Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");
            BatOpSettings.BatteryRangeMin = BatteryMin.Value;
            key.SetValue("Settings", BatOpSettings);
            key.Close();
        }

        private void BatteryMax_ValueChanged(object sender, EventArgs e)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\BatteryOptimizer", true);
            Settings_BatteryOptimizer BatOpSettings = (Settings_BatteryOptimizer)key.GetValue("Settings");
            BatOpSettings.BatteryRangeMax = BatteryMax.Value;
            key.SetValue("Settings", BatOpSettings);
            key.Close();
        }
    } //END Class
} //END Namespace
