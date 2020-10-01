using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace BatteryPerserve
{
	//Class Helper Classes -----------------------------------------------------------
	[Serializable()]
	public struct Settings_BatteryOptimizer
	{
		public bool     auto_connect;
		public bool     start_minimized;
		//public string LastCom;
		public bool     optimize_schedule;
		public DateTime start_charge_time;
		public DateTime stop_charge_time;
		public decimal  battery_range_min;
		public decimal  battery_range_max;
	}

	public class BP_WIFI_Packet //Total bytes:  
	{
		private byte prefix; // 0xB1

		public BP_WIFI_Packet()
		{
			prefix = 0xB1;
		}

		//public byte[] extract( string p_ssid, string p_pass, byte[] p_key )
		//{
		//	byte[]		packet;
		//	ushort		encrypt_len;
		//	char[]		to_encrypt	= '$' + p_ssid.ToArray() + '!' + p_pass;
		//	byte[]		encrypted;
		//	Aes			aes			= Aes.Create();
		//	BP_Helper	helper		= new BP_Helper();

		//	//Encrypt:
		//	encrypted = helper.EncryptStringToBytes_Aes(to_encrypt, p_key, aes.IV);
		//	encrypt_len = (UInt16)encrypted.Length;

		//	//Create Packet:
		//	packet = new byte[3 + encrypted.Length];
		//	packet[0] = prefix;
		//	packet[1] = (byte)(encrypt_len & 0xFF);
		//	packet[2] = (byte)((encrypt_len & 0xFF00) >> 4);
		//	encrypted.CopyTo( packet, 3 );

		//	return packet;
		//}

	} //END class BP_WIFI_Packet

	public class BP_Relay_Packet //Total bytes
	{
		public byte prefix; // 0xB2
		public byte end_prefix; //0xC2

		public BP_Relay_Packet()
		{
			prefix = 0xB2;
			end_prefix = 0xC2;
		}

		public byte[] extract(byte relay, byte[] p_key , byte[] p_iv) //relay: 0 - Open, 1 - Close
		{
			byte[]		packet;
			ushort		encrypt_len;
			byte[] to_encrypt = new byte[128];//"relay_packet_value".ToArray();
			char[] to_encrypt_char = new char[128];
			string test_data = "AES test data 1234 test. This is long data string test of a message with a higher byte size.. This is the end of the message1234";	
			//test_data.CopyTo( 0, to_encrypt_char, 0, test_data.Length );
			to_encrypt = Encoding.ASCII.GetBytes( test_data );
			//to_encrypt[0] = relay;
			//for (int x = 1; x < 128; x++)
			//	to_encrypt[x] = (byte)(x*2);

			byte[]		encrypted;
			//Aes			aes			= Aes.Create();
			BP_Helper	helper		= new BP_Helper();

			//Encrypt:
			encrypted = helper.EncryptBytes_Aes( to_encrypt, p_key, p_iv );
			encrypt_len = (UInt16)encrypted.Length;

			//Create Packet:
			packet = new byte[3 + encrypted.Length + 1];
			packet[0] = prefix;
			packet[1] = (byte) ((encrypt_len & 0xFF00) >> 4);//(encrypt_len & 0xFF);
			packet[2] = (byte) (encrypt_len & 0xFF);//((encrypt_len & 0xFF00) >> 4);
			encrypted.CopyTo( packet, 3 );
			packet[3 + encrypted.Length] = end_prefix;


			return packet;
		}

	} //END class BP_Relay_Packet



	class BP_Helper
	{
		



		public byte[] EncryptBytes_Aes( byte[] to_encrypt, byte[] Key, byte[] IV )
		{
			// Check arguments.
			if (to_encrypt == null || to_encrypt.Length <= 0)
				throw new ArgumentNullException( "plainText" );
			if (Key == null || Key.Length <= 0)
				throw new ArgumentNullException( "Key" );
			if (IV == null || IV.Length <= 0)
				throw new ArgumentNullException( "IV" );
			byte[] encrypted;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.IV = IV;
				aesAlg.Mode = CipherMode.ECB;
				aesAlg.BlockSize = 128;
				aesAlg.Padding = PaddingMode.Zeros;

				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor( aesAlg.Key, aesAlg.IV );

				// Create the streams used for encryption.
				using (MemoryStream msEncrypt = new MemoryStream())
				{
					using (CryptoStream csEncrypt = new CryptoStream( msEncrypt, encryptor, CryptoStreamMode.Write ))
					{
						csEncrypt.Write( to_encrypt, 0, to_encrypt.Length );
						encrypted = msEncrypt.ToArray();

						//using (StreamWriter swEncrypt = new StreamWriter( csEncrypt ))
						//{
						//	//Write all data to the stream.
						//	swEncrypt.Write( to_encrypt );
						//}
						//encrypted = msEncrypt.ToArray();
					}
				}
			}

			// Return the encrypted bytes from the memory stream.
			return encrypted;
		}

		public byte[] DecryptBytes_Aes( byte[] cipherText, byte[] Key, byte[] IV )
		{
			// Check arguments.
			if (cipherText == null || cipherText.Length <= 0)
				throw new ArgumentNullException( "cipherText" );
			if (Key == null || Key.Length <= 0)
				throw new ArgumentNullException( "Key" );
			if (IV == null || IV.Length <= 0)
				throw new ArgumentNullException( "IV" );

			// Declare the string used to hold
			// the decrypted text.
			byte[] de_crypted = null;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Key;
				aesAlg.IV = IV;
				aesAlg.Mode = CipherMode.ECB;
				aesAlg.BlockSize = 128;
				aesAlg.Padding = PaddingMode.Zeros;

				// Create a decryptor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor( aesAlg.Key, aesAlg.IV );

				// Create the streams used for decryption.
				using (MemoryStream msDecrypt = new MemoryStream( cipherText ))
				{
					using (CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Read ))
					{

						de_crypted = new byte[150];
						csDecrypt.Read( de_crypted, 0, 128 );


						//using (StreamReader srDecrypt = new StreamReader( csDecrypt ))
						//{
						//	//srDecrypt.ReadBlock( de_crypted, 0, (int)srDecrypt.BaseStream.Length );
						//	// Read the decrypted bytes from the decrypting stream
						//	// and place them in a string.
						//	//de_crypted = Encoding.GetEncoding("UTF-8").GetBytes( (srDecrypt.ReadToEnd()).ToArray() );

						//	//de_crypted = srDecrypt.ReadToEnd()).ToArray();


						//}
					}
				}
			}

			return de_crypted;
		}



	}


	



}
