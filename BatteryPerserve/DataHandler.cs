using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryPerserve
{
	public enum CollectionStatus
	{
		COLLECT_UNINITIALIZED = 0,
		COLLECT_SEACHING,
		COLLECT_COLLECTING,
		COLLECT_FOUND_CS,
		COLLECT_FOUND,
		COLLECT_INVALID,
		COLLECT_INVALID_CS,
		COLLECT_VALID_CS,
		COLLECT_PARSED,
	};

	public enum FieldType
	{
		BYTE1 = 0,
		BYTE2,
		BYTE4,
		BYTE8,
		BYTE16,
		BYTE32,
		BYTE1_VAR_SIZE,
		BYTE2_VAR_SIZE,
		BYTE4_VAR_SIZE,
		BYTE8_VAR_SIZE,
		BYTE1_CHECKSUM,
		BYTE2_CHECKSUM,
		BYTE4_CHECKSUM,
		BYTE8_CHECKSUM,

	};

	public enum Endian
	{
		BigEndian = 0,
		LittleEndian,
	};

	public enum CacheDataSizes
	{
		CACHE500SIZE = 500,
		CACHE1000SIZE = 1000,
		CACHE2000SIZE = 2000,
		CACHE10000SIZE = 10000,

	};


	public class ByteManipulator
	{
		//Helper Functions:
		public static UInt16 Mesh2Bytes(	byte byte1, byte byte2,
											Endian EndianType )
		{
			UInt16 ui16_temp = 0;

			if (Endian.BigEndian == EndianType)
			{
				ui16_temp = (UInt16)(	(((UInt16)byte1) << 8) & 0xFF00 |
										((UInt16)byte2) & 0x00FF);
			}
			else if (Endian.LittleEndian == EndianType)
			{
				ui16_temp = (UInt16)(	(((UInt16)byte2) << 8) & 0xFF00 |
										((UInt16)byte1) & 0x00FF);
			}

			return ui16_temp;
		} //END Mesh2Bytes

		public static UInt32 Mesh4Bytes(	byte byte1, byte byte2,
											byte byte3, byte byte4,
											Endian EndianType )
		{
			UInt32 ui32_temp = 0;

			if (Endian.BigEndian == EndianType)
			{
				ui32_temp = (UInt32)(	(((UInt32)byte1) << 24) & 0xFF000000 |
										(((UInt32)byte2) << 16) & 0x00FF0000 |
										(((UInt32)byte3) << 8) & 0x0000FF00 |
										((UInt32)byte4) & 0x000000FF);
			}
			else if (Endian.LittleEndian == EndianType)
			{
				ui32_temp = (UInt32)(	(((UInt32)byte4) << 24) & 0xFF000000 |
										(((UInt32)byte3) << 16) & 0x00FF0000 |
										(((UInt32)byte2) << 8) & 0x0000FF00 |
										((UInt32)byte1) & 0x000000FF);
			}

			return ui32_temp;
		} //END Mesh4Bytes


		public static UInt64 Mesh8Bytes(	byte byte1, byte byte2, byte byte3, byte byte4,
											byte byte5, byte byte6, byte byte7, byte byte8,
											Endian EndianType )
		{
			UInt64 ui64_temp = 0;

			if (Endian.BigEndian == EndianType)
			{
				ui64_temp = (UInt64)(	(((UInt64)byte1) << 56) & 0xFF00000000000000 |
										(((UInt64)byte2) << 48) & 0x00FF000000000000 |
										(((UInt64)byte3) << 40) & 0x0000FF0000000000 |
										(((UInt64)byte4) << 32) & 0x000000FF00000000 |
										(((UInt64)byte5) << 24) & 0x00000000FF000000 |
										(((UInt64)byte6) << 16) & 0x0000000000FF0000 |
										(((UInt64)byte7) << 8) & 0x000000000000FF00 |
										((UInt64)byte8) & 0x00000000000000FF);
			}
			else if (Endian.LittleEndian == EndianType)
			{
				ui64_temp = (UInt64)(	(((UInt64)byte8) << 56) & 0xFF00000000000000 |
										(((UInt64)byte7) << 48) & 0x00FF000000000000 |
										(((UInt64)byte6) << 40) & 0x0000FF0000000000 |
										(((UInt64)byte5) << 32) & 0x000000FF00000000 |
										(((UInt64)byte4) << 24) & 0x00000000FF000000 |
										(((UInt64)byte3) << 16) & 0x0000000000FF0000 |
										(((UInt64)byte2) << 8) & 0x000000000000FF00 |
										((UInt64)byte1) & 0x00000000000000FF);
			}

			return ui64_temp;
		} //END Mesh8Bytes

		public static void Split2Bytes(	UInt16 split, ref byte byte1, ref byte byte2,
										Endian EndianType )
		{
			if (Endian.BigEndian == EndianType)
			{
				byte1 = (byte)((split & 0xFF00) >> 8);
				byte2 = (byte)(split & 0x00FF);
			}
			else if (Endian.LittleEndian == EndianType)
			{
				byte1 = (byte)(split & 0x00FF);
				byte2 = (byte)((split & 0xFF00) >> 8);
			}

		} //END Split2Bytes


		public static void Split4Bytes(	UInt32 split,	ref byte byte1, ref byte byte2,
														ref byte byte3, ref byte byte4,
										Endian EndianType )
		{
			if (Endian.BigEndian == EndianType)
			{
				byte1 = (byte)((split & 0xFF000000) >> 24);
				byte2 = (byte)((split & 0x00FF0000) >> 16);
				byte3 = (byte)((split & 0x0000FF00) >> 8);
				byte4 = (byte)(split & 0x000000FF);
			}
			else if (Endian.LittleEndian == EndianType)
			{
				byte1 = (byte)(split & 0x000000FF);
				byte2 = (byte)((split & 0x0000FF00) >> 8);
				byte3 = (byte)((split & 0x00FF0000) >> 16);
				byte4 = (byte)((split & 0xFF000000) >> 24);
			}

		} //END Split4Bytes


		public static void Split8Bytes(	UInt64 split,	ref byte byte1, ref byte byte2,
														ref byte byte3, ref byte byte4,
														ref byte byte5, ref byte byte6,
														ref byte byte7, ref byte byte8,
										Endian EndianType )
		{
			if (Endian.BigEndian == EndianType)
			{
				byte1 = (byte)((split & 0xFF00000000000000) >> 56);
				byte2 = (byte)((split & 0x00FF000000000000) >> 48);
				byte3 = (byte)((split & 0x0000FF0000000000) >> 40);
				byte4 = (byte)((split & 0x000000FF00000000) >> 32);
				byte5 = (byte)((split & 0x00000000FF000000) >> 24);
				byte6 = (byte)((split & 0x0000000000FF0000) >> 16);
				byte7 = (byte)((split & 0x000000000000FF00) >> 8);
				byte8 = (byte)(split & 0x00000000000000FF);
			}
			else if (Endian.LittleEndian == EndianType)
			{
				byte1 = (byte)(split & 0x00000000000000FF);
				byte2 = (byte)((split & 0x000000000000FF00) >> 8);
				byte3 = (byte)((split & 0x0000000000FF0000) >> 16);
				byte4 = (byte)((split & 0x00000000FF000000) >> 24);
				byte5 = (byte)((split & 0x000000FF00000000) >> 32);
				byte6 = (byte)((split & 0x0000FF0000000000) >> 40);
				byte7 = (byte)((split & 0x00FF000000000000) >> 48);
				byte8 = (byte)((split & 0xFF00000000000000) >> 56);
			}

		} //END Split8Bytes


	} //END ByteManipulator


	public class Field
	{
		public UInt64 ui64_field;
		public byte[] array_field;

		public Field( int size )
		{
			ui64_field = 0;
			array_field = new byte[size];
		}

		//public void SetArray( int size )
		//{
		//	array_field = new byte[size];
		//}

		public void Reset()
		{
			ui64_field = 0;

			if (array_field != null)
			{
				for (int x = 0; x < array_field.Length; x++)
				{
					array_field[x] = 0;
				}
			}
		} //END Reset
	} //END class Field


	public class Collector
	{
		//Private:
		private UInt64 prefix;
		private UInt64 prefix_checksum;
		private UInt64 cache_prefix;
		private FieldType[] field_types;
		private UInt64 fields_size;
		private UInt16 field_checksum_prefix_index;

		//Public:
		public CollectionStatus status;
		public byte[] cache_data;
		public UInt64 cache_data_size;
		public UInt64 cache_data_index;
		public UInt64 packet_size;
		public Field[] fields;



		public Collector(	UInt64 p_prefix, UInt64 p_prefix_checksum,
							UInt64 p_packet_size, FieldType[] p_fields,
							UInt64 p_fields_size, UInt16 p_field_checksum_prefix_index )
		{
			if (0 != p_prefix)
			{
				if (0 != p_prefix_checksum || 0 != p_packet_size)
				{
					prefix = p_prefix;
					prefix_checksum = p_prefix_checksum;
					packet_size = p_packet_size;

					if (null != p_fields && 0 != p_fields_size)
					{
						field_types = p_fields;
						fields_size = p_fields_size;
						field_checksum_prefix_index = p_field_checksum_prefix_index;
						fields = new Field[fields_size];
						for (UInt64 x = 0; null != fields && x < fields_size; x++)
							fields[x] = new Field( (int)CacheDataSizes.CACHE500SIZE );
					}
					else
					{
						field_types = null;
					}

					//Defaults:
					cache_prefix = 0;
					status = CollectionStatus.COLLECT_SEACHING;
					cache_data = new byte[(int)CacheDataSizes.CACHE500SIZE];
					cache_data_size = 0;
					cache_data_index = 0;
				}
			}
			else
			{
				prefix = 0;
				prefix_checksum = 0;
				packet_size = 0;
				status = CollectionStatus.COLLECT_UNINITIALIZED;
				field_types = null;
			}



		} //END Constructor

		~Collector()
		{

		}

		public void ResetCache()
		{
			if (null != cache_data)
			{
				for (int x = 0; x < cache_data.Length; x++)
					cache_data[x] = 0;
			}

			if (null != fields)
			{
				for (UInt64 x = 0; x < fields_size; x++)
					fields[x].Reset();
			}


			status = CollectionStatus.COLLECT_SEACHING;
			cache_prefix = 0;
			cache_data_size = 0;
			cache_data_index = 0;
		} //END ResetCache

		public void Collect(ref byte[] buffer, ref UInt64 buff_size )
		{
			if (CollectionStatus.COLLECT_UNINITIALIZED == status)
			{
				//Serial.println( "Collector is uninitialized!" );
			}
			else if (null == buffer && 0 == buff_size)
			{
				//Serial.println( "NULL buffer or 0 buffer size!" );
			}
			else
			{
				byte[] buff = buffer;
				UInt64 buff_left = buff_size;
				int buff_index = 0;
				byte val;


				while (buff_left > 0 && CollectionStatus.COLLECT_SEACHING == status)
				{
					val = buff[buff_index];
					cache_prefix |= (UInt64)val;


					if (FieldType.BYTE1 == field_types[0] &&
						(cache_prefix & 0x000000FF) == prefix)
					{
						cache_data[0] = (byte)(prefix & 0x000000FF);
						cache_data_index++;
						cache_prefix = 0;
						status = CollectionStatus.COLLECT_COLLECTING;
					}
					else if (FieldType.BYTE2 == field_types[0] &&
							(cache_prefix & 0x0000FFFF) == prefix)
					{
						cache_data[0] = (byte)((prefix & 0x0000FF00) >> 8);
						cache_data[1] = (byte)(prefix & 0x000000FF);
						cache_data_index += 2;
						cache_prefix = 0;
						status = CollectionStatus.COLLECT_COLLECTING;
					}
					else if (FieldType.BYTE4 == field_types[0] &&
							(cache_prefix & 0xFFFFFFFF) == prefix)
					{
						cache_data[0] = (byte)((prefix & 0xFF000000) >> 24);
						cache_data[1] = (byte)((prefix & 0x00FF0000) >> 16);
						cache_data[2] = (byte)((prefix & 0x0000FF00) >> 8);
						cache_data[3] = (byte)(prefix & 0x000000FF);
						cache_data_index += 4;
						cache_prefix = 0;
						status = CollectionStatus.COLLECT_COLLECTING;
					}
					else
					{
						cache_prefix <<= 8;
					}

					//Increment:
					buff_index++;
					buff_left--;
				}

				while (buff_left > 0 && CollectionStatus.COLLECT_COLLECTING == status)
				{
					val = buff[buff_index];
					cache_prefix |= (UInt64)val;
					cache_data[cache_data_index] = val;

					if (	(FieldType.BYTE1 == field_types[field_checksum_prefix_index] &&
							(cache_prefix & 0x000000FF) == prefix_checksum) ||
							(FieldType.BYTE2 == field_types[field_checksum_prefix_index] &&
							(cache_prefix & 0x0000FFFF) == prefix_checksum) ||
							(FieldType.BYTE4 == field_types[field_checksum_prefix_index] &&
							(cache_prefix & 0xFFFFFFFF) == prefix_checksum)	)
					{
						status = CollectionStatus.COLLECT_FOUND_CS;
					}
					else if (cache_data_index == packet_size)
					{
						status = CollectionStatus.COLLECT_FOUND;
					}
					else
					{
						cache_prefix <<= 8;
					}

					cache_data_index++;

					//Increment:
					buff_index++;
					buff_left--;
				}

				if (buff_left > 0 && CollectionStatus.COLLECT_FOUND_CS == status)
				{
					val = buff[buff_index];
					cache_data[cache_data_index] = val;
					cache_data_index++;
					packet_size = cache_data_index;
					status = CollectionStatus.COLLECT_FOUND;

					//Increment:
					buff_index++;
					buff_left--;
				}


				buff_size = buff_left;
			}
		} //END Collect


		public void CheckChecksum()
		{
			byte checksum = 0x00;
			for (UInt64 x = 0; x < packet_size - 1; x++) //Fix???????
			{
				checksum ^= cache_data[x];
			}

			if (checksum == cache_data[packet_size - 1])
			{
				status = CollectionStatus.COLLECT_VALID_CS;
			}
			else
			{
				status = CollectionStatus.COLLECT_INVALID_CS;
			}
		} //END CheckChecksum


		public void Parse()
		{
			if (CollectionStatus.COLLECT_UNINITIALIZED == status)
			{
				//Serial.println( "Collector is uninitialized!" );
			}
			else if (null == field_types)
			{
				//Serial.println( "NULL fields!" );
			}
			else if (CollectionStatus.COLLECT_FOUND != status && CollectionStatus.COLLECT_VALID_CS != status)
			{
				//Serial.println( "Have not found a complete message!" );
			}
			else
			{
				UInt64 index;
				UInt64 field_index;
				byte ui8_temp = 0;
				UInt16 ui16_temp = 0;
				UInt32 ui32_temp = 0;
				UInt64 ui64_temp = 0;


				for (index = 0, field_index = 0; field_index < fields_size; field_index++)
				{
					ui64_temp = 0;
					fields[field_index].Reset();

					if (FieldType.BYTE1 == field_types[field_index] ||
						FieldType.BYTE1_CHECKSUM == field_types[field_index])
					{
						ui8_temp = 0;
						ui8_temp = cache_data[index];
						ui64_temp = (UInt64)ui8_temp;
						index += 1;
					}
					else if (FieldType.BYTE2 == field_types[field_index])
					{
						ui16_temp = ByteManipulator.Mesh2Bytes( cache_data[index], cache_data[index + 1], Endian.BigEndian );
						ui64_temp = (UInt64)ui16_temp;
						index += 2;
					}
					else if (FieldType.BYTE4 == field_types[field_index])
					{
						ui32_temp = ByteManipulator.Mesh4Bytes( cache_data[index], cache_data[index + 1],
																cache_data[index + 2], cache_data[index + 3],										Endian.BigEndian );
						ui64_temp = (UInt64)ui32_temp;
						index += 4;
					}
					else if (FieldType.BYTE8 == field_types[field_index])
					{
						ui64_temp = ByteManipulator.Mesh8Bytes( cache_data[index], cache_data[index + 1],
																cache_data[index + 2], cache_data[index + 3],
																cache_data[index + 4], cache_data[index + 5],
																cache_data[index + 6], cache_data[index + 7],										Endian.BigEndian );
						index += 8;
					}
					else if (FieldType.BYTE16 == field_types[field_index])
					{
						ui64_temp = 16;
						for (UInt64 x = 0; x < ui64_temp; x++)
							fields[field_index].array_field[x] = cache_data[index + x];
						index += 16;
					}
					else if (FieldType.BYTE32 == field_types[field_index])
					{
						ui64_temp = 32;
						for (UInt64 x = 0; x < ui64_temp; x++)
							fields[field_index].array_field[x] = cache_data[index + x];
						index += 32;
					}
					else if (FieldType.BYTE1_VAR_SIZE == field_types[field_index])
					{
						ui8_temp = 0;
						ui8_temp = cache_data[index];
						ui64_temp = (UInt64)ui8_temp;
						index += 1;
						for (UInt64 x = 0; x < ui64_temp; x++)
							fields[field_index].array_field[x] = cache_data[index + x];
						index += ui8_temp;
					}
					else if (FieldType.BYTE2_VAR_SIZE == field_types[field_index])
					{
						ui16_temp = ByteManipulator.Mesh2Bytes( cache_data[index], cache_data[index + 1],												Endian.LittleEndian );
						ui64_temp = (UInt64)ui16_temp;
						index += 2;
						for (UInt64 x = 0; x < ui64_temp; x++)
							fields[field_index].array_field[x] = cache_data[index + x];
						index += ui16_temp;
					}
					else if (FieldType.BYTE4_VAR_SIZE == field_types[field_index])
					{
						ui32_temp = ByteManipulator.Mesh4Bytes( cache_data[index], cache_data[index + 1],
																cache_data[index + 2], cache_data[index + 3],										Endian.LittleEndian );
						ui64_temp = (UInt64)ui32_temp;
						index += 4;
						for (UInt64 x = 0; x < ui64_temp; x++)
							fields[field_index].array_field[x] = cache_data[index + x];
						index += ui32_temp;
					}
					else if (FieldType.BYTE8_VAR_SIZE == field_types[field_index])
					{
						ui64_temp = ByteManipulator.Mesh8Bytes( cache_data[index], cache_data[index + 1],
																cache_data[index + 2], cache_data[index + 3],
																cache_data[index + 4], cache_data[index + 5],
																cache_data[index + 6], cache_data[index + 7],										Endian.LittleEndian );
						index += 8;
						for (UInt64 x = 0; x < ui64_temp; x++)
							fields[field_index].array_field[x] = cache_data[index + x];
						index += ui64_temp;
					}

					fields[field_index].ui64_field = ui64_temp;
				}

				status = CollectionStatus.COLLECT_PARSED;
			}
		} //END Parse



	} //END class Colllector




} //END namespce
