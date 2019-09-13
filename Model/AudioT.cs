using OPL;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static OPL.Imf;

namespace WOLF3D
{
    public class AudioT
    {
        public static AudioT Load(string folder, XElement xml)
        {
            using (FileStream audioHead = new FileStream(System.IO.Path.Combine(folder, xml.Element("Audio").Attribute("AudioHead").Value), FileMode.Open))
            using (FileStream audioTStream = new FileStream(System.IO.Path.Combine(folder, xml.Element("Audio").Attribute("AudioT").Value), FileMode.Open))
                return new AudioT(audioHead, audioTStream, xml.Element("Audio"));
        }

        public uint[] AudioHead;
        public byte[][] AudioTFile;
        public Adl[] Sounds;
        public ImfPacket[][] Songs;
        public uint StartAdlibSounds;
        public uint StartMusic;

        public static uint[] ParseHead(Stream stream)
        {
            List<uint> list = new List<uint>();
            using (BinaryReader binaryReader = new BinaryReader(stream))
                while (stream.Position <= stream.Length - 4) // minus 4 because a 32 bits is 4 bytes
                    list.Add(binaryReader.ReadUInt32());
            return list.ToArray();
        }

        public static byte[][] SplitFile(uint[] head, Stream file)
        {
            byte[][] split = new byte[head.Length - 1][];
            for (uint chunk = 0; chunk < split.Length; chunk++)
            {
                uint size = head[chunk + 1] - head[chunk];
                if (size > 0)
                {
                    split[chunk] = new byte[size];
                    file.Seek(head[chunk], 0);
                    file.Read(split[chunk], 0, split[chunk].Length);
                }
            }
            return split;
        }

        public AudioT(Stream audioHedStream, Stream audioTStream, XElement audio)
        {
            AudioHead = ParseHead(audioHedStream);
            AudioTFile = SplitFile(AudioHead, audioTStream);

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
                    using (MemoryStream song = new MemoryStream(AudioTFile[StartMusic + i]))
                        Songs[i] = ReadImf(song);
        }
    }
}
