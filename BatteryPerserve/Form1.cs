using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatteryPerserve
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PowerStatus pwr = SystemInformation.PowerStatus;

            System.IO.Ports.SerialPort SP0 = new System.IO.Ports.SerialPort("COM" + "4", 9600); //name, baudrate
            try
            {
                SP0.Open(); //open serial port
                //MessageBox.Show("Port Opened Successfully !");
            }
            catch
            {
                MessageBox.Show("Could Not Open Specified Port !");
            }

            SP0.DiscardInBuffer(); //discard input buffer
            SP0.Write("gpio set " + "0" + "\r"); //writing "gpio set X" command to serial port         
            System.Threading.Thread.Sleep(200); //system sleep
            SP0.DiscardOutBuffer(); //discard output buffer

            SP0.DiscardInBuffer(); //discard input buffer
            SP0.Write("gpio clear " + "0" + "\r"); //writing "gpio set X" command to serial port
            System.Threading.Thread.Sleep(200); //system sleep
            SP0.DiscardOutBuffer(); //discard output buffer
            
            SP0.Close();

        }
    }
}
