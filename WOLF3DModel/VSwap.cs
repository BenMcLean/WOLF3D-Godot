using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WOLF3DModel
{
    public struct VSwap
    {
        public static VSwap Load(string folder, XElement xml)
        {
            using (FileStream vSwap = new FileStream(System.IO.Path.Combine(folder, xml.Element("VSwap").Attribute("Name").Value), FileMode.Open))
                return new VSwap(LoadPalette(xml), vSwap,
                    ushort.TryParse(xml?.Element("VSwap")?.Attribute("Sqrt")?.Value, out ushort tileSqrt) ? tileSqrt : (ushort)64
                    );
        }

        public uint[] Palette { get; set; }
        public byte[][] Pages { get; set; }
        public byte[][] DigiSounds { get; set; }
        public ushort SpritePage { get; set; }
        public ushort NumPages { get; set; }
        public int SoundPage => Pages.Length;
        public ushort TileSqrt { get; set; }

        public byte[] Sprite(ushort number) => Pages[SpritePage + number];

        public static uint GetOffset(ushort x, ushort y, ushort tileSqrt = 64) => (uint)((tileSqrt * y + x) * 4);
        public uint GetOffset(ushort x, ushort y) => GetOffset(x, y, TileSqrt);
        public byte GetR(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y)];
        public byte GetG(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y) + 1];
        public byte GetB(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y) + 2];
        public byte GetA(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y) + 3];
        public bool IsTransparent(ushort page, ushort x, ushort y) =>
            page >= Pages.Length
            || Pages[page] == null
            || (page >= SpritePage // We know walls aren't transparent
            && GetOffset(x, y) + 3 is uint offset
            && offset < Pages[page].Length
            && Pages[page][offset] > 128);

        public VSwap(uint[] palette, Stream stream, ushort tileSqrt = 64)
        {
            Palette = palette;
            TileSqrt = tileSqrt;
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
                        byte[] wall = new byte[TileSqrt * TileSqrt];
                        for (ushort col = 0; col < TileSqrt; col++)
                            for (ushort row = 0; row < TileSqrt; row++)
                                wall[TileSqrt * row + col] = (byte)stream.ReadByte();
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
                        byte[] sprite = new byte[TileSqrt * TileSqrt];
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
                                    sprite[(row * TileSqrt - 1) + column + leftExtent - 1] = binaryReader.ReadByte();
                                trexels = stream.Position;
                                stream.Seek(commands, 0);
                            }
                        }
                        //Pages[page] = Index2ByteArray(sprite, palette);
                        Pages[page] = Int2ByteArray(TransparentBorder(Index2IntArray(sprite, palette)));
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
                 || numColors != 256)
                    throw new InvalidDataException("Palette stream does not contain exactly 256 colors.");
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

        public static byte R(uint color) => (byte)(color >> 24);
        public static byte G(uint color) => (byte)(color >> 16);
        public static byte B(uint color) => (byte)(color >> 8);
        public static byte A(uint color) => (byte)color;
        public static uint Color(byte r, byte g, byte b, byte a)
            => (uint)(r << 24 | g << 16 | b << 8 | a);

        public static uint[] TransparentBorder(uint[] squareTexture)
            => TransparentBorder(squareTexture, (uint)System.Math.Sqrt(squareTexture.Length));

        public static uint[] TransparentBorder(uint[] texture, uint width)
        {
            uint[] result = new uint[texture.Length];
            Array.Copy(texture, result, result.Length);
            uint height = (uint)(texture.Length / width);
            int Index(int x, int y) => x * (int)width + y;
            List<uint> neighbors = new List<uint>();
            void Add(int x, int y)
            {
                if (x >= 0 && y >= 0 && x < width && y < height
                    && texture[Index(x, y)] is uint pixel
                    && A(pixel) > 128)
                    neighbors.Add(pixel);
            }
            uint Average()
            {
                int count = neighbors.Count();
                if (count == 1)
                    return neighbors.First() & 0xFFFFFF00u;
                uint r = 0, g = 0, b = 0;
                foreach (uint color in neighbors)
                {
                    r += R(color);
                    g += G(color);
                    b += B(color);
                }
                return Color((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
            }
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (A(texture[Index(x, y)]) < 128)
                    {
                        neighbors.Clear();
                        Add(x - 1, y);
                        Add(x + 1, y);
                        Add(x, y - 1);
                        Add(x, y + 1);
                        if (neighbors.Count > 0)
                            result[Index(x, y)] = Average();
                        else
                        {
                            Add(x - 1, y - 1);
                            Add(x + 1, y - 1);
                            Add(x - 1, y + 1);
                            Add(x + 1, y + 1);
                            if (neighbors.Count > 0)
                                result[Index(x, y)] = Average();
                            else // Make non-border transparent pixels transparent black
                                result[Index(x, y)] = 0u;
                        }
                    }
            return result;
        }

        /// <param name="index">Palette indexes (one byte per pixel)</param>
        /// <returns>rgba8888 texture (four bytes per pixel) using current palette</returns>
        public byte[] Index2ByteArray(byte[] index) => Index2ByteArray(index, Palette);

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

        /// <param name="index">Palette indexes (one byte per pixel)</param>
        /// <returns>rgba8888 texture (one int per pixel) using current palette</returns>
        public uint[] Index2IntArray(byte[] index) => Index2IntArray(index, Palette);

        /// <param name="index">Palette indexes (one byte per pixel)</param>
        /// <param name="palette">256 rgba8888 color values</param>
        /// <returns>rgba8888 texture (one int per pixel)</returns>
        public static uint[] Index2IntArray(byte[] index, uint[] palette)
        {
            uint[] ints = new uint[index.Length];
            for (uint i = 0; i < index.Length; i++)
                ints[i] = palette[index[i]];
            return ints;
        }

        public static uint[] Repeat256(uint[] pixels256)
        {
            uint[] repeated = new uint[4096];
            for (uint x = 0; x < repeated.Length; x += 256)
                Array.Copy(pixels256, 0, repeated, x, 256);
            return repeated;
        }

        public static uint[] Tile(uint[] squareTexture, uint tileSqrt = 64)
        {
            uint side = (uint)System.Math.Sqrt(squareTexture.Length);
            uint newSide = side * tileSqrt;
            uint[] tiled = new uint[squareTexture.Length * tileSqrt * tileSqrt];
            for (uint x = 0; x < newSide; x++)
                for (uint y = 0; y < newSide; y++)
                    tiled[x * newSide + y] = squareTexture[x % side * side + y % side];
            return tiled;
        }

        public static byte[] Scale(byte[] squareTexture, int factor) => Int2ByteArray(Scale(Byte2IntArray(squareTexture), factor));

        public static uint[] Scale(uint[] squareTexture, int factor)
        {
            if (factor == 1) return squareTexture;
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

        /// <param name="bytes">rgba8888 color values (four bytes per pixel)</param>
        /// <returns>rgba8888 texture (one int per pixel)</returns>
        public static uint[] Byte2IntArray(byte[] bytes)
        {
            uint[] ints = new uint[bytes.Length / 4];
            for (uint i = 0; i < bytes.Length; i += 4)
                ints[i / 4] = (uint)(bytes[i] << 24) |
                    (uint)(bytes[i + 1] << 16) |
                    (uint)(bytes[i + 2] << 8) |
                    bytes[i + 3];
            return ints;
        }

        public static T[] ConcatArrays<T>(params T[][] list)
        {
            T[] result = new T[list.Sum(a => a.Length)];
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
