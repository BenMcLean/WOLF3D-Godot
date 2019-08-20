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
            public long MapPointer { get; set; }
            public long ObjectPointer { get; set; }
            public long OtherPointer { get; set; }
            public uint MapSize { get; set; }
            public uint ObjectSize { get; set; }
            public uint OtherSize { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
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
                        MapPointer = file.ReadDWord(),
                        ObjectPointer = file.ReadDWord(),
                        OtherPointer = file.ReadDWord(),
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
                    maps.Add(map);
                }
                Maps = maps.ToArray();
            }
            return this;
        }
    }
}
