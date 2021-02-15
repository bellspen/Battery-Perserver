using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;


namespace BatteryPerserve
{
	//Class Helper Classes -----------------------------------------------------------
	[Serializable()]
	public struct Settings_BatteryOptimizer
	{
		//Form information:
		public bool     auto_connect;
		public bool     start_minimized;
		public bool     optimize_schedule;
		public DateTime start_charge_time;
		public DateTime stop_charge_time;
		public decimal  battery_range_min;
		public decimal  battery_range_max;
		//Device information:
		public string	device_name;
		public DeviceStatus device_satus;
		public byte[]	device_id;
	}

	public struct EncDec_Resources
	{
		public AesGcm256 gcm256;
		public byte[] aes_key;// = Encoding.UTF8.GetBytes( "89mUHZXCu94IbwUxMdSNiNCGw9OyLyeu" );
		public string[] associated_data;
	}

	//Enums: -------------------------------------------------------------------------------
	public enum DeviceStatus
	{
		NO_DEVICE = 0,
		SEARCHING,
		FOUND,
		CONNECTED,
		LOST_CONNECTION
	};




}
