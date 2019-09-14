using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace WOLF3D
{
    public class VgaGraph
    {
        public static VgaGraph Load(string folder, XElement xml)
        {
            using (FileStream vgaDict = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaDict").Value), FileMode.Open))
            using (FileStream vgaHead = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaHead").Value), FileMode.Open))
            using (FileStream vgaGraphStream = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaGraph").Value), FileMode.Open))
                return new VgaGraph(vgaDict, vgaHead, vgaGraphStream);
        }

        public byte[][] VgaGraphFile { get; set; }

        public VgaGraph(Stream dictionary, Stream vgaHead, Stream vgaGraph)
        {
            VgaGraphFile = SplitFile(ParseHead(vgaHead), vgaGraph, LoadDictionary(dictionary));
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

        public static byte[][] SplitFile(uint[] head, Stream file, ushort[][] dictionary)
        {
            byte[][] split = new byte[head.Length - 1][];
            for (uint i = 0; i < split.Length; i++)
            {
                uint size = head[i + 1] - head[i];
                if (size > 0)
                {
                    split[i] = new byte[size];
                    file.Seek(head[i], 0);
                    uint length = Read24Bits(file);
                    file.Read(split[i], 0, split[i].Length - 3);
                    split[i] = CAL_HuffExpand(split[i], dictionary, length);
                }
            }
            return split;
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

        public static ushort[][] LoadDictionary(Stream stream)
        {
            using (BinaryReader binaryReader = new BinaryReader(stream))
                return LoadDictionary(binaryReader);
        }

        public static ushort[][] LoadDictionary(BinaryReader binaryReader)
        {
            ushort[][] dictionary = new ushort[255][];
            for (uint i = 0; i < dictionary.Length; i++)
                dictionary[i] = new ushort[]
                {
                    binaryReader.ReadUInt16(),
                    binaryReader.ReadUInt16()
                };
            return dictionary;
        }
    }
}
