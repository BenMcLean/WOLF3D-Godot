using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3DModel
{
	public struct GameMap
	{
		#region Data
		public string Name { get; private set; }
		public override string ToString() => Name;
		public ushort Number { get; private set; }
		public ushort Width { get; private set; }
		public ushort Depth { get; private set; }
		public ushort[] MapData { get; private set; }
		public ushort[] ObjectData { get; private set; }
		public ushort[] OtherData { get; private set; }
		public ushort X(uint i) => X((ushort)i);
		public ushort X(ushort i) => (ushort)(i % Width);
		public const ushort Y = 0; // Vertical
		public ushort Z(uint i) => Z((ushort)i);
		public ushort Z(ushort i) => (ushort)(i / Depth);
		public ushort GetIndex(uint x, uint z) => GetIndex((ushort)x, (ushort)z);
		public ushort GetIndex(ushort x, ushort z) => (ushort)(z * Depth + x);
		public ushort GetMapData(ushort x, ushort z) => MapData[GetIndex(x, z)];
		public ushort GetMapData(uint x, uint z) => GetMapData((ushort)x, (ushort)z);
		public ushort GetObjectData(uint x, uint z) => GetObjectData((ushort)x, (ushort)z);
		public ushort GetObjectData(ushort x, ushort z) => ObjectData[GetIndex(x, z)];
		public ushort GetOtherData(uint x, uint z) => GetOtherData((ushort)x, (ushort)z);
		public ushort GetOtherData(ushort x, ushort z) => OtherData[GetIndex(x, z)];
		public bool IsWithinMap(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth;
		#endregion Data
		#region Loading
		public static GameMap[] Load(string folder, XElement xml) => Load(
			mapHead: Path.Combine(folder, xml.Element("Maps").Attribute("MapHead").Value),
			gameMaps: Path.Combine(folder, xml.Element("Maps").Attribute("GameMaps").Value)
			);
		public static GameMap[] Load(string mapHead, string gameMaps)
		{
			using (FileStream mapHeadStream = new FileStream(mapHead, FileMode.Open))
			using (FileStream gameMapsStream = new FileStream(gameMaps, FileMode.Open))
				return Load(mapHeadStream, gameMapsStream);
		}
		public static IEnumerable<long> ParseMapHead(Stream stream)
		{
			using (BinaryReader mapHeadReader = new BinaryReader(stream))
			{
				if (mapHeadReader.ReadUInt16() != 0xABCD)
					throw new InvalidDataException("File \"" + stream + "\" has invalid signature code!");
				uint offset;
				while (stream.CanRead && (offset = mapHeadReader.ReadUInt32()) != 0)
					yield return offset;
			}
		}
		public static GameMap[] Load(Stream mapHead, Stream gameMaps) =>
			Load(gameMaps, ParseMapHead(mapHead).ToArray());
		public static GameMap[] Load(Stream gameMaps, params long[] offsets)
		{
			GameMap[] maps = new GameMap[offsets.Length];
			using (BinaryReader gameMapsReader = new BinaryReader(gameMaps))
				for (ushort mapNumber = 0; mapNumber < offsets.Length; mapNumber++)
				{
					long offset = offsets[mapNumber];
					gameMaps.Seek(offset, 0);
					uint mapOffset = gameMapsReader.ReadUInt32(),
						objectOffset = gameMapsReader.ReadUInt32(),
						otherOffset = gameMapsReader.ReadUInt32();
					ushort mapByteSize = gameMapsReader.ReadUInt16(),
						objectByteSize = gameMapsReader.ReadUInt16(),
						otherByteSize = gameMapsReader.ReadUInt16();
					GameMap map = new GameMap
					{
						Number = mapNumber,
						Width = gameMapsReader.ReadUInt16(),
						Depth = gameMapsReader.ReadUInt16(),
					};
					char[] name = new char[16];
					gameMapsReader.Read(name, 0, name.Length);
					map.Name = new string(name).Replace("\0", string.Empty).Trim();
					char[] carmackized = new char[4];
					gameMapsReader.Read(carmackized, 0, carmackized.Length);
					bool isCarmackized = new string(carmackized).Equals("!ID!");
					// "Note that for Wolfenstein 3-D, a 4-byte signature string ("!ID!") will normally be present directly after the level name. The signature does not appear to be used anywhere, but is useful for distinguishing between v1.0 files (the signature string is missing), and files for v1.1 and later (includes the signature string)."
					// "Note that for Wolfenstein 3-D v1.0, map files are not carmackized, only RLEW compression is applied."
					// http://www.shikadi.net/moddingwiki/GameMaps_Format#Map_data_.28GAMEMAPS.29
					// Carmackized game maps files are external GAMEMAPS.xxx files and the map header is stored internally in the executable. The map header must be extracted and the game maps decompressed before TED5 can access them. TED5 itself can produce carmackized files and external MAPHEAD.xxx files. Carmackization does not replace the RLEW compression used in uncompressed data, but compresses this data, that is, the data is doubly compressed.
					ushort[] mapData;
					gameMaps.Seek(mapOffset, 0);
					if (isCarmackized)
						mapData = CarmackExpand(gameMapsReader);
					else
					{
						mapData = new ushort[mapByteSize / 2];
						for (uint i = 0; i < mapData.Length; i++)
							mapData[i] = gameMapsReader.ReadUInt16();
					}
					map.MapData = RlewExpand(mapData, (ushort)(map.Depth * map.Width), 0xABCD);
					ushort[] objectData;
					gameMaps.Seek(objectOffset, 0);
					if (isCarmackized)
						objectData = CarmackExpand(gameMapsReader);
					else
					{
						objectData = new ushort[objectByteSize / 2];
						for (uint i = 0; i < objectData.Length; i++)
							objectData[i] = gameMapsReader.ReadUInt16();
					}
					map.ObjectData = RlewExpand(objectData, (ushort)(map.Depth * map.Width), 0xABCD);
					ushort[] otherData;
					gameMaps.Seek(otherOffset, 0);
					if (isCarmackized)
						otherData = CarmackExpand(gameMapsReader);
					else
					{
						otherData = new ushort[otherByteSize / 2];
						for (uint i = 0; i < otherData.Length; i++)
							otherData[i] = gameMapsReader.ReadUInt16();
					}
					map.OtherData = RlewExpand(otherData, (ushort)(map.Depth * map.Width), 0xABCD);
					maps[mapNumber] = map;
				}
			return maps;
		}
		#endregion Loading
		#region Decompression algorithms
		public const ushort CARMACK_NEAR = 0xA7;
		public const ushort CARMACK_FAR = 0xA8;
		public static ushort[] RlewExpand(ushort[] carmackExpanded, ushort length, ushort tag)
		{
			ushort[] rawMapData = new ushort[length];
			int src_index = 1, dest_index = 0;
			do
			{
				ushort value = carmackExpanded[src_index++]; // WORDS!!
				if (value != tag) // uncompressed
					rawMapData[dest_index++] = value;
				else
				{ // compressed string
					ushort count = carmackExpanded[src_index++];
					value = carmackExpanded[src_index++];
					for (ushort i = 1; i <= count; i++)
						rawMapData[dest_index++] = value;
				}
			} while (dest_index < length);
			return rawMapData;
		}
		public static ushort[] CarmackExpand(BinaryReader binaryReader)
		{
			////////////////////////////
			// Get to the correct chunk
			ushort ch, chhigh, count, offset, index;
			// First word is expanded length
			ushort length = binaryReader.ReadUInt16();
			ushort[] expandedWords = new ushort[length]; // array of WORDS
			length /= 2;
			index = 0;
			while (length > 0)
			{
				ch = binaryReader.ReadUInt16();
				chhigh = (ushort)(ch >> 8);
				if (chhigh == CARMACK_NEAR)
				{
					count = (ushort)(ch & 0xFF);
					if (count == 0)
					{
						ch |= binaryReader.ReadByte();
						expandedWords[index++] = ch;
						length--;
					}
					else
					{
						offset = binaryReader.ReadByte();
						length -= count;
						if (length < 0)
							return expandedWords;
						while (count-- > 0)
						{
							expandedWords[index] = expandedWords[index - offset];
							index++;
						}
					}
				}
				else if (chhigh == CARMACK_FAR)
				{
					count = (ushort)(ch & 0xFF);
					if (count == 0)
					{
						ch |= binaryReader.ReadByte();
						expandedWords[index++] = ch;
						length--;
					}
					else
					{
						offset = binaryReader.ReadUInt16();
						length -= count;
						if (length < 0)
							return expandedWords;
						while (count-- > 0)
							expandedWords[index++] = expandedWords[offset++];
					}
				}
				else
				{
					expandedWords[index++] = ch;
					length--;
				}
			}
			return expandedWords;
		}
		#endregion Decompression algorithms
	}
}
