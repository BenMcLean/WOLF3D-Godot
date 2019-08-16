using System.IO;

namespace WOLF3D.Graphics
{
    public class WolfFileWrapper
    {
        private static readonly int CARMACK_NEAR = 0xA7;
        private static readonly int CARMACK_FAR = 0xA8;
        private static readonly int WORD_LENGTH = 2;
        private static readonly int DWORD_LENGTH = WORD_LENGTH * 2;

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

        public static int ReadWord(FileStream file)
        {
            return (file.ReadByte() << 8) + file.ReadByte();
        }

        public static int[] CarmackExpand(long position, FileStream file)
        {
            ////////////////////////////
            // Get to the correct chunk
            int length;
            int ch, chhigh, count, offset, index=0;
            file.Seek(position, 0);
            // First word is expanded length
            length = ReadWord(file);
            int[] expandedWords = new int[length]; // array of WORDS
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
                        ch |= file.ReadByte();
                        expandedWords[index++] = ch;
                        length--;
                    }
                    else
                    {
                        offset = file.ReadByte();
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
                        ch |= file.ReadByte();
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
