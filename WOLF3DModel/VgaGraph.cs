using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace WOLF3DModel
{
    public struct VgaGraph
    {
        XElement XML { get; set; }

        public static VgaGraph Load(string folder, XElement xml)
        {
            using (FileStream vgaHead = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaHead").Value), FileMode.Open))
            using (FileStream vgaGraphStream = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaGraph").Value), FileMode.Open))
            using (FileStream vgaDict = new FileStream(System.IO.Path.Combine(folder, xml.Element("VgaGraph").Attribute("VgaDict").Value), FileMode.Open))
                return new VgaGraph(vgaHead, vgaGraphStream, vgaDict, xml);
        }

        public struct Font
        {
            public ushort Height;
            public byte[] Width;
            public byte[][] Character;

            public Font(Stream stream)
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    Height = binaryReader.ReadUInt16();
                    ushort[] location = new ushort[256];
                    for (uint i = 0; i < location.Length; i++)
                        location[i] = binaryReader.ReadUInt16();
                    Width = new byte[location.Length];
                    for (uint i = 0; i < Width.Length; i++)
                        Width[i] = binaryReader.ReadByte();
                    Character = new byte[Width.Length][];
                    for (uint i = 0; i < Character.Length; i++)
                    {
                        Character[i] = new byte[Width[i] * Height * 4];
                        stream.Seek(location[i], 0);
                        for (uint j = 0; j < Character[i].Length / 4; j++)
                            if (binaryReader.ReadByte() != 0)
                                for (uint k = 0; k < 4; k++)
                                    Character[i][j * 4 + k] = 255;
                    }
                }
            }

            public byte[] Text(string input, ushort padding = 0)
            {
                if (input == null)
                    return null;
                int width = CalcWidth(input);
                string[] lines = input.Split('\n');
                byte[] bytes = new byte[width * 4 * (Height + padding) * lines.Length];
                for (int line = 0; line < lines.Length; line++)
                {
                    int lineWidth = CalcWidthLine(lines[line]),
                        lineStart = line * (Height + padding);
                    byte[] lineBytes = Line(lines[line]);
                    for (int y = 0; y < Height; y++)
                        Array.Copy(
                            //sourceArray
                            lineBytes,
                            //sourceIndex
                            y * lineWidth * 4,
                            //destinationArray
                            bytes,
                            //destinationIndex
                            (lineStart + y) * width * 4,
                            //length
                            lineWidth * 4
                            );
                }
                return bytes;
            }

            public byte[] Line(string input)
            {
                if (input == null)
                    return null;
                int width = CalcWidth(input) * 4;
                byte[] bytes = new byte[width * Height];
                int rowStart = 0;
                foreach (char c in input)
                {
                    for (int x = 0; x < Width[c] * 4; x++)
                        for (int y = 0; y < Height; y++)
                            bytes[y * width + rowStart + x] = Character[c][y * Width[c] * 4 + x];
                    rowStart += Width[c] * 4;
                }
                return bytes;
            }

            public int CalcHeight(string input, ushort padding = 0) => (Height + padding) * (input == null ? 0 : input.Split('\n').Length);

            public int CalcWidth(string input)
            {
                if (input == null)
                    return 0;
                int longest = 0;
                foreach (string line in input.Split('\n'))
                    longest = Math.Max(longest, CalcWidthLine(line));
                return longest;
            }

            public int CalcWidthLine(string input)
            {
                if (input == null)
                    return 0;
                int result = 0;
                foreach (char c in input)
                    result += Width[c];
                return result;
            }
        }

        public Font[] Fonts { get; set; }
        public byte[][] Pics { get; set; }
        public ushort[][] Sizes { get; set; }
        public uint[] Palette { get; set; }

        public VgaGraph(Stream vgaHead, Stream vgaGraph, Stream dictionary, XElement xml) : this(SplitFile(ParseHead(vgaHead), vgaGraph, Load16BitPairs(dictionary)), xml)
        { }

        public VgaGraph(byte[][] file, XElement xml)
        {
            Palette = VSwap.LoadPalette(xml);
            XML = xml.Element("VgaGraph");
            using (MemoryStream sizes = new MemoryStream(file[(uint)XML.Element("Sizes").Attribute("Chunk")]))
                Sizes = Load16BitPairs(sizes);
            uint startFont = (uint)XML.Element("Sizes").Attribute("StartFont");
            Fonts = new Font[(uint)XML.Element("Sizes").Attribute("NumFont")];
            for (uint i = 0; i < Fonts.Length; i++)
                using (MemoryStream font = new MemoryStream(file[startFont + i]))
                    Fonts[i] = new Font(font);
            uint startPics = (uint)XML.Element("Sizes").Attribute("StartPics");
            Pics = new byte[(uint)XML.Element("Sizes").Attribute("NumPics")][];
            for (uint i = 0; i < Pics.Length; i++)
                Pics[i] = VSwap.Index2ByteArray(Deplanify(file[startPics + i], Sizes[i][0]), Palette);
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
            using (BinaryReader binaryReader = new BinaryReader(file))
                for (uint i = 0; i < split.Length; i++)
                {
                    uint size = head[i + 1] - head[i];
                    if (size > 0)
                    {
                        file.Seek(head[i], 0);
                        uint length = binaryReader.ReadUInt32();
                        binaryReader.Read(split[i] = new byte[size - 2], 0, split[i].Length);
                        split[i] = CAL_HuffExpand(split[i], dictionary, length);
                    }
                }
            return split;
        }

        public static byte[] Deplanify(byte[] input, ushort width) =>
            Deplanify(input, width, (ushort)(input.Length / width));

        public static byte[] Deplanify(byte[] input, ushort width, ushort height)
        {
            byte[] bytes = new byte[input.Length];
            int linewidth = width / 4;
            for (int i = 0; i < bytes.Length; i++)
            {
                int plane = i / ((width * height) / 4),
                    sx = ((i % linewidth) * 4) + plane,
                    sy = ((i / linewidth) % height);
                bytes[sy * width + sx] = input[i];
            }
            return bytes;
        }

        /// <summary>
        /// Implementing Huffman decompression. http://www.shikadi.net/moddingwiki/Huffman_Compression#Huffman_implementation_in_ID_Software_games
        /// Translated from https://github.com/mozzwald/wolf4sdl/blob/master/id_ca.cpp#L214-L260
        /// </summary>
        /// <param name="length">When to stop. Default 0 indicates to keep going until source is exhausted.</param>
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

        public static ushort[][] Load16BitPairs(Stream stream)
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
