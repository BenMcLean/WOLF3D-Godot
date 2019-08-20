using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D
{
    public class Maps
    {
        public Maps Read(string mapHead, string gameMaps)
        {
            using (FileStream file = new FileStream(mapHead, FileMode.Open))
            {
                if (file.ReadWord() != 0xABCD)
                    throw new InvalidDataException("File \"" + mapHead + "\" has invalid signature code!");
            }
            return this;
        }
    }
}
