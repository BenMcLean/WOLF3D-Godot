using System.Linq;
using System.Text;

namespace WOLF3D.WOLF3DGame.Action
{
    public class TriangularMatrix<T>
    {
        private T[][] Data;

        public TriangularMatrix(uint length) => Length = length;

        public uint Length
        {
            get => (uint)(Data?.Length ?? 0);
            set
            {
                Data = new T[value][];
                Clear();
            }
        }

        public TriangularMatrix<T> Clear()
        {
            for (uint i = 0; i < Length; i++)
                Data[i] = new T[i + 1];
            return this;
        }

        public T this[uint x, uint y]
        {
            get => x < y ?
                Data[y - 1][x]
                : Data[x - 1][y];
            set
            {
                if (x < y)
                    Data[y - 1][x] = value;
                else
                    Data[x - 1][y] = value;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (T[] row in Data)
                sb.AppendLine(string.Join(" ", row));
            return sb.ToString();
        }
    }
}
