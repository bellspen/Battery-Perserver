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
		public BatteryOptimizer()
		{
			InitializeComponent();

			BO_Constrcutor();

		} //END Constructor

		//Form Functions::::::::::::::::::::::::::::::::::::
		private void OnPowerChange( object s, PowerModeChangedEventArgs e )
		{
			switch (e.Mode)
			{
				case PowerModes.Resume:
					{
						Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
						watch_power = true;
						allow_search = true;
						//Auto Connect & Start Optimizing
						if (BatOpSettings.auto_connect == true)
						{
							button_search_for_device_Click();
						}
						break;
					}
				case PowerModes.Suspend:
					{
						watch_power = false;
						allow_search = false;
						if (DeviceStatus.CONNECTED == device_status)
							UpdateDeviceStatus( DeviceStatus.LOST_CONNECTION );
						break;
					}


			}
		} //END OnPowerChange


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
				//Turn relay back on:
				relay_status = (byte)DeviceRelay.Relay_ON;
				UDP_SendToClient( packet_manager.msg_type_relay );
			}
		} //END Optimize Battery


		private void Program_Settings_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();

			//Start program at boot:
			if (Program_Settings.SelectedIndex == (int)ProgramSettingsCheckbox.StartProgramBoot)
			{
				if (Program_Settings.CheckedIndices.Contains( (int)ProgramSettingsCheckbox.StartProgramBoot ) == false) //Not checked, being checked
					AddToStartup();
				else //Checked, being removed
					RemoveFromStartup();
			}
			//Auto Connect & Start Optimizing:
			else if (Program_Settings.SelectedIndex == (int)ProgramSettingsCheckbox.AutoConnectStartOptim)
			{
				if (Program_Settings.CheckedIndices.Contains( (int)ProgramSettingsCheckbox.AutoConnectStartOptim ) == false) //Not checked, being checked
				{
					BatOpSettings.auto_connect = true;
					//Watch_OpenCheck = true;
				}
				else //Checked, being removed
				{
					BatOpSettings.auto_connect = false;
					//Watch_OpenCheck = false;
				}
			}
			//Start Minimized:
			else if (Program_Settings.SelectedIndex == (int)ProgramSettingsCheckbox.StartMinimized)
			{
				if (Program_Settings.CheckedIndices.Contains( (int)ProgramSettingsCheckbox.StartMinimized ) == false) //Not checked, being checked
					BatOpSettings.start_minimized = true;
				else //Checked, being removed
					BatOpSettings.start_minimized = false;
			}

			SaveSettings( BatOpSettings );

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
