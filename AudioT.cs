using System.Collections.Generic;
using System.IO;

namespace WOLF3D
{
    public class AudioT
    {
        public uint[] AudioHed;

        public AudioT(Stream AudioHed)
        {
            using (BinaryReader binaryReader = new BinaryReader(AudioHed))
            {
                List<uint> list = new List<uint>();
                while (AudioHed.Position < AudioHed.Length)
                    list.Add(binaryReader.ReadUInt32());
                this.AudioHed = list.ToArray();
            }
        }
    }
}
