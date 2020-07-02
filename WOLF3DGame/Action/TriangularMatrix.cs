namespace WOLF3D.WOLF3DGame.Action
{
    public class TriangularMatrix<T>
    {
        private T[][] Data;

        public TriangularMatrix(uint length)
        {
            Length = length;
            Clear();
        }

        public uint Length
        {
            get => (uint)(Data?.Length ?? 0);
            set => Data = new T[value][];
        }

        public TriangularMatrix<T> Clear()
        {
            for (uint i = 1; i < Length; i++)
                Data[i] = new T[i - 1];
            return this;
        }

        public T this[uint x, uint y]
        {
            get => x < y ?
                Data[y][x]
                : Data[x][y];
            set
            {
                if (x < y)
                    Data[y][x] = value;
                else
                    Data[x][y] = value;
            }
        }
    }
}
