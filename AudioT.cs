using System.Collections.Generic;
using System.IO;

namespace WOLF3D
{
    public class AudioT
    {
        public uint[] AudioHed;
        public byte[][] AudioTFile;

        public AudioT(Stream audioHedStream, Stream audioTStream)
        {
            // Parse AUDIOHED file
            using (BinaryReader binaryReader = new BinaryReader(audioHedStream))
            {
                List<uint> list = new List<uint>();
                while (audioHedStream.Position < audioHedStream.Length)
                    list.Add(binaryReader.ReadUInt32());
                AudioHed = list.ToArray();
            }

            // Convert AUDIOT file into byte arrays
            AudioTFile = new byte[AudioHed.Length - 1][];
            for (uint chunk = 0; chunk < AudioTFile.Length; chunk++)
            {
                AudioTFile[chunk] = new byte[AudioHed[chunk + 1] - AudioHed[chunk]];
                audioTStream.Seek(AudioHed[chunk], 0);
                audioTStream.Read(AudioTFile[chunk], 0, AudioTFile[chunk].Length);
            }
        }
    }
}
