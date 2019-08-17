using System.Collections.Generic;
using System.IO;

namespace WOLF3D
{
    static class FileStreamExtension
    {
        public static uint ReadWord(this FileStream file)
        {
            return (uint)file.ReadByte() + (uint)(file.ReadByte() << 8);
        }

        public static uint ReadDWord(this FileStream file)
        {
            return file.ReadWord() + (file.ReadWord() << 16);
        }
    }

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

        public VSwap Read(FileStream file, uint dimension = 64)
        {
            // parse header info
            uint chunks = file.ReadWord();
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
            for (; page < GraphicChunks; page++)
            {
                file.Seek(pageOffsets[page], 0);
                // https://devinsmith.net/backups/bruce/wolf3d.html
                // Each sprite is a 64 texel wide and 64 texel high block.
                byte[] sprite = new byte[dimension * dimension];
                for (uint i = 0; i < sprite.Length; i++)
                    sprite[i] = 255;

                // It is a sparse array and is packed as RLE columns. The first part of the sprite is two short integers (two bytes each) that tell the left and right extents of the sprite. By extents I mean that the left extent is the first column on the left with a colored texel in it. And the right extent is the last column on the right that has a colored texel in it.
                uint leftExtent = file.ReadWord(),
                    rightExtent = file.ReadWord();

                // Immediately after this data (four bytes into the sprite) is a list of two byte offsets into the file that has the drawing information for each of those columns. The first is the offset for the left extent column and the last is the offset for the right extent column with all the other column offsets stored sequentially between them.
                uint[] drawingInfoOffsets = new uint[rightExtent - leftExtent];
                for (uint i = 0; i < drawingInfoOffsets.Length; i++)
                    drawingInfoOffsets[i] = file.ReadWord();

                // The area between the end of the column segment offset list and the first column drawing instructions is the actual texels of the sprite.
                long trexelsOffset = file.Position;

                // Now comes the interesting part. Each of these offsets, points to a possible list of drawing commands for the scalers to use to draw the sprite. Each column segment instruction is a series of 6 bytes. If the first two bytes of the column segment instructions is zero, then that is the end of that column and we can move on to the next column.
                List<List<byte[]>> drawingCommands = new List<List<byte[]>>();
                foreach (uint offset in drawingInfoOffsets)
                {
                    file.Seek(offset, 0);
                    List<byte[]> columnSegments = new List<byte[]>();
                    byte a, b;
                    do
                    {
                        a = (byte)file.ReadByte();
                        b = (byte)file.ReadByte();
                        columnSegments.Add(new byte[] {
                            a,
                            b,
                            (byte)file.ReadByte(),
                            (byte)file.ReadByte(),
                            (byte)file.ReadByte(),
                            (byte)file.ReadByte()
                        });
                    } while (a != 0 && b != 0);
                    drawingCommands.Add(columnSegments);
                }

                // To interpret these columns was the tricky part. Each of these offsets points to an offset into an array of short unsigned integers in the original game which are the offsets of individual rows in the unwound column drawers.

                file.Seek(trexelsOffset, 0);
                for (uint column = 0; column < drawingCommands.Count; column++)
                    foreach (byte[] segment in drawingCommands[(int)column])
                    {
                        // So if we take the starting position (which is the first two bytes) and divide it by two, we have one end of the column segment.
                        uint startingPosition = (segment[0] + (uint)(segment[1] << 8)) / 2;

                        // The other end of that segment is the last two bytes(of the six byte instruction) and we also divide that by two to get the ending position of that column segment.
                        uint endingPosition = (segment[4] + (uint)(segment[5] << 8)) / 2;

                        // But where do we get the texels to draw from?
                        // The area between the end of the column segment offset list and the first column drawing instructions is the actual texels of the sprite. Only the colored texels are stored here and they are stored sequentially as are the column drawing instructions. There is a one to one correspondence between each drawing instruction and the texels stored here. Each column segment's height uses that many texels from this pool of texels.
                        for (uint row = startingPosition; row < endingPosition; row++)
                            sprite[(column + leftExtent) * dimension + row] = (byte)file.ReadByte();
                    }
                Graphics.Add(sprite);
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
            length = file.ReadWord();
            uint[] expandedWords = new uint[length]; // array of WORDS
            length /= 2;
            while (length > 0)
            {
                ch = file.ReadWord();
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
                        offset = file.ReadWord();
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
