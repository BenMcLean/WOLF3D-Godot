using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D
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
            public uint MapByteSize { get; set; }
            public uint ObjectByteSize { get; set; }
            public uint OtherByteSize { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public int[] MapData { get; set; }

            public bool IsCarmackized { get; set; }
        }

        public Map[] Maps { get; set; }

        public GameMaps Read(string mapHead, string gameMaps)
        {
            using (FileStream file = new FileStream(mapHead, FileMode.Open))
            {
                if (file.ReadWord() != 0xABCD)
                    throw new InvalidDataException("File \"" + mapHead + "\" has invalid signature code!");
                List<long> offsets = new List<long>();
                uint offset;
                while ((offset = file.ReadDWord()) != 0 && file.Position < file.Length)
                    offsets.Add(offset);
                Offsets = offsets.ToArray();
            }
            using (FileStream file = new FileStream(gameMaps, FileMode.Open))
            {
                List<Map> maps = new List<Map>();
                foreach (long offset in new long[] { Offsets[0] })
                {
                    file.Seek(offset, 0);
                    Map map = new Map
                    {
                        MapOffset = file.ReadDWord(),
                        ObjectOffset = file.ReadDWord(),
                        OtherOffset = file.ReadDWord(),
                        MapByteSize = file.ReadWord(),
                        ObjectByteSize = file.ReadWord(),
                        OtherByteSize = file.ReadWord(),
                        Width = file.ReadWord(),
                        Height = file.ReadWord()
                    };
                    char[] name = new char[16];
                    for (uint i = 0; i < 16; i++)
                        name[i] = (char)file.ReadByte();
                    name[15] = ' '; // getting rid of null characters at the end
                    map.Name = new string(name);

                    char[] carmackized = new char[4];
                    for (uint i = 0; i < 4; i++)
                        carmackized[i] = (char)file.ReadByte();
                    map.IsCarmackized = new string(carmackized).Equals("!ID!");

                    // "Note that for Wolfenstein 3D, a 4-byte signature string ("!ID!") will normally be present directly after the level name. The signature does not appear to be used anywhere, but is useful for distinguishing between v1.0 files (the signature string is missing), and files for v1.1 and later (includes the signature string)."
                    // "Note that for Wolfenstein 3D v1.0, map files are not carmackized, only RLEW compression is applied."
                    // http://www.shikadi.net/moddingwiki/GameMaps_Format#Map_data_.28GAMEMAPS.29

                    // Carmackized game maps files are external GAMEMAPS.xxx files and the map header is stored internally in the executable. The map header must be extracted and the game maps decompressed before TED5 can access them. TED5 itself can produce carmackized files and external MAPHEAD.xxx files. Carmackization does not replace the RLEW compression used in uncompressed data, but compresses this data, that is, the data is doubly compressed.

                    int[] mapData;

                    file.Seek(map.MapOffset, 0);
                    if (map.IsCarmackized)
                        mapData = CarmackExpand(file);
                    else
                    {
                        mapData = new int[map.MapByteSize / 2];
                        for (uint i = 0; i < mapData.Length; i++)
                            mapData[i] = file.ReadSWord();
                    }

                    map.MapData = RlewExpand(mapData, 64 * 64, 0xABCD);

                    int[] objectData = new int[map.ObjectByteSize];
                    int[] otherData = new int[map.OtherByteSize];
                    maps.Add(map);
                }
                Maps = maps.ToArray();
            }
            return this;
        }

        #region Decompression algorithms
        private static readonly int CARMACK_NEAR = 0xA7;
        private static readonly int CARMACK_FAR = 0xA8;

        public static int[] RlewExpand(int[] carmackExpanded, uint length, uint tag)
        {
            int[] rawMapData = new int[length];
            int src_index = 1, dest_index = 0;
            do
            {
                int value = carmackExpanded[src_index++]; // WORDS!!
                if (value != tag)
                    // uncompressed
                    rawMapData[dest_index++] = value;
                else
                {
                    // compressed string
                    int count = carmackExpanded[src_index++];
                    value = carmackExpanded[src_index++];
                    for (int i = 1; i <= count; i++)
                        rawMapData[dest_index++] = value;
                }
            } while (dest_index < length);
            return rawMapData;
        }

        public int[] CarmackExpand(FileStream file)
        {
            ////////////////////////////
            // Get to the correct chunk
            ushort ch, chhigh, count, offset, index;
            // First word is expanded length
            uint length = file.ReadWord();
            int[] expandedWords = new int[length]; // array of WORDS
            length /= 2;
            index = 0;
            while (length > 0)
            {
                ch = (ushort)file.ReadWord();
                chhigh = (ushort)(ch >> 8);
                if (chhigh == CARMACK_NEAR)
                {
                    count = (ushort)(ch & 0xFF);
                    if (count == 0)
                    {
                        ch |= (ushort)file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = (ushort)file.ReadByte();
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
                        ch |= (ushort)file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = (ushort)file.ReadWord();
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
