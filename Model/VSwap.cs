using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WOLF3D
{
    public struct VSwap
    {
        public static VSwap Load(string folder, XElement xml)
        {
            using (FileStream vSwap = new FileStream(System.IO.Path.Combine(folder, xml.Element("VSwap").Attribute("Name").Value), FileMode.Open))
                return new VSwap(LoadPalette(xml), vSwap);
        }

        private static readonly ushort COLORS = 256;

        public uint[] Palette { get; set; }
        public byte[][] Pages { get; set; }
        public byte[][] DigiSounds { get; set; }
        public ushort SpritePage { get; set; }
        public ushort NumPages { get; set; }

        public byte[] Sprite(ushort number)
        {
            return Pages[SpritePage + number];
        }

        public VSwap(Stream palette, Stream vswap) : this(LoadPalette(palette), vswap)
        { }

        public VSwap(uint[] palette, Stream stream, ushort tileSqrt = 64)
        {
            Palette = palette;
            if (Palette == null)
                throw new InvalidDataException("Must load a palette before loading a VSWAP!");
            using (BinaryReader binaryReader = new BinaryReader(stream))
            {
                // parse header info
                NumPages = binaryReader.ReadUInt16();
                SpritePage = binaryReader.ReadUInt16();
                Pages = new byte[binaryReader.ReadUInt16()][]; // SoundPage

                uint[] pageOffsets = new uint[NumPages];
                uint dataStart = 0;
                for (ushort i = 0; i < pageOffsets.Length; i++)
                {
                    pageOffsets[i] = binaryReader.ReadUInt32();
                    if (i == 0)
                        dataStart = pageOffsets[0];
                    if ((pageOffsets[i] != 0 && pageOffsets[i] < dataStart) || pageOffsets[i] > stream.Length)
                        throw new InvalidDataException("VSWAP contains invalid page offsets.");
                }
                ushort[] pageLengths = new ushort[NumPages];
                for (ushort i = 0; i < pageLengths.Length; i++)
                    pageLengths[i] = binaryReader.ReadUInt16();

                ushort page;
                // read in walls
                for (page = 0; page < SpritePage; page++)
                    if (pageOffsets[page] > 0)
                    {
                        stream.Seek(pageOffsets[page], 0);
                        byte[] wall = new byte[tileSqrt * tileSqrt];
                        for (ushort col = 0; col < tileSqrt; col++)
                            for (ushort row = 0; row < tileSqrt; row++)
                                wall[tileSqrt * row + col] = (byte)stream.ReadByte();
                        Pages[page] = Index2ByteArray(wall, palette);
                    }

                // read in sprites
                for (; page < Pages.Length; page++)
                    if (pageOffsets[page] > 0)
                    {
                        stream.Seek(pageOffsets[page], 0);
                        ushort leftExtent = binaryReader.ReadUInt16(),
                            rightExtent = binaryReader.ReadUInt16(),
                            startY, endY;
                        byte[] sprite = new byte[tileSqrt * tileSqrt];
                        for (ushort i = 0; i < sprite.Length; i++)
                            sprite[i] = 255; // set transparent
                        long[] columnDataOffsets = new long[rightExtent - leftExtent + 1];
                        for (ushort i = 0; i < columnDataOffsets.Length; i++)
                            columnDataOffsets[i] = pageOffsets[page] + binaryReader.ReadUInt16();
                        long trexels = stream.Position;
                        for (ushort column = 0; column <= rightExtent - leftExtent; column++)
                        {
                            long commands = columnDataOffsets[column];
                            stream.Seek(commands, 0);
                            while ((endY = binaryReader.ReadUInt16()) != 0)
                            {
                                endY >>= 1;
                                binaryReader.ReadUInt16(); // Not using this value for anything. Don't know why it's here!
                                startY = binaryReader.ReadUInt16();
                                startY >>= 1;
                                commands = stream.Position;
                                stream.Seek(trexels, 0);
                                for (ushort row = startY; row < endY; row++)
                                    sprite[(row * tileSqrt - 1) + column + leftExtent - 1] = binaryReader.ReadByte();
                                trexels = stream.Position;
                                stream.Seek(commands, 0);
                            }
                        }
                        Pages[page] = Index2ByteArray(sprite, palette);
                    }

                // read in digisounds
                byte[] soundData = new byte[stream.Length - pageOffsets[Pages.Length]];
                stream.Seek(pageOffsets[Pages.Length], 0);
                stream.Read(soundData, 0, soundData.Length);

                uint start = pageOffsets[NumPages - 1] - pageOffsets[Pages.Length];
                ushort[][] soundTable;
                using (MemoryStream memoryStream = new MemoryStream(soundData, (int)start, soundData.Length - (int)start))
                    soundTable = VgaGraph.Load16BitPairs(memoryStream);

                uint numDigiSounds = 0;
                while (numDigiSounds < soundTable.Length && soundTable[numDigiSounds][1] > 0)
                    numDigiSounds++;

                DigiSounds = new byte[numDigiSounds][];
                for (uint sound = 0; sound < DigiSounds.Length; sound++)
                    if (soundTable[sound][1] > 0 && pageOffsets[Pages.Length + soundTable[sound][0]] > 0)
                    {
                        DigiSounds[sound] = new byte[soundTable[sound][1]];
                        start = pageOffsets[Pages.Length + soundTable[sound][0]] - pageOffsets[Pages.Length];
                        for (uint bite = 0; bite < DigiSounds[sound].Length; bite++)
                            DigiSounds[sound][bite] = (byte)(soundData[start + bite] - 128); // Godot makes some kind of oddball conversion from the unsigned byte to a signed byte
                    }
            }
        }

        public static uint[] LoadPalette(XElement xml)
        {
            using (MemoryStream palette = new MemoryStream(Encoding.ASCII.GetBytes(xml.Element("Palette").Value)))
                return LoadPalette(palette);
        }

        public static uint[] LoadPalette(Stream stream)
        {
            uint[] result;
            using (StreamReader streamReader = new StreamReader(stream))
            {
                string line;
                while (string.IsNullOrWhiteSpace(line = streamReader.ReadLine().Trim())) { }
                if (!line.Equals("JASC-PAL") || !streamReader.ReadLine().Trim().Equals("0100"))
                    throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
                if (!uint.TryParse(streamReader.ReadLine()?.Trim(), out uint numColors)
                 || numColors != COLORS)
                    throw new InvalidDataException("Palette stream does not contain exactly " + COLORS + " colors.");
                result = new uint[numColors];
                for (uint x = 0; x < numColors; x++)
                {
                    string[] tokens = streamReader.ReadLine()?.Trim().Split(' ');
                    if (tokens == null || tokens.Length != 3
                        || !byte.TryParse(tokens[0], out byte r)
                        || !byte.TryParse(tokens[1], out byte g)
                        || !byte.TryParse(tokens[2], out byte b))
                        throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
                    result[x] = (uint)(r << 24)
                        + (uint)(g << 16)
                        + (uint)(b << 8)
                        + (uint)(x == 255 ? 0 : 255);
                }
            }
            return result;
        }

        /// <param name="index">Palette indexes (one byte per pixel)</param>
        /// <returns>rgba8888 texture (four bytes per pixel) using current palette</returns>
        public byte[] Index2ByteArray(byte[] index)
        {
            return Index2ByteArray(index, Palette);
        }

        /// <param name="index">Palette indexes (one byte per pixel)</param>
        /// <param name="palette">256 rgba8888 color values</param>
        /// <returns>rgba8888 texture (four bytes per pixel)</returns>
        public static byte[] Index2ByteArray(byte[] index, uint[] palette)
        {
            byte[] bytes = new byte[index.Length * 4];
            for (uint i = 0; i < index.Length; i++)
            {
                bytes[i * 4] = (byte)(palette[index[i]] >> 24);
                bytes[i * 4 + 1] = (byte)(palette[index[i]] >> 16);
                bytes[i * 4 + 2] = (byte)(palette[index[i]] >> 8);
                bytes[i * 4 + 3] = (byte)palette[index[i]];
            }
            return bytes;
        }

        public static uint[] Repeat256(uint[] pixels256)
        {
            uint[] repeated = new uint[4096];
            for (uint x = 0; x < repeated.Length; x += 256)
                Array.Copy(pixels256, 0, repeated, x, 256);
            return repeated;
        }

        public static uint[] Tile(uint[] squareTexture, uint tileSqrt)
        {
            uint side = (uint)System.Math.Sqrt(squareTexture.Length);
            uint newSide = side * tileSqrt;
            uint[] tiled = new uint[squareTexture.Length * tileSqrt * tileSqrt];
            for (uint x = 0; x < newSide; x++)
                for (uint y = 0; y < newSide; y++)
                    tiled[x * newSide + y] = squareTexture[x % side * side + y % side];
            return tiled;
        }

        public static uint[] Scale(uint[] squareTexture, int factor)
        {
            uint side = (uint)System.Math.Sqrt(squareTexture.Length);
            int newSide = (int)side * factor;
            uint[] scaled = new uint[squareTexture.Length * factor * factor];
            for (uint x = 0; x < newSide; x++)
                for (uint y = 0; y < newSide; y++)
                    scaled[x * newSide + y] = squareTexture[x / factor * side + y / factor];
            return scaled;
        }

        /// <param name="ints">rgba8888 color values (one uint per pixel)</param>
        /// <returns>rgba8888 texture (four bytes per pixel)</returns>
        public static byte[] Int2ByteArray(uint[] ints)
        {
            byte[] bytes = new byte[ints.Length * 4];
            for (uint i = 0; i < ints.Length; i++)
            {
                bytes[i * 4] = (byte)(ints[i] >> 24);
                bytes[i * 4 + 1] = (byte)(ints[i] >> 16);
                bytes[i * 4 + 2] = (byte)(ints[i] >> 8);
                bytes[i * 4 + 3] = (byte)ints[i];
            }
            return bytes;
        }

        public static T[] ConcatArrays<T>(params T[][] list)
        {
            var result = new T[list.Sum(a => a.Length)];
            int offset = 0;
            for (int x = 0; x < list.Length; x++)
            {
                list[x].CopyTo(result, offset);
                offset += list[x].Length;
            }
            return result;
        }
    }
}
