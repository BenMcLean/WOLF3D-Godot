using System.IO;

namespace WOLF3D
{
    class VgaGraph
    {
        /// <summary>
        /// Implementing Huffman decompression. http://www.shikadi.net/moddingwiki/Huffman_Compression#Huffman_implementation_in_ID_Software_games
        /// Translated from https://github.com/mozzwald/wolf4sdl/blob/master/id_ca.cpp#L214-L260
        /// </summary>
        /// <param name="dictionary">The Huffman dictionary is a ushort[255][2]</param>
        public static byte[] CAL_HuffExpand(byte[] source, uint length, ushort[][] dictionary)
        {
            byte[] dest = new byte[length];
            ushort[] huffNode = dictionary[254];
            uint read = 0, written = 0;
            ushort nodeVal;
            byte val = source[read++], mask = 1;
            while (written < dest.Length)
            {
                if ((val & mask) == 0)
                    nodeVal = huffNode[0];
                else
                    nodeVal = huffNode[1];
                if (mask == 0x80)
                {
                    val = source[read++];
                    mask = 1;
                }
                else
                    mask <<= 1;
                if (nodeVal < 256)
                { // 0-255 is a character, > is a pointer to a node
                    dest[written++] = (byte)nodeVal;
                    huffNode = dictionary[254];
                }
                else
                    huffNode = dictionary[nodeVal - 256];
            }
            return dest;
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
