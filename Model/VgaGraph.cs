using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace WOLF3D
{
    public class VgaGraph
    {
        public static VgaGraph Load(string folder, XElement xml)
        {
            using (FileStream vgaHead = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaHead").Value), FileMode.Open))
            using (FileStream vgaGraphStream = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaGraph").Value), FileMode.Open))
            using (FileStream vgaDict = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaDict").Value), FileMode.Open))
                return new VgaGraph(vgaHead, vgaGraphStream, vgaDict, xml);
        }

        public byte[][] File { get; set; }
        public uint[] Palette { get; set; }
        public ushort[][] Sizes { get; set; }
        public uint StartPics { get; set; }
        public uint NumPics { get; set; }

        public ushort[] Size(uint pic)
        {
            return Sizes[pic - StartPics];
        }

        public VgaGraph(Stream vgaHead, Stream vgaGraph, Stream dictionary, XElement xml) : this(SplitFile(ParseHead(vgaHead), vgaGraph, Load16BitTuples(dictionary)), xml)
        { }

        public VgaGraph(byte[][] file, XElement xml)
        {
            File = file;
            using (MemoryStream sizes = new MemoryStream(File[(uint)xml.Element("VgaGraph").Element("Sizes").Attribute("Number")]))
                Sizes = Load16BitTuples(sizes);
            StartPics = (uint)xml.Element("VgaGraph").Element("Sizes").Attribute("StartPics");
            NumPics = (uint)xml.Element("VgaGraph").Element("Sizes").Attribute("NumPics");
            for (uint i = StartPics; i < NumPics - StartPics; i++)
                File[i] = Deplanify(File[i], Size(i));
            Palette = VSwap.LoadPalette(xml);
        }

        public static uint[] ParseHead(Stream stream)
        {
            uint[] head = new uint[stream.Length / 3];
            for (uint i = 0; i < head.Length; i++)
                head[i] = Read24Bits(stream);
            return head;
        }

        public static uint Read24Bits(Stream stream)
        {
            return (uint)(stream.ReadByte() | (stream.ReadByte() << 8) | (stream.ReadByte() << 16));
        }

        public static uint[] GetLengths(uint[] head, Stream file)
        {
            uint[] lengths = new uint[head.Length - 1];
            using (BinaryReader binaryReader = new BinaryReader(file))
                for (uint i = 0; i < lengths.Length; i++)
                {
                    file.Seek(head[i], 0);
                    lengths[i] = binaryReader.ReadUInt32();
                }
            return lengths;
        }

        public static byte[][] SplitFile(uint[] head, Stream file, ushort[][] dictionary)
        {
            byte[][] split = new byte[head.Length - 1][];
            using (BinaryReader binaryReader = new BinaryReader(file))
                for (uint i = 0; i < split.Length; i++)
                {
                    uint size = head[i + 1] - head[i];
                    if (size > 0)
                    {
                        file.Seek(head[i], 0);
                        uint length = binaryReader.ReadUInt32();
                        file.Read(split[i] = new byte[size - 2], 0, split[i].Length);
                        split[i] = CAL_HuffExpand(split[i], dictionary, length);
                    }
                }
            return split;
        }

        public static byte[] Deplanify(byte[] input, ushort[] size)
        {
            return Deplanify(input, size[0], size[1]);
        }

        public static byte[] Deplanify(byte[] input, ushort width, ushort height)
        {
            byte[] bytes = new byte[input.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                int linewidth = width / 4,
                    plane = i / ((width * height) / 4),
                    sx = ((i % (linewidth)) * 4) + plane,
                    sy = ((i / linewidth) % height);
                bytes[sy * width + sx] = input[i];
            }
            return bytes;
        }

        /// <summary>
        /// Implementing Huffman decompression. http://www.shikadi.net/moddingwiki/Huffman_Compression#Huffman_implementation_in_ID_Software_games
        /// Translated from https://github.com/mozzwald/wolf4sdl/blob/master/id_ca.cpp#L214-L260
        /// </summary>
        /// <param name="dictionary">The Huffman dictionary is a ushort[255][2]</param>
        public static byte[] CAL_HuffExpand(byte[] source, ushort[][] dictionary, uint length = 0)
        {
            List<byte> dest = new List<byte>();
            ushort[] huffNode = dictionary[254];
            uint read = 0;
            ushort nodeVal;
            byte val = source[read++], mask = 1;
            while (read < source.Length && (length <= 0 || dest.Count < length))
            {
                nodeVal = huffNode[(val & mask) == 0 ? 0 : 1];
                if (mask == 0x80)
                {
                    val = source[read++];
                    mask = 1;
                }
                else
                    mask <<= 1;
                if (nodeVal < 256)
                { // 0-255 is a character, > is a pointer to a node
                    dest.Add((byte)nodeVal);
                    huffNode = dictionary[254];
                }
                else
                    huffNode = dictionary[nodeVal - 256];
            }
            return dest.ToArray();
        }

        public static ushort[][] Load16BitTuples(Stream stream)
        {
            ushort[][] dest = new ushort[stream.Length / 4][];
            using (BinaryReader binaryReader = new BinaryReader(stream))
                for (uint i = 0; i < dest.Length; i++)
                    dest[i] = new ushort[]
                    {
                        binaryReader.ReadUInt16(),
                        binaryReader.ReadUInt16()
                    };
            return dest;
        }
    }
}
