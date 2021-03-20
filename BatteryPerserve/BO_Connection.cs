using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace BatteryPerserve
{
	public partial class BatteryOptimizer
	{
		public struct BO_Connection
		{
			//UDP:
			//private bool run_udp_loop;
			public const int rec_listenPort = 8011;
			public const int send_listenPort = 8010;
			public UdpClient udp_client;
			public IPEndPoint rec_end_point;
			public IPEndPoint send_end_point;

			//Packets:
			public string latest_packet_sent;

			//Flags:
			public bool received_response;
		}

		BO_Connection bo_connection;


		private void BO_Connection_Initialize()
		{
			//Start UDP server to listen in for the Battery Optimizer device:
			bo_connection.udp_client = new UdpClient( BO_Connection.rec_listenPort );
			bo_connection.rec_end_point = new IPEndPoint(	IPAddress.Any,
															BO_Connection.rec_listenPort );
			bo_connection.send_end_point = new IPEndPoint(	IPAddress.Parse("239.5.6.7"),
															BO_Connection.send_listenPort );

			//udp_client.EnableBroadcast = true;
			//udp_client.JoinMulticastGroup( IPAddress.Parse( "239.5.6.7"), IPAddress.Any);
			//udp_client.MulticastLoopback = true;

			bo_connection.latest_packet_sent = "";
			bo_connection.received_response = false;
		}

		private void UDP_ReceiveContinuously( IAsyncResult res )
		{
			byte[] received = bo_connection.udp_client.EndReceive( res, ref bo_connection.rec_end_point );

			//Process Data:
			UDP_ProcessData( ref received );

			//Set up to receive again:
			UDP_SetupReceive();
		} //END UDP_ReceiveContinuously

		private void UDP_SetupReceive()
		{
			try
			{
				bo_connection.udp_client.BeginReceive( new AsyncCallback( UDP_ReceiveContinuously ), null );
			}
			catch (Exception e)
			{
				MessageBox.Show( e.ToString() );
			}
		} //END UDP_SetupReceive


		private void UDP_ExecuteCommand( byte msg_type, ref byte[] command )
		{

			if (msg_type == packet_manager.msg_type_relay)
			{
				if (0x01 == command[0] || 0x02 == command[0])
				{
					relay_status = command[0];
				}

			}
			else if (msg_type == packet_manager.msg_type_wifi)
			{

			}
			else if (msg_type == packet_manager.msg_type_name)
			{


			}
			else if (msg_type == packet_manager.msg_type_response)
			{
				string temp = System.Text.Encoding.UTF8.GetString( command ); //clean up??
				string temp2 = temp.Trim();


				if (temp2 == bo_connection.latest_packet_sent)
				{

					//MessageBox.Show( "Success!!!" );
					if (temp2 == encdec_resources.associated_data[1]) //Device has received the WiFi packet
					{
						UpdateDeviceStatus( DeviceStatus.FOUND );
						//switch wifi here??
					}
					else if (temp2 == encdec_resources.associated_data[4])
					{
						UpdateDeviceStatus( DeviceStatus.CONNECTED );
					}

					bo_connection.latest_packet_sent = "";
					bo_connection.received_response = true;
				}


			}

		} //END UDP_ExecuteCommand


		private void UDP_ProcessData(ref byte[] data)
		{
			UInt64 data_size = (UInt64)data.Length;

			packet_collector.Collect( ref data, ref data_size );

			if (packet_collector.status == CollectionStatus.COLLECT_FOUND)
			{
				packet_collector.CheckChecksum();

				if (packet_collector.status == CollectionStatus.COLLECT_VALID_CS)
				{
					packet_collector.Parse();

					if (packet_collector.status == CollectionStatus.COLLECT_PARSED)
					{
						byte msg_type = (byte)packet_collector.fields[1].ui64_field;
						byte[] iv = new byte[encdec_resources.gcm256.IVByteSize];
						byte[] enc_data_mac;
						byte[] ass_data;
						byte[] decrypted_data;


						if (msg_type >= 0 && msg_type <= 9)
						{
							//Process data:
							for (int x = 0; x < encdec_resources.gcm256.IVByteSize; x++)
								iv[x] = (byte)packet_collector.fields[2].array_field[x];


							enc_data_mac = new byte[packet_collector.fields[3].ui64_field];

							for (int x = 0; x < (int)packet_collector.fields[3].ui64_field; x++)
								enc_data_mac[x] = (byte)packet_collector.fields[3].array_field[x];

							ass_data = Encoding.UTF8.GetBytes( encdec_resources.associated_data[msg_type] );

							decrypted_data = encdec_resources.gcm256.Decrypt( enc_data_mac,
																				encdec_resources.aes_key,
																				iv,
																				ass_data );

							//Execute Command:
							if (null != decrypted_data)
							{
								UDP_ExecuteCommand( msg_type, ref decrypted_data );
							}
						}


					} //END PARSED
				} //END VALID CS
				packet_collector.ResetCache();
			} //END FOUND


		} //END UDP_ProcessData


		/*
		 * 0x00 Relay
		 * 0x01 Wifi
		 * 0x02 Device Name
		 * 0x03 Response
		 */
		private void UDP_SendToClient(byte msg_type)
		{
			Settings_BatteryOptimizer BatOpSettings = RetrieveSettings();
			byte[] packet = null;

			if (msg_type == packet_manager.msg_type_relay)
			{
				//Encrypt:
				packet = packet_manager.BuildRelayPacket(	BatOpSettings.device_id,
															relay_status,
															ref encdec_resources );
			}
			else if (msg_type == packet_manager.msg_type_wifi)
			{
				//Get Wifi info to send:
				form_wifi_profiles.ShowDialog();
				form_wifi_profiles.Activate();
				List<string> wifi_profile = form_wifi_profiles.wp_profile;


				if (null != wifi_profile &&
					2 == wifi_profile.Count &&
					null != wifi_profile[0] &&
					null != wifi_profile[1] &&
					"" != wifi_profile[0] &&
					"" != wifi_profile[1])
				{
					//Set WiFi sent, so we know what WiFi to switch back to
					wifi_profile_sent = wifi_profile[0];

					//Create packet & Encrypt:
					packet = packet_manager.BuildWifiPacket(BatOpSettings.device_id,
															wifi_profile[0],
															wifi_profile[1],
															ref encdec_resources );
				}
			}
			else if (msg_type == packet_manager.msg_type_name)
			{

			}
			else if (msg_type == packet_manager.msg_type_response)
			{

			}
			else if (msg_type == packet_manager.msg_type_status)
			{
				packet = packet_manager.BuildStatusPacket(	BatOpSettings.device_id,
															device_status,
															ref encdec_resources );
			}


			//Send Packet:
			if (packet != null)
			{
				bo_connection.udp_client.Send( packet, packet.Length, bo_connection.send_end_point );
				//Set latest packet sent:
				bo_connection.latest_packet_sent = encdec_resources.associated_data[msg_type];
			}

		} //END UDP_ClientLoop




	} //END class
}
