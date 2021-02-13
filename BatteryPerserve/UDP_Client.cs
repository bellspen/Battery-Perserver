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
	partial class BatteryOptimizer
	{
		//UDP:
		private bool run_udp_loop;
		private const int rec_listenPort = 8011;
		private const int send_listenPort = 8010;
		private UdpClient udp_client;
		private IPEndPoint rec_end_point;
		private IPEndPoint send_end_point;

		//Packets:
		private string latest_packet_sent;

		private void UDP_Initialize_Client()
		{
			//Start UDP server to listen in for the Battery Optimizer device:
			udp_client = new UdpClient( rec_listenPort );
			rec_end_point = new IPEndPoint( IPAddress.Broadcast, rec_listenPort );
			send_end_point = new IPEndPoint( IPAddress.Broadcast, send_listenPort );

			udp_client.EnableBroadcast = true;
			udp_client.MulticastLoopback = false;
			latest_packet_sent = "";
		}

		private void UDP_ReceiveContinuously( IAsyncResult res )
		{
			byte[] received = udp_client.EndReceive( res, ref rec_end_point );

			//Process Data:
			UDP_ProcessData( ref received );


			UDP_SetupReceive();
		} //END UDP_ReceiveContinuously

		private void UDP_SetupReceive()
		{
			try
			{
				udp_client.BeginReceive( new AsyncCallback( UDP_ReceiveContinuously ), null );
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


				if (temp2 == latest_packet_sent)
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

					latest_packet_sent = "";
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

			if (msg_type == packet_manager.msg_type_relay)
			{
				//Encrypt:
				byte[] relay_packet = packet_manager.BuildRelayPacket( relay_status, ref encdec_resources );
				udp_client.Send( relay_packet, relay_packet.Length, send_end_point );

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
					byte[] wifi_packet = packet_manager.BuildWifiPacket(	wifi_profile[0],
																			wifi_profile[1],
																			ref encdec_resources );
					//Send Packet
					udp_client.Send( wifi_packet, wifi_packet.Length, send_end_point );
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
				byte[] packet = packet_manager.BuildStatusPacket(	device_status,
																	ref encdec_resources );
				udp_client.Send( packet, packet.Length, send_end_point );
			}

			//Set latest packet sent:
			latest_packet_sent = encdec_resources.associated_data[msg_type];
		} //END UDP_ClientLoop




	}
}
