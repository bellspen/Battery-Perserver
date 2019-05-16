﻿using System;
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

namespace BatteryPerserve
{
    public partial class BatteryPerserver : Form
    {
        private delegate void SafeCallDelegate(string text);
        private SerialPort SP0;
        private PowerStatus Pwr_Info;
        private Thread Pwr_Watching;
        private bool Watch_Pwr;
        private bool Pwr_Control;

        public BatteryPerserver()
        {
            InitializeComponent();
            Find_Coms();
            SP0 = new SerialPort();
            Pwr_Control = true;
            Watch_Pwr = true;
            Pwr_Watching = new Thread(PowerWatch);
            Pwr_Watching.Start();
        } //END Constructor

        private void Find_Coms()
        {
            
            //string[] PortName = SerialPort.GetPortNames();
            foreach(string i in SerialPort.GetPortNames())
            {
                Com_Selection.Items.Add(i);
            }           

        } //END Find_Coms

        private void Com_Con_Dis_Click(object sender, EventArgs e)
        {
            if (Com_Con_Dis.Text == "Connect")
                OpenPort();
            else //Disconnect
                ClosePort();                
          
        } //END Com_Con_Dis

        private void OpenPort()
        {
            try
            {
                SP0.PortName = Com_Selection.SelectedItem.ToString(); //name
                SP0.BaudRate = 9600; //baudrate
                SP0.Open(); //open serial port
                //MessageBox.Show("Port Opened Successfully !");
                Com_Con_Dis.Text = "Disconnect";
            }
            catch
            {
                MessageBox.Show("Could Not Open Specified Port !");
            }

        } //END OpenPort

        private void ClosePort()
        {
            try
            {
                SP0.DiscardInBuffer(); //discard input buffer
                SP0.Write("gpio writeall 00" + "\r"); //writing "gpio writeall xx" command to serial port //Clearing all gpio's
                System.Threading.Thread.Sleep(200); //system sleep
                SP0.DiscardOutBuffer(); //discard output buffer

                SP0.Close(); //close serial port
                //MessageBox.Show("Port Closed Successfully !");
                Com_Con_Dis.Text = "Connect";
            }
            catch
            {
                MessageBox.Show("Could Not Close Specified Port !");
            }

        } //END ClosePort

        private void UpdatePowerInfo(string text)
        {
            if (Battery_Percentage.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdatePowerInfo);
                Invoke(d, new object[] { text });
            }
            else
                Battery_Percentage.Text = text; //Check about accessing from other threads

        } //END UpdatePowerInfo

        private void PowerWatch()
        {
            while (Watch_Pwr) //While true
            {
                //Update Battery Info
                Pwr_Info = SystemInformation.PowerStatus; //Get Power Status

                UpdatePowerInfo((Pwr_Info.BatteryLifePercent * 100).ToString() + "%");
                                               
                if (Pwr_Info.PowerLineStatus.ToString() == "Online") //fix same as above
                    Battery_LineStatus.Text = "Charging";
                else
                    Battery_LineStatus.Text = "Dis-Charging";

                //Check if able to optimize battery
                if (button_OptmizeBattery.Text == "ON")
                {
                    if ((checkBox_OptimizeChargeTime.Checked == false || Battery_OptimizeChargeTime.Value.TimeOfDay <= DateTime.Now.TimeOfDay) && (checkBox_NormalChargeTime.Checked == false || Battery_NormalChargeTime.Value.TimeOfDay >= DateTime.Now.TimeOfDay)) //Check if checkbox is checked and also if it is time to always stay charged
                    {

                        if (SP0.IsOpen) //Check Port Open && not
                        {
                            if (Pwr_Info.BatteryLifePercent >= 0.42)
                            {
                                if (Pwr_Info.PowerLineStatus.ToString() == "Online")
                                {
                                    SP0.DiscardInBuffer(); //discard input buffer
                                    SP0.Write("gpio set " + "0" + "\r"); //writing "gpio set X" command to serial port
                                    System.Threading.Thread.Sleep(200); //system sleep
                                    SP0.DiscardOutBuffer(); //discard output buffer
                                }

                            }
                            else if (Pwr_Info.BatteryLifePercent <= 0.40)
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

                } //END Checking Optimizaion active

            }//END Loop


        } //END PowerWatch

        private void checkBox1_NormalChargeTime_CheckedChanged(object sender, EventArgs e)
        {
            if (Battery_NormalChargeTime.Enabled == false)
                Battery_NormalChargeTime.Enabled = true;
            else //true
                Battery_NormalChargeTime.Enabled = false;


        } //END CheckBox

        private void checkBox_OptimizeChargeTime_CheckedChanged(object sender, EventArgs e)
        {
            if (Battery_OptimizeChargeTime.Enabled == false)
                Battery_OptimizeChargeTime.Enabled = true;
            else //true
                Battery_OptimizeChargeTime.Enabled = false;
        } //END CheckBox

        private void button_DefaultBatteryRange_Click(object sender, EventArgs e)
        {
            BatteryMin.Value = 40;
            BatteryMax.Value = 60;
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
            }
                


        }
    } //END Class
} //END Namespace
