using OPL;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using static OPL.Imf;

namespace WOLF3D
{
    public struct AudioT
    {
        public static AudioT Load(string folder, XElement xml)
        {
            using (FileStream audioHead = new FileStream(System.IO.Path.Combine(folder, xml.Element("Audio").Attribute("AudioHead").Value), FileMode.Open))
            using (FileStream audioTStream = new FileStream(System.IO.Path.Combine(folder, xml.Element("Audio").Attribute("AudioT").Value), FileMode.Open))
                return new AudioT(audioHead, audioTStream, xml.Element("Audio"));
        }

        public Adl[] Sounds;
        public Imf[][] Songs;

        public static uint[] ParseHead(Stream stream)
        {
            List<uint> list = new List<uint>();
            using (BinaryReader binaryReader = new BinaryReader(stream))
                while (stream.Position < stream.Length)
                    list.Add(binaryReader.ReadUInt32());
            return list.ToArray();
        }

        public static byte[][] SplitFile(Stream head, Stream file)
        {
            return SplitFile(ParseHead(head), file);
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

        public AudioT(Stream audioHedStream, Stream audioTStream, XElement audio) : this(SplitFile(audioHedStream, audioTStream), audio)
        { }

        public AudioT(byte[][] file, XElement audio)
        {
            uint startAdlibSounds = (uint)audio.Attribute("StartAdlibSounds");
            Sounds = new Adl[(uint)audio.Attribute("NumSounds")];
            for (uint i = 0; i < Sounds.Length; i++)
                if (file[startAdlibSounds + i] != null)
                    using (MemoryStream sound = new MemoryStream(file[startAdlibSounds + i]))
                        Sounds[i] = new Adl(sound);
            uint startMusic = (uint)audio.Attribute("StartMusic");
            Songs = new Imf[file.Length - startMusic][];
            for (uint i = 0; i < Songs.Length; i++)
                if (file[startMusic + i] != null)
                    using (MemoryStream song = new MemoryStream(file[startMusic + i]))
                        Songs[i] = ReadImf(song);
        }
    }
}
