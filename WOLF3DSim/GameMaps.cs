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
            public ushort Height { get; set; }
            public ushort[] MapData { get; set; }
            public ushort[] ObjectData { get; set; }
            public ushort[] OtherData { get; set; }
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
                foreach (long offset in Offsets)
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
                    name[14] = ' '; // getting rid of null characters at the end
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

                    ushort[] mapData;
                    file.Seek(map.MapOffset, 0);
                    if (map.IsCarmackized)
                        mapData = CarmackExpand(file);
                    else
                    {
                        mapData = new ushort[map.MapByteSize / 2];
                        for (uint i = 0; i < mapData.Length; i++)
                            mapData[i] = file.ReadWord();
                    }
                    map.MapData = RlewExpand(mapData, (ushort)(map.Height * map.Width), 0xABCD);

                    ushort[] objectData;
                    file.Seek(map.ObjectOffset, 0);
                    if (map.IsCarmackized)
                        objectData = CarmackExpand(file);
                    else
                    {
                        objectData = new ushort[map.ObjectByteSize / 2];
                        for (uint i = 0; i < objectData.Length; i++)
                            objectData[i] = file.ReadWord();
                    }
                    map.ObjectData = RlewExpand(objectData, (ushort)(map.Height * map.Width), 0xABCD);

                    ushort[] otherData;
                    file.Seek(map.OtherOffset, 0);
                    if (map.IsCarmackized)
                        otherData = CarmackExpand(file);
                    else
                    {
                        otherData = new ushort[map.OtherByteSize / 2];
                        for (uint i = 0; i < otherData.Length; i++)
                            otherData[i] = file.ReadWord();
                    }
                    map.OtherData = RlewExpand(otherData, (ushort)(map.Height * map.Width), 0xABCD);

                    maps.Add(map);
                }
                Maps = maps.ToArray();
            }
            return this;
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

        public ushort[] CarmackExpand(FileStream file)
        {
            ////////////////////////////
            // Get to the correct chunk
            ushort ch, chhigh, count, offset, index;
            // First word is expanded length
            ushort length = file.ReadWord();
            ushort[] expandedWords = new ushort[length]; // array of WORDS
            length /= 2;
            index = 0;
            while (length > 0)
            {
                ch = file.ReadWord();
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
