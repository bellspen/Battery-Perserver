using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management.Automation;

namespace BatteryPerserve
{
	public partial class BO_Form_WiFiProfiles : Form
	{
		private PowerShell wp_collect_all_wifi_profiles;
		private PowerShell wp_collect_wifi_profile;
		public List<string> wp_profile { get; set; }

		public BO_Form_WiFiProfiles()
		{
			InitializeComponent();

			//Create Powershell commands
			wp_collect_all_wifi_profiles = PowerShell.Create();
			wp_collect_all_wifi_profiles.AddScript( "netsh wlan show profiles" );
			wp_collect_wifi_profile = PowerShell.Create();

			//Fill list box
			List<string> profiles = CollectProfiles();
			if (profiles.Count != 0)
			{
				listBox_wifi_profiles.Items.Clear();
				foreach (string value in profiles)
				{
					listBox_wifi_profiles.Items.Add( value );
				}
			}

		} //END constructor

		private List<string> CollectProfiles()
		{
			List<string> profiles = new List<string>();
			string temp1;
			string[] temp2;

			var results = wp_collect_all_wifi_profiles.Invoke();

			for (int x = 0; x < results.Count(); x++)
			{
				temp1 = results[x].ToString();
				if (temp1.Contains( "All User Profile" ) == true)
				{
					temp2 = temp1.Split( ':' );
					profiles.Add( temp2[1].Substring( 1 ) );//skips the space given
				}
			}

			return profiles;
		} //END CollectProfiles

		private List<string> CollectProfile( string profile_name ) //[0] - profile_name, [1] - password
		{
			List<string> profile = new List<string>();
			profile.Add( profile_name );
			string temp1;
			string[] temp2;

			wp_collect_wifi_profile.AddScript( "netsh wlan show profile name=" + profile_name + " key=clear" );
			var results = wp_collect_wifi_profile.Invoke();

			for (int x = 0; x < results.Count(); x++)
			{
				temp1 = results[x].ToString();

				if (temp1.Contains( "Key Content" ))
				{
					temp2 = temp1.Split( ':' );
					profile.Add( temp2[1].Substring( 1 ) );//skips the space given
				}
			}

			return profile;
		} //END CollectProfile

		private void listBox_wifi_profiles_DoubleClick( object sender, EventArgs e )
		{
			wp_profile = CollectProfile( (string)listBox_wifi_profiles.SelectedItem );
			this.Close();
			//Close form
		}

		private void listBox_wifi_profiles_Click( object sender, EventArgs e )
		{
			wp_profile = CollectProfile( (string)listBox_wifi_profiles.SelectedItem );
		}
	}
}
