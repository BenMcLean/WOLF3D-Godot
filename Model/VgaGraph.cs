namespace WOLF3D.Model
{
    class VgaGraph
    {
        //HuffNode[] grhuffman = new HuffNode[255];

        /// <summary>
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
    }
}
