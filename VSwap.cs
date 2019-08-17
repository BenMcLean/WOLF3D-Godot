using System.Collections.Generic;
using System.IO;

namespace WOLF3D
{
    public class VSwap
    {
        private static readonly int CARMACK_NEAR = 0xA7;
        private static readonly int CARMACK_FAR = 0xA8;
        private static readonly uint COLORS = 256;

        public uint[] Palette { get; set; }
        public List<byte[]> Graphics { get; set; }
        public uint WallStartIndex { get; set; }
        public uint WallEndIndex { get; set; }
        public uint SpritePageOffset { get; set; }
        public uint SoundPageOffset { get; set; }
        public uint GraphicChunks { get; set; }
        public uint SpriteStartIndex { get; set; }
        public uint SpriteEndIndex { get; set; }

        public static uint ReadWord(FileStream file)
        {
            return (uint)file.ReadByte() + (uint)(file.ReadByte() << 8);
        }

        public static uint ReadDWord(FileStream file)
        {
            return ReadWord(file) + (ReadWord(file) << 16);
        }

        public VSwap Read(FileStream file, uint dimension=64)
        {
            // parse header info
            uint chunks = ReadWord(file);
            SpritePageOffset = ReadWord(file);
            SoundPageOffset = ReadWord(file);
            GraphicChunks = SoundPageOffset;
            uint[] pageOffsets = new uint[GraphicChunks];
            uint dataStart = 0;

            for (int x = 0; x < GraphicChunks; x++)
            {
                pageOffsets[x] = ReadDWord(file);
                if (x == 0)
                    dataStart = pageOffsets[0];
                if (pageOffsets[x] != 0 && (pageOffsets[x] < dataStart || pageOffsets[x] > file.Length))
                    throw new InvalidDataException("VSWAP file '" + file.Name + "' contains invalid page offsets.");
            }

            // parse graphic data
            Graphics = new List<byte[]>();
            uint page;
            // read in walls
            WallStartIndex = 0;
            for (page = WallStartIndex; page < SpritePageOffset; page++)
            {
                file.Seek(pageOffsets[page], 0);
                byte[] wall = new byte[dimension * dimension];
                for (int col = 0; col < dimension; col++)
                    for (int row = 0; row < dimension; row++)
                        wall[dimension * row + col] = (byte)file.ReadByte();
                Graphics.Add(wall);
            }

            // read in sprites
            //        for (; page<graphicChunks; page++) {
            //            file.Seek(pageOffsets[page], 0);
            //// https://devinsmith.net/backups/bruce/wolf3d.html
            //        }
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
                if (!int.TryParse(input.ReadLine(), out int numColors)
                 || numColors != COLORS)
                    throw new InvalidDataException("Palette \"" + file + "\" does not contain exactly " + COLORS + " colors.");
                for (int x = 0; x < numColors; x++)
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
            byte[] bytes = new byte[index.Length * 4];
            for (uint i = 0; i < index.Length; i++)
            {
                bytes[i * 4] = (byte)(Palette[index[i]] >> 24);
                bytes[i * 4 + 1] = (byte)(Palette[index[i]] >> 16);
                bytes[i * 4 + 2] = (byte)(Palette[index[i]] >> 8);
                bytes[i * 4 + 3] = (byte)Palette[index[i]];
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

        public static int[] RlewExpand(int[] carmackExpanded, int length, int tag)
        {
            int[] rawMapData = new int[length];
            int src_index = 1, dest_index = 0;
            do
            {
                int value = carmackExpanded[src_index++]; // WORDS!!
                if (value != tag)
                    // uncompressed
                    rawMapData[dest_index++] = value;
                else
                {
                    // compressed string
                    int count = carmackExpanded[src_index++];
                    value = carmackExpanded[src_index++];
                    for (int i = 1; i <= count; i++)
                        rawMapData[dest_index++] = value;
                }
            } while (dest_index < length);
            return rawMapData;
        }

        public static uint[] CarmackExpand(long position, FileStream file)
        {
            ////////////////////////////
            // Get to the correct chunk
            uint length;
            uint ch, chhigh, count, offset, index = 0;
            file.Seek(position, 0);
            // First word is expanded length
            length = ReadWord(file);
            uint[] expandedWords = new uint[length]; // array of WORDS
            length /= 2;
            while (length > 0)
            {
                ch = ReadWord(file);
                chhigh = ch >> 8;
                if (chhigh == CARMACK_NEAR)
                {
                    count = (ch & 0xFF);
                    if (count == 0)
                    {
                        ch |= (uint)file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = (uint)file.ReadByte();
                        length -= count;
                        if (length < 0)
                            return expandedWords;
                        while ((count--) > 0)
                        {
                            expandedWords[index] = expandedWords[index - offset];
                            index++;
                        }
                    }
                }
                else if (chhigh == CARMACK_FAR)
                {
                    count = (ch & 0xFF);
                    if (count == 0)
                    {
                        ch |= (uint)file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = ReadWord(file);
                        length -= count;
                        if (length < 0)
                            return expandedWords;
                        while ((count--) > 0)
                            expandedWords[index++] = expandedWords[offset++];
                    }
                }
                else
                {
                    expandedWords[index++] = ch;
                    length--;
                }
            }
            return expandedWords;
        }
    }
}
