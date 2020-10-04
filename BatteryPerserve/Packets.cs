using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryPerserve
{
	class BP_Packets
	{
		//Packet Prefixs:
		private readonly UInt32 prefix = 0XB30B60B9;
		private readonly byte prefix1 = 0XB3;
		private readonly byte prefix2 = 0X0B;
		private readonly byte prefix3 = 0X60;
		private readonly byte prefix4 = 0XB9;
		private readonly UInt32 prefix_cs = 0xC30C60C9;
		private readonly byte prefix_cs1 = 0xC3;
		private readonly byte prefix_cs2 = 0x0C;
		private readonly byte prefix_cs3 = 0x60;
		private readonly byte prefix_cs4 = 0xC9;

		//For Encryption/Decryption
		private AesGcm256 gcm256;
		private readonly byte[] aes_key = Encoding.UTF8.GetBytes( "89mUHZXCu94IbwUxMdSNiNCGw9OyLyeu" );
		private string[] associated_data;
		/*
		 * 0x00 - "RelayPacket"
		 * 0x01 = "WifiPacket"
		 */

		public BP_Packets()
		{
			gcm256 = new AesGcm256();

			associated_data = new string[10];
			associated_data[0] = "RelayPacket";
			associated_data[1] = "WifiPacket";
		}


		public byte[] BuildRelayPacket (byte relay)
		{
			byte[] relay_packet = null;
			byte[] iv = gcm256.NewIv();
			byte[] to_encrypt = new byte[128];
			byte[] ass_data = Encoding.UTF8.GetBytes( associated_data[0] );
			byte[] encrypted_data;
			byte checksum = 0x00;
			UInt16 ed_size = 0;
			int rp_index = 0;
			int rp_size = 28;
			/*
			 * 4 - Prefix
			 * 1 - Msg Type
			 * 16 - IV
			 * 2 - Encrypted & Tag/Mac data size
			 * ?? - Encrypted & Tag/Mac data
			 * 4 - Checksum Prefix
			 * 1 - Checksum
			*/
			//Prepare to encrypt:
			to_encrypt[0] = relay;

			//Encrypt:
			encrypted_data = gcm256.Encrypt( to_encrypt, aes_key, iv, ass_data );

			//Build Packet:
			ed_size = (UInt16)encrypted_data.Length;
			rp_size += ed_size;
			relay_packet = new byte[rp_size];

			relay_packet[0] = prefix1; //Prefix
			relay_packet[1] = prefix2;
			relay_packet[2] = prefix3;
			relay_packet[3] = prefix4;
			relay_packet[4] = 0x00; //Msg Type
			rp_index += 5;

			iv.CopyTo( relay_packet, rp_index ); //IV
			rp_index += gcm256.IVBitSize / 8;

			relay_packet[rp_index] = (byte)(ed_size & 0xFF); //Encrypted data size
			relay_packet[rp_index + 1] = (byte)((ed_size & 0xFF00) >> 4);
			rp_index += 2;

			encrypted_data.CopyTo( relay_packet, rp_index); //Encrypted data & Tag/Mac
			rp_index += ed_size;

			relay_packet[rp_index] = prefix_cs1; //Checksum Prefix
			relay_packet[rp_index + 1] = prefix_cs2;
			relay_packet[rp_index + 2] = prefix_cs3;
			relay_packet[rp_index + 3] = prefix_cs4;
			rp_index += 4;

			for (int x = 0; x < relay_packet.Length - 1; x++) //Checksum
			{
				checksum ^= relay_packet[x];
			}

			relay_packet[rp_index] = checksum;


			return relay_packet;
		} //END BuildRelayPacket


		public byte[] BuildWifiPacket( string p_ssid, string p_pass )
		{
			byte[] wifi_packet = null;


			return wifi_packet;
		}




	} //END class BP_Packets
}
