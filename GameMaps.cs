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
            public uint MapSize { get; set; }
            public uint ObjectSize { get; set; }
            public uint OtherSize { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint[] MapData { get; set; }
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
                while ((offset = file.ReadDWord()) != 0)
                    offsets.Add(offset);
                Offsets = offsets.ToArray();
            }
            using (FileStream file = new FileStream(gameMaps, FileMode.Open))
            {
                List<Map> maps = new List<Map>();
                foreach (long offset in Offsets)
                {
                    file.Seek(offset, 0);
                    Map map = new Map
                    {
                        MapOffset = file.ReadDWord(),
                        ObjectOffset = file.ReadDWord(),
                        OtherOffset = file.ReadDWord(),
                        MapSize = file.ReadWord(),
                        ObjectSize = file.ReadWord(),
                        OtherSize = file.ReadWord(),
                        Width = file.ReadWord(),
                        Height = file.ReadWord()
                    };
                    char[] name = new char[16];
                    for (uint i = 0; i < 16; i++)
                        name[i] = (char)file.ReadByte();
                    map.Name = new string(name);

                    // http://www.shikadi.net/moddingwiki/GameMaps_Format#Map_data_.28GAMEMAPS.29

                    //map.MapData = new int[map.MapSize];
                    file.Seek(map.MapOffset, 0);
                    //map.MapData = CarmackExpand(map.MapOffset, file);
                    //map.MapData = RlewExpand(CarmackExpand(map.MapOffset, file), map.MapSize, 0xABCD);
                    //for (int i = 0; i < map.MapData.Length; i++)
                    //    map.MapData[i] = (int)file.ReadWord();

                    int[] objectData = new int[map.ObjectSize];
                    int[] otherData = new int[map.OtherSize];

                    maps.Add(map);
                }
                Maps = maps.ToArray();
            }
            return this;
        }

        #region Decompression algorithms
        private static readonly int CARMACK_NEAR = 0xA7;
        private static readonly int CARMACK_FAR = 0xA8;

        public static uint[] RlewExpand(uint[] carmackExpanded, uint length, uint tag)
        {
            uint[] rawMapData = new uint[length];
            int src_index = 1, dest_index = 0;
            do
            {
                uint value = carmackExpanded[src_index++]; // WORDS!!
                if (value != tag)
                    // uncompressed
                    rawMapData[dest_index++] = value;
                else
                {
                    // compressed string
                    uint count = carmackExpanded[src_index++];
                    value = carmackExpanded[src_index++];
                    for (int i = 1; i <= count; i++)
                        rawMapData[dest_index++] = value;
                }
            } while (dest_index < length);
            return rawMapData;
        }

        public static uint[] CarmackExpand(long position, FileStream file)
        {
            ////////////////////////////
            // Get to the correct chunk
            uint length;
            uint ch, chhigh, count, offset, index = 0;
            file.Seek(position, 0);
            // First word is expanded length
            length = file.ReadWord();
            uint[] expandedWords = new uint[length]; // array of WORDS
            length /= 2;
            while (length > 0)
            {
                ch = file.ReadWord();
                chhigh = ch >> 8;
                if (chhigh == CARMACK_NEAR)
                {
                    count = (ch & 0xFF);
                    if (count == 0)
                    {
                        ch |= (uint)file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = (uint)file.ReadByte();
                        length -= count;
                        if (length < 0)
                            return expandedWords;
                        while ((count--) > 0)
                        {
                            expandedWords[index] = expandedWords[index - offset];
                            index++;
                        }
                    }
                }
                else if (chhigh == CARMACK_FAR)
                {
                    count = (ch & 0xFF);
                    if (count == 0)
                    {
                        ch |= (uint)file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = file.ReadWord();
                        length -= count;
                        if (length < 0)
                            return expandedWords;
                        while ((count--) > 0)
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
        #endregion
    }
}
