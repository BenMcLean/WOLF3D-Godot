using System.Collections.Generic;
using System.IO;

namespace WOLF3DSim
{
    public class GameMaps
    {
        public long[] Offsets { get; set; }

        public struct Map
        {
            public string Name { get; set; }
            public long MapOffset { get; set; }
            public long ObjectOffset { get; set; }
            public long OtherOffset { get; set; }
            public ushort MapByteSize { get; set; }
            public ushort ObjectByteSize { get; set; }
            public ushort OtherByteSize { get; set; }
            public ushort Width { get; set; }
            public ushort Depth { get; set; }
            public ushort[] MapData { get; set; }
            public ushort[] ObjectData { get; set; }
            public ushort[] OtherData { get; set; }
            public bool IsCarmackized { get; set; }

            public ushort X(uint i)
            {
                return (ushort)(i % Width);
            }

            public ushort Z(uint i)
            {
                return (ushort)(i / Depth);
            }

            public Map StartPosition(out ushort x, out ushort z)
            {
                //19,"Start position/North",
                //20,"Start position/East",
                //21,"Start position/South",
                //22,"Start position/West",
                for (uint i = 0; i < ObjectData.Length; i++)
                    if (ObjectData[i] >= 19 && ObjectData[i] <= 22)
                    {
                        x = (ushort)(i / Width);
                        z = (ushort)(i % Depth);
                        return this;
                    }
                throw new InvalidDataException("Map \"" + Name + "\" has no starting position!");
            }
        }

        public Map[] Maps { get; set; }

        public GameMaps(Stream mapHead, Stream gameMaps)
        {
            // Read in MAPHEAD
            if (mapHead.ReadWord() != 0xABCD)
                throw new InvalidDataException("File \"" + mapHead + "\" has invalid signature code!");
            List<long> offsets = new List<long>();
            uint mapHeadOffset;
            while ((mapHeadOffset = mapHead.ReadDWord()) != 0 && mapHead.Position < mapHead.Length)
                offsets.Add(mapHeadOffset);
            Offsets = offsets.ToArray();

            // Read in GAMEMAPS
            List<Map> maps = new List<Map>();
            foreach (long offset in Offsets)
            {
                gameMaps.Seek(offset, 0);
                Map map = new Map
                {
                    MapOffset = gameMaps.ReadDWord(),
                    ObjectOffset = gameMaps.ReadDWord(),
                    OtherOffset = gameMaps.ReadDWord(),
                    MapByteSize = gameMaps.ReadWord(),
                    ObjectByteSize = gameMaps.ReadWord(),
                    OtherByteSize = gameMaps.ReadWord(),
                    Width = gameMaps.ReadWord(),
                    Depth = gameMaps.ReadWord()
                };
                char[] name = new char[16];
                for (uint i = 0; i < name.Length; i++)
                    name[i] = (char)gameMaps.ReadByte();
                map.Name = new string(name);

                char[] carmackized = new char[4];
                for (uint i = 0; i < carmackized.Length; i++)
                    carmackized[i] = (char)gameMaps.ReadByte();
                map.IsCarmackized = new string(carmackized).Equals("!ID!");

                // "Note that for Wolfenstein 3D, a 4-byte signature string ("!ID!") will normally be present directly after the level name. The signature does not appear to be used anywhere, but is useful for distinguishing between v1.0 files (the signature string is missing), and files for v1.1 and later (includes the signature string)."
                // "Note that for Wolfenstein 3D v1.0, map files are not carmackized, only RLEW compression is applied."
                // http://www.shikadi.net/moddingwiki/GameMaps_Format#Map_data_.28GAMEMAPS.29
                // Carmackized game maps files are external GAMEMAPS.xxx files and the map header is stored internally in the executable. The map header must be extracted and the game maps decompressed before TED5 can access them. TED5 itself can produce carmackized files and external MAPHEAD.xxx files. Carmackization does not replace the RLEW compression used in uncompressed data, but compresses this data, that is, the data is doubly compressed.

                ushort[] mapData;
                gameMaps.Seek(map.MapOffset, 0);
                if (map.IsCarmackized)
                    mapData = CarmackExpand(gameMaps);
                else
                {
                    mapData = new ushort[map.MapByteSize / 2];
                    for (uint i = 0; i < mapData.Length; i++)
                        mapData[i] = gameMaps.ReadWord();
                }
                map.MapData = RlewExpand(mapData, (ushort)(map.Depth * map.Width), 0xABCD);

                ushort[] objectData;
                gameMaps.Seek(map.ObjectOffset, 0);
                if (map.IsCarmackized)
                    objectData = CarmackExpand(gameMaps);
                else
                {
                    objectData = new ushort[map.ObjectByteSize / 2];
                    for (uint i = 0; i < objectData.Length; i++)
                        objectData[i] = gameMaps.ReadWord();
                }
                map.ObjectData = RlewExpand(objectData, (ushort)(map.Depth * map.Width), 0xABCD);

                ushort[] otherData;
                gameMaps.Seek(map.OtherOffset, 0);
                if (map.IsCarmackized)
                    otherData = CarmackExpand(gameMaps);
                else
                {
                    otherData = new ushort[map.OtherByteSize / 2];
                    for (uint i = 0; i < otherData.Length; i++)
                        otherData[i] = gameMaps.ReadWord();
                }
                map.OtherData = RlewExpand(otherData, (ushort)(map.Depth * map.Width), 0xABCD);

                maps.Add(map);
            }
            Maps = maps.ToArray();
        }

        #region Decompression algorithms
        private static readonly ushort CARMACK_NEAR = 0xA7;
        private static readonly ushort CARMACK_FAR = 0xA8;

        public static ushort[] RlewExpand(ushort[] carmackExpanded, ushort length, ushort tag)
        {
            ushort[] rawMapData = new ushort[length];
            int src_index = 1, dest_index = 0;
            do
            {
                ushort value = carmackExpanded[src_index++]; // WORDS!!
                if (value != tag)
                    // uncompressed
                    rawMapData[dest_index++] = value;
                else
                {
                    // compressed string
                    ushort count = carmackExpanded[src_index++];
                    value = carmackExpanded[src_index++];
                    for (ushort i = 1; i <= count; i++)
                        rawMapData[dest_index++] = value;
                }
            } while (dest_index < length);
            return rawMapData;
        }

        public static ushort[] CarmackExpand(Stream stream)
        {
            ////////////////////////////
            // Get to the correct chunk
            ushort ch, chhigh, count, offset, index;
            // First word is expanded length
            ushort length = stream.ReadWord();
            ushort[] expandedWords = new ushort[length]; // array of WORDS
            length /= 2;
            index = 0;
            while (length > 0)
            {
                ch = stream.ReadWord();
                chhigh = (ushort)(ch >> 8);
                if (chhigh == CARMACK_NEAR)
                {
                    count = (ushort)(ch & 0xFF);
                    if (count == 0)
                    {
                        ch |= (ushort)stream.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = (ushort)stream.ReadByte();
                        length -= count;
                        if (length < 0)
                        {
                            return expandedWords;
                        }
                        while ((count--) > 0)
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
                        ch |= (ushort)stream.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = (ushort)stream.ReadWord();
                        length -= count;
                        if (length < 0)
                        {
                            return expandedWords;
                        }
                        while ((count--) > 0)
                        {
                            expandedWords[index++] = expandedWords[offset++];
                        }
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
        #endregion
    }
}
