using System.Collections.Generic;
using System.IO;

namespace OPL
{
    /// <summary>
    /// Parses and stores IMF format music data. http://www.shikadi.net/moddingwiki/IMF_Format
    /// </summary>
    public static class Imf
    {
        /// <summary>
        /// A song is just an array of ImfPackets
        /// </summary>
        public struct ImfPacket
        {
            /// <summary>
            /// Sent to register port.
            /// </summary>
            public byte Register { get; set; }

            /// <summary>
            /// Sent to data port.
            /// </summary>
            public byte Data { get; set; }

            /// <summary>
            /// How much to wait.
            /// </summary>
            public ushort Delay { get; set; }

            public ImfPacket(BinaryReader binaryReader)
            {
                Register = binaryReader.ReadByte();
                Data = binaryReader.ReadByte();
                Delay = binaryReader.ReadUInt16();
            }
        }

        /// <summary>
        /// Wolf3D song notes happen at 700 hz.
        /// </summary>
        /// <param name="time">Delay value read from IMF</param>
        public static float Delay(ushort time)
        {
            return time / 700f;
        }

        /// <summary>
        /// Parsing IMF files based on http://www.shikadi.net/moddingwiki/IMF_Format
        /// </summary>
        public static ImfPacket[] ReadImf(Stream stream)
        {
            ImfPacket[] imf;
            using (BinaryReader binaryReader = new BinaryReader(stream))
            {
                ushort length = (ushort)(binaryReader.ReadUInt16() / 4); // Length is provided in number of bytes. Divide by 4 to get the number of 4 byte packets.
                if (length == 0)
                { // Type-0 format
                    stream.Seek(0, 0);
                    List<ImfPacket> list = new List<ImfPacket>();
                    while (stream.Position < stream.Length)
                        list.Add(new ImfPacket(binaryReader));
                    imf = list.ToArray();
                }
                else
                { // Type-1 format
                    imf = new ImfPacket[length];
                    for (uint i = 0; i < imf.Length; i++)
                        imf[i] = new ImfPacket(binaryReader);
                }
            }
            return imf;
        }
    }
}
