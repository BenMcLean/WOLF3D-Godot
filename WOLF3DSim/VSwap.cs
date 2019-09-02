using System;
using System.IO;
using System.Linq;

namespace WOLF3DSim
{
    public static class StreamExtension
    {
        public static ushort ReadWord(this Stream stream)
        {
            return (ushort)(stream.ReadByte() + (stream.ReadByte() << 8));
        }

        public static uint ReadDWord(this Stream stream)
        {
            return stream.ReadWord() + (uint)(stream.ReadWord() << 16);
        }
    }

    public class VSwap
    {
        private static readonly ushort COLORS = 256;

        public uint[] Palette { get; set; }
        public byte[][] Pages { get; set; }

        /// <summary>
        /// C# expects file offsets to be long
        /// </summary>
        public long[] PageOffsets { get; set; }
        public ushort[] PageLengths { get; set; }
        public ushort SpritePage { get; set; }
        public ushort SoundPage { get; set; }

        public VSwap(StreamReader palette, Stream vswap)
        {
            Palette = LoadPalette(palette);
            Read(vswap);
        }

        public VSwap(uint[] palette, Stream vswap)
        {
            Palette = palette;
            Read(vswap);
        }

        public VSwap Read(Stream stream, ushort tileSqrt = 64)
        {
            if (Palette == null)
                throw new InvalidDataException("Must load a palette before loading a VSWAP!");
            // parse header info
            Pages = new byte[stream.ReadWord()][];
            SpritePage = stream.ReadWord();
            SoundPage = stream.ReadWord();

            PageOffsets = new long[Pages.Length];
            long dataStart = 0;
            for (ushort i = 0; i < PageOffsets.Length; i++)
            {
                PageOffsets[i] = stream.ReadDWord();
                if (i == 0)
                    dataStart = PageOffsets[0];
                if ((PageOffsets[i] != 0 && PageOffsets[i] < dataStart) || PageOffsets[i] > stream.Length)
                    throw new InvalidDataException("VSWAP contains invalid page offsets.");
            }
            PageLengths = new ushort[Pages.Length];
            for (ushort i = 0; i < PageLengths.Length; i++)
                PageLengths[i] = stream.ReadWord();

            ushort page;
            // read in walls
            for (page = 0; page < SpritePage; page++)
                if (PageOffsets[page] != 0)
                {
                    stream.Seek(PageOffsets[page], 0);
                    byte[] wall = new byte[tileSqrt * tileSqrt];
                    for (ushort col = 0; col < tileSqrt; col++)
                        for (ushort row = 0; row < tileSqrt; row++)
                            wall[tileSqrt * row + col] = (byte)stream.ReadByte();
                    Pages[page] = Index2ByteArray(wall);
                }

            // read in sprites
            for (; page < SoundPage; page++)
                if (PageOffsets[page] != 0)
                {
                    stream.Seek(PageOffsets[page], 0);
                    ushort leftExtent = stream.ReadWord(),
                        rightExtent = stream.ReadWord(),
                        startY, endY;
                    byte[] sprite = new byte[tileSqrt * tileSqrt];
                    for (ushort i = 0; i < sprite.Length; i++)
                        sprite[i] = 255; // set transparent
                    long[] columnDataOffsets = new long[rightExtent - leftExtent + 1];
                    for (ushort i = 0; i < columnDataOffsets.Length; i++)
                        columnDataOffsets[i] = PageOffsets[page] + stream.ReadWord();
                    long trexels = stream.Position;
                    for (ushort column = 0; column <= rightExtent - leftExtent; column++)
                    {
                        long commands = columnDataOffsets[column];
                        stream.Seek(commands, 0);
                        while ((endY = stream.ReadWord()) != 0)
                        {
                            endY >>= 1;
                            stream.ReadWord(); // Not using this value for anything. Don't know why it's here!
                            startY = stream.ReadWord();
                            startY >>= 1;
                            commands = stream.Position;
                            stream.Seek(trexels, 0);
                            for (ushort row = startY; row < endY; row++)
                                sprite[(row * tileSqrt - 1) + column + leftExtent - 1] = (byte)stream.ReadByte();
                            trexels = stream.Position;
                            stream.Seek(commands, 0);
                        }
                    }
                    Pages[page] = Index2ByteArray(sprite);
                }

            // read in sounds
            for (; page < Pages.Length; page++)
                if (PageOffsets[page] != 0)
                {
                    stream.Seek(PageOffsets[page], 0);
                    Pages[page] = new byte[PageLengths[page]];
                    for (uint i = 0; i < Pages[page].Length; i++)
                        Pages[page][i] = (byte)(stream.ReadByte() - 128); // Godot makes some kind of oddball conversion from the unsigned byte to a signed byte
                }
            return this;
        }

        public static uint[] LoadPalette(StreamReader stream)
        {
            if (!stream.ReadLine().Equals("JASC-PAL") || !stream.ReadLine().Equals("0100"))
                throw new InvalidDataException("Palette stream is an incorrectly formatted JASC palette.");
            if (!uint.TryParse(stream.ReadLine(), out uint numColors)
             || numColors != COLORS)
                throw new InvalidDataException("Palette stream does not contain exactly " + COLORS + " colors.");

            uint[] result = new uint[numColors];
            for (uint x = 0; x < numColors; x++)
            {
                string[] tokens = stream.ReadLine()?.Split(' ');
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

        private byte[] paletteTextureRepeated;

        public byte[] PaletteTextureRepeated
        {
            get
            {
                if (Palette == null)
                    throw new InvalidDataException("Palette is null!");
                return paletteTextureRepeated ?? (paletteTextureRepeated = Int2ByteArray(Repeat256(Palette)));
            }
        }

        public static uint[] Repeat256(uint[] pixels256)
        {
            uint[] repeated = new uint[4096];
            for (uint x = 0; x < repeated.Length; x += 256)
                Array.Copy(pixels256, 0, repeated, x, 256);
            return repeated;
        }

        private byte[] paletteTextureTiled;

        public byte[] PaletteTextureTiled
        {
            get
            {
                if (Palette == null)
                    throw new InvalidDataException("Palette is null!");
                return paletteTextureTiled ?? (paletteTextureTiled = Int2ByteArray(Tile(Palette, 4)));
            }
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

        private byte[] paletteTexture;

        public byte[] PaletteTexture
        {
            get
            {
                if (Palette == null)
                    throw new InvalidDataException("Palette is null!");
                return paletteTexture ?? (paletteTexture = Int2ByteArray(Scale(Palette, 4)));
            }
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
