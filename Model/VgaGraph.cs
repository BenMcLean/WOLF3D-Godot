namespace WOLF3D.Model
{
    class VgaGraph
    {
        //HuffNode[] grhuffman = new HuffNode[255];

        public struct HuffNode
        {
            /// <summary>
            /// 0-255 is a character, > is a pointer to a node
            /// </summary>
            public ushort bit0, bit1;
        }

        /// <summary>
        /// Translated from https://github.com/mozzwald/wolf4sdl/blob/master/id_ca.cpp#L214-L260
        /// </summary>
        public static byte[] CAL_HuffExpand(byte[] source, uint length, HuffNode[] huffTable)
        {
            byte[] dest = new byte[length];
            HuffNode huffNode = huffTable[254];
            uint read = 0, written = 0;
            ushort nodeVal;
            byte val = source[read++], mask = 1;
            do
            {
                if ((val & mask) == 0)
                    nodeVal = huffNode.bit0;
                else
                    nodeVal = huffNode.bit1;
                if (mask == 0x80)
                {
                    val = source[read++];
                    mask = 1;
                }
                else mask <<= 1;
                if (nodeVal < 256)
                {
                    dest[written++] = (byte)nodeVal;
                    huffNode = huffTable[254];
                }
                else
                    huffNode = huffTable[nodeVal - 256];
            }
            while (written < dest.Length);
            return dest;
        }
    }
}
