using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WOLF3D
{
    static class FileStreamExtension
    {
        public static ushort ReadWord(this FileStream file)
        {
            return (ushort)(file.ReadByte() + (file.ReadByte() << 8));
        }

        public static int ReadSWord(this FileStream file)
        {
            return file.ReadByte() + (file.ReadByte() << 8);
        }

        public static uint ReadDWord(this FileStream file)
        {
            return file.ReadWord() + (uint)(file.ReadWord() << 16);
        }
    }

    public class VSwap
    {
        private static readonly uint COLORS = 256;

        public uint[] Palette { get; set; }
        public byte[][] Graphics { get; set; }
        public ushort Pages { get; set; }
        public ushort WallEndIndex { get; set; }
        public ushort SpritePageOffset { get; set; }
        public ushort SoundPageOffset { get; set; }
        public ushort GraphicChunks { get; set; }
        public ushort SpriteStartIndex { get; set; }
        public ushort SpriteEndIndex { get; set; }

        public VSwap Read(string vswap, ushort dimension = 64)
        {
            using (FileStream file = new FileStream(vswap, FileMode.Open))
            {
                // parse header info
                Pages = file.ReadWord();
                SpritePageOffset = file.ReadWord();
                SoundPageOffset = file.ReadWord();
                GraphicChunks = SoundPageOffset;
                uint[] pageOffsets = new uint[GraphicChunks];
                uint dataStart = 0;
                for (int x = 0; x < GraphicChunks; x++)
                {
                    pageOffsets[x] = file.ReadDWord();
                    if (x == 0)
                        dataStart = pageOffsets[0];
                    if (pageOffsets[x] != 0 && (pageOffsets[x] < dataStart || pageOffsets[x] > file.Length))
                        throw new InvalidDataException("VSWAP file '" + file.Name + "' contains invalid page offsets.");
                }
                ushort[] pageLengths = new ushort[Pages];
                for (ushort i = 0; i < Pages; i++)
                    pageLengths[i] = file.ReadWord();
                //uint maxPageLength = pageLengths.Max();
                //uint maxPageWidth = (uint)Math.Ceiling(Math.Sqrt(maxPageLength));

                // parse graphic data
                List<byte[]> graphics = new List<byte[]>();
                ushort page;
                // read in walls
                for (page = 0; page < SpritePageOffset; page++)
                {
                    file.Seek(pageOffsets[page], 0);
                    byte[] wall = new byte[dimension * dimension];
                    for (ushort col = 0; col < dimension; col++)
                        for (ushort row = 0; row < dimension; row++)
                            wall[dimension * row + col] = (byte)file.ReadByte();
                    graphics.Add(wall);
                }

                // read in sprites
                for (; page < GraphicChunks; page++)
                {
                    // TODO: ONLY FOR SHAREWARE
                    if (page == 293) page = 403;
                    if (page == 413) page = 514;
                    file.Seek(pageOffsets[page], 0);
                    ushort leftExtent = file.ReadWord(),
                        rightExtent = file.ReadWord(),
                        startY, endY;
                    byte[] sprite = new byte[dimension * dimension];
                    for (ushort i = 0; i < sprite.Length; i++)
                        sprite[i] = 255;
                    long[] columnDataOffsets = new long[rightExtent - leftExtent + 1];
                    for (uint i = 0; i < columnDataOffsets.Length; i++)
                        columnDataOffsets[i] = pageOffsets[page] + file.ReadWord();
                    long trexels = file.Position;
                    for (ushort column = 0; column <= rightExtent - leftExtent; column++)
                    {
                        long commands = columnDataOffsets[column];
                        file.Seek(commands, 0);
                        while ((endY = file.ReadWord()) != 0)
                        {
                            endY >>= 1;
                            file.ReadWord(); // Not using this value for anything. Don't know why it's here!
                            startY = file.ReadWord();
                            startY >>= 1;
                            commands = file.Position;
                            file.Seek(trexels, 0);
                            for (ushort row = startY; row < endY; row++)
                                sprite[(row * dimension - 1) + column + leftExtent - 1] = (byte)file.ReadByte();
                            trexels = file.Position;
                            file.Seek(commands, 0);
                        }
                    }
                    graphics.Add(sprite);
                }
                Graphics = graphics.ToArray();
            }
            return this;
        }

        public VSwap LoadPalette(string file)
        {
            Palette = MakePalette(file);
            return this;
        }

        public static uint[] MakePalette(string file)
        {
            uint[] result = new uint[COLORS];
            using (StreamReader input = new StreamReader(file))
            {
                if (!input.ReadLine().Equals("JASC-PAL") || !input.ReadLine().Equals("0100"))
                    throw new InvalidDataException("Palette \"" + file + "\" is an incorrectly formatted JASC palette.");
                if (!uint.TryParse(input.ReadLine(), out uint numColors)
                 || numColors != COLORS)
                    throw new InvalidDataException("Palette \"" + file + "\" does not contain exactly " + COLORS + " colors.");
                for (uint x = 0; x < numColors; x++)
                {
                    string[] tokens = input.ReadLine()?.Split(' ');
                    if (tokens == null || tokens.Length != 3
                        || !byte.TryParse(tokens[0], out byte r)
                        || !byte.TryParse(tokens[1], out byte g)
                        || !byte.TryParse(tokens[2], out byte b))
                        throw new InvalidDataException("Palette \"" + file + "\" is an incorrectly formatted JASC palette.");
                    result[x] = (uint)(r << 24)
                        + (uint)(g << 16)
                        + (uint)(b << 8)
                        + (uint)(x == 255 ? 0 : 255);
                }
            }
            return result;
        }

        public byte[] Index2ByteArray(byte[] index)
        {
            return Index2ByteArray(index, Palette);
        }

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
    }
}
