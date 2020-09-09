using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    /// <summary>
    /// A symetric matrix of shorts whose diagonal elements are all equal to zero.
    /// Symetery is enforced by only storing the lower half of the matrix and ignoring any attempts to set the diagonal entries.
    /// Wolfenstein 3-D uses this data structure to keep track of noise propagation between the different rooms (floor codes) of a level.
    /// </summary>
    public class SymetricMatrix
    {
        private short[][] Data;

        public SymetricMatrix(uint size) => Size = size;

        public uint Size
        {
            get => (uint)(Data?.Length ?? 0);
            set
            {
                Data = new short[value][];
                Clear();
            }
        }

        public SymetricMatrix Clear()
        {
            for (uint i = 0; i < Size; i++)
                Data[i] = new short[i + 1];
            return this;
        }

        public short this[uint x, uint y]
        {
            get => x < y ?
                Data[y - 1][x]
                : x > y ?
                Data[x - 1][y]
                : (short)0;
            set
            {
                if (x < y)
                    Data[y - 1][x] = value;
                else if (x > y)
                    Data[x - 1][y] = value;
            }
        }

        public static uint CalcSize(uint n) => n * (n + 1) / 2;
        public static uint CalcSizeReversed(uint s) => (uint)Math.Floor(Math.Sqrt(2u * s));

        public override string ToString() => ToString(",");

        public string ToString(string separator, string rowSeparator = null)
        {
            List<string> rows = new List<string>();
            foreach (short[] row in Data)
                rows.Add(string.Join(separator, row));
            return string.Join(rowSeparator ?? separator, rows);
        }

        public string ToXMLTag(string separator = ",") => "<" + GetType().Name + " Data=\"" + ToString(separator) + "\" />";

        public SymetricMatrix(XElement e, char separator = ',') : this(e.Attribute("Data").Value, separator) { }

        public SymetricMatrix(string @string, char separator = ',') : this(CalcSizeReversed((uint)@string.Count(x => x == separator) + 1))
        {
            Queue<string> queue = new Queue<string>(@string.Split(separator));
            for (uint row = 0; row < Data.Length; row++)
                for (uint column = 0; column < Data[row].Length; column++)
                    Data[row][column] = short.Parse(queue.Dequeue());
        }

        public SymetricMatrix(SymetricMatrix other) : this(other.Size)
        {
            for (uint row = 0; row < Size; row++)
                Array.Copy(other.Data[row], Data[row], Data[row].Length);
        }

        public uint[] FloorCodes(uint start)
        {
            List<uint> results = new List<uint>();
            void DoFloor(uint floor)
            {
                if (!results.Contains(floor))
                {
                    results.Add(floor);
                    for (uint i = 0; i < Size; i++)
                        if (this[floor, i] > 0)
                            DoFloor(i);
                }
            }
            DoFloor(start);
            return results.ToArray();
        }
    }
}
