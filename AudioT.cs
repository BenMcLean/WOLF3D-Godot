using OPL;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static OPL.Imf;

namespace WOLF3D
{
    public class AudioT
    {
        public uint[] AudioHead;
        public byte[][] AudioTFile;
        public Adl[] Sounds;
        public ImfPacket[][] Songs;
        public uint StartAdlibSounds;
        public uint StartMusic;

        public AudioT(Stream audioHedStream, Stream audioTStream, XElement audio)
        {
            // Parse AUDIOHED file
            using (BinaryReader binaryReader = new BinaryReader(audioHedStream))
            {
                List<uint> list = new List<uint>();
                while (audioHedStream.Position < audioHedStream.Length)
                    list.Add(binaryReader.ReadUInt32());
                AudioHead = list.ToArray();
            }

            // Convert AUDIOT file into byte arrays
            AudioTFile = new byte[AudioHead.Length - 1][];
            for (uint chunk = 0; chunk < AudioTFile.Length; chunk++)
            {
                uint size = AudioHead[chunk + 1] - AudioHead[chunk];
                if (size > 0)
                {
                    AudioTFile[chunk] = new byte[size];
                    audioTStream.Seek(AudioHead[chunk], 0);
                    audioTStream.Read(AudioTFile[chunk], 0, AudioTFile[chunk].Length);
                }
            }

            // Convert byte arrays into sounds
            StartAdlibSounds = (uint)audio.Attribute("StartAdlibSounds");
            Sounds = new Adl[(uint)audio.Attribute("NumSounds")];
            for (uint i = 0; i < Sounds.Length; i++)
                if (AudioTFile[StartAdlibSounds + i] != null)
                    using (MemoryStream sound = new MemoryStream(AudioTFile[StartAdlibSounds + i]))
                        Sounds[i] = new Adl(sound);

            // Convert byte arrays into songs
            StartMusic = (uint)audio.Attribute("StartMusic");
            Songs = new ImfPacket[AudioTFile.Length - StartMusic][];
            for (uint i = 0; i < Songs.Length; i++)
                if (AudioTFile[StartMusic + i] != null)
                    using (MemoryStream song = new MemoryStream(AudioTFile[StartMusic + i], 0, AudioTFile[StartMusic + i].Length))
                        Songs[i] = ReadImf(song);
        }
    }
}
