using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryPerserve
{
	class BO_Packets
	{
		//Packet Prefixs:
		public readonly UInt32 prefix = 0XB30B60B9;
		private readonly byte prefix1 = 0XB3;
		private readonly byte prefix2 = 0X0B;
		private readonly byte prefix3 = 0X60;
		private readonly byte prefix4 = 0XB9;
		public readonly UInt32 prefix_cs = 0xC30C60C9;
		private readonly byte prefix_cs1 = 0xC3;
		private readonly byte prefix_cs2 = 0x0C;
		private readonly byte prefix_cs3 = 0x60;
		private readonly byte prefix_cs4 = 0xC9;

		//Message types:
		public readonly byte msg_type_relay = 0x00;
		public readonly byte msg_type_wifi = 0x01;
		public readonly byte msg_type_name = 0x02;
		public readonly byte msg_type_response = 0x03;
		public readonly byte msg_type_status = 0x04;
		/*
		 * 0x00 - "RelayPacket"
		 * 0x01 = "WifiPacket"
		 * 0x02 = "NamePacket"
		 * 0x03 = "ResponsePacket"
		 * 0x04 = "StatusPacket"
		 */

		public BO_Packets()
		{

		}


		public byte[] BuildBasePacket(byte msg_type, byte[] iv, byte[] enc_data)
		{
			/*
			* 4 - Prefix
			* 1 - Msg Type
			* 16 - IV
			* 2 - Encrypted & Tag/Mac data size
			* ?? - Encrypted & Tag/Mac data
			* 4 - Checksum Prefix
			* 1 - Checksum
			*/
			byte[] packet = null;
			byte checksum = 0x00;
			int index = 0;
			int size = 28;
			UInt16 enc_data_size;

			//Get encrypted data size & increase total size:
			enc_data_size = (UInt16)enc_data.Length;
			size += enc_data_size;

			//Get bytes:
			packet = new byte[size];

			//Prefix:
			packet[0] = prefix1;
			packet[1] = prefix2;
			packet[2] = prefix3;
			packet[3] = prefix4;

			//Msg Type:
			packet[4] = msg_type;
			index += 5;

			//IV:
			iv.CopyTo( packet, index );
			index += iv.Length;

			//Encrypted data size:
			packet[index] = (byte)(enc_data_size & 0xFF);
			packet[index + 1] = (byte)((enc_data_size & 0xFF00) >> 8);
			index += 2;

			//Encrypted data & Tag/Mac:
			enc_data.CopyTo( packet, index );
			index += enc_data_size;

			//Checksum Prefix:
			packet[index] = prefix_cs1;
			packet[index + 1] = prefix_cs2;
			packet[index + 2] = prefix_cs3;
			packet[index + 3] = prefix_cs4;
			index += 4;

			//Checksum:
			for (int x = 0; x < packet.Length - 1; x++)
			{
				checksum ^= packet[x];
			}

			//Append checksum:
			packet[index] = checksum;

			return packet;
		} //END BuildBasePacket


		public byte[] BuildRelayPacket ( byte[] dev_id, byte relay, ref EncDec_Resources p_encec_resources )
		{
			byte[] relay_packet = null;
			byte[] iv = p_encec_resources.gcm256.NewIv();
			byte[] to_encrypt = new byte[128];
			byte[] ass_data = Encoding.UTF8.GetBytes( p_encec_resources.associated_data[0] );
			byte[] encrypted_data;

			//Prepare to encrypt:
			to_encrypt[0] = relay;
			to_encrypt[1] = (byte)'R';
			to_encrypt[2] = (byte)'E';
			to_encrypt[3] = (byte)'L';
			to_encrypt[4] = (byte)'A';
			to_encrypt[5] = (byte)'Y';

			dev_id.CopyTo( to_encrypt, 100 );

			//Encrypt:
			encrypted_data = p_encec_resources.gcm256.Encrypt( to_encrypt, p_encec_resources.aes_key, iv, ass_data );

			//Build Packet:
			if (null != encrypted_data)
				relay_packet = BuildBasePacket( msg_type_relay, iv, encrypted_data );


			return relay_packet;
		} //END BuildRelayPacket


		public byte[] BuildWifiPacket( byte[] dev_id, string p_ssid, string p_pass, ref EncDec_Resources p_encec_resources )
		{
			byte[] wifi_packet = null;
			byte[] iv = p_encec_resources.gcm256.NewIv();
			byte[] to_encrypt = new byte[128];
			byte[] ssid = Encoding.UTF8.GetBytes( p_ssid );
			byte[] pass = Encoding.UTF8.GetBytes( p_pass );
			byte[] ass_data = Encoding.UTF8.GetBytes( p_encec_resources.associated_data[1] );
			byte[] encrypted_data;

			//Prepare to encrypt:
			ssid.CopyTo( to_encrypt, 0 );
			pass.CopyTo( to_encrypt, 50 );
			dev_id.CopyTo( to_encrypt, 100 );

			//Encrypt:
			encrypted_data = p_encec_resources.gcm256.Encrypt( to_encrypt, p_encec_resources.aes_key, iv, ass_data );

			//Build Packet:
			if (null != encrypted_data)
				wifi_packet = BuildBasePacket( msg_type_wifi, iv, encrypted_data );


			return wifi_packet;
		} //END BuildWifiPacket


		public byte[] BuildDeviceNamePacket( byte[] dev_id, string p_device_name, ref EncDec_Resources p_encec_resources )
		{
			byte[] device_name_packet = null;
			byte[] iv = p_encec_resources.gcm256.NewIv();
			byte[] to_encrypt = new byte[128];
			byte[] device_name = Encoding.UTF8.GetBytes( p_device_name );




			return device_name_packet;
		} //END BuildDeviceNamePacket


		public byte[] BuildStatusPacket( byte[] dev_id, DeviceStatus status, ref EncDec_Resources p_encec_resources )
		{
			byte[] status_packet = null;
			byte[] iv = p_encec_resources.gcm256.NewIv();
			byte[] to_encrypt = new byte[128];
			byte[] ass_data = Encoding.UTF8.GetBytes( p_encec_resources.associated_data[4] );
			byte[] encrypted_data;

			//Prepare to encrypt:
			to_encrypt[0] = (byte)status;
			to_encrypt[1] = (byte)'S';
			to_encrypt[2] = (byte)'T';
			to_encrypt[3] = (byte)'A';
			to_encrypt[4] = (byte)'T';
			to_encrypt[5] = (byte)'U';
			to_encrypt[6] = (byte)'S';

			dev_id.CopyTo( to_encrypt, 100 );

			//Encrypt:
			encrypted_data = p_encec_resources.gcm256.Encrypt( to_encrypt, p_encec_resources.aes_key, iv, ass_data );

			//Build Packet:
			if (null != encrypted_data)
				status_packet = BuildBasePacket( msg_type_status, iv, encrypted_data );


			return status_packet;
		} //END BuildStatusPacket


	} //END class BO_Packets
}
