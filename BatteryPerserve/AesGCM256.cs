using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace BatteryPerserve
{
	public class AesGcm256
	{
		private static readonly SecureRandom Random = new SecureRandom();

		// Pre-configured Encryption Parameters
		public readonly int IVBitSize = 128;
		public readonly int MacBitSize = 128;
		public readonly int KeyBitSize = 256;


		public byte[] NewKey()
		{
			var key = new byte[KeyBitSize / 8];
			Random.NextBytes( key );
			return key;
		}

		public byte[] NewIv()
		{
			var iv = new byte[IVBitSize / 8];
			Random.NextBytes( iv );
			return iv;
		}

		public byte[] Encrypt( byte[] to_encrypt, byte[] key, byte[] iv, byte[] associated_data)
		{
			byte[] encryptedBytes = null;

			try
			{
				GcmBlockCipher cipher = new GcmBlockCipher( new AesEngine() ); //AesFastEngine
				AeadParameters parameters = new AeadParameters( new KeyParameter( key ), MacBitSize, iv, associated_data );

				cipher.Init( true, parameters );

				encryptedBytes = new byte[cipher.GetOutputSize( to_encrypt.Length )];
				Int32 retLen = cipher.ProcessBytes( to_encrypt, 0, to_encrypt.Length, encryptedBytes, 0 );
				cipher.DoFinal( encryptedBytes, retLen );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );
			}

			//Tag is appened to encryptedBytes
			return encryptedBytes;
		}

		public byte[] Decrypt( byte[] to_decrypt, byte[] key, byte[] iv, byte[] associated_data)
		{
			byte[] decryptedBytes = null;

			try
			{
				GcmBlockCipher cipher = new GcmBlockCipher( new AesEngine() );
				AeadParameters parameters = new AeadParameters( new KeyParameter( key ), MacBitSize, iv, associated_data );
				//ParametersWithIV parameters = new ParametersWithIV(new KeyParameter(key), iv);

				cipher.Init( false, parameters );
				decryptedBytes = new byte[cipher.GetOutputSize( to_decrypt.Length )];


				Int32 retLen = cipher.ProcessBytes( to_decrypt, 0, to_decrypt.Length, decryptedBytes, 0 );
				cipher.DoFinal( decryptedBytes, retLen );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );
			}

			return decryptedBytes;
		}


		public static string Encode64( byte[] to_encode )
		{
			string encoded64 = "";

			try
			{
				encoded64 = Convert.ToBase64String( to_encode, Base64FormattingOptions.None );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );

			}

			return encoded64;
		}

		public static byte[] Decode64( string to_decode )
		{
			byte[] decoded64 = null;

			try
			{
				decoded64 = Convert.FromBase64String( to_decode );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );

			}

			return decoded64;
		}

	} //END class AesGcm256
}
