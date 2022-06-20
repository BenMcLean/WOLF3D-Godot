using System;

namespace WOLF3DTest.FovShadowCastTest
{
	public class ShadowCast
	{
		public interface ILit
		{
			ushort GetLitWidth();
			ushort GetLitDepth();
			bool IsLit(ushort x, ushort z);
			void SetLit(ushort x, ushort z, bool @bool = true);
		}
		public class LitBools : ILit
		{
			public bool[][] Bools { get; set; }
			public LitBools(bool[][] bools) => Bools = bools;
			public LitBools(LitBools other)
			{
				Bools = new bool[other.Bools.Length][];
				for (int x = 0; x < Bools.Length; x++)
				{
					Bools[x] = new bool[other.Bools[x].Length];
					Array.Copy(other.Bools[x], Bools[x], Bools[x].Length);
				}
			}
			public LitBools(ushort width, ushort depth)
			{
				Bools = new bool[width][];
				for (int x = 0; x < Bools.Length; x++)
					Bools[x] = new bool[depth];
			}
			public ushort GetLitWidth() => (ushort)Bools.Length;
			public ushort GetLitDepth() => (ushort)Bools[0].Length;
			public bool IsLit(ushort x, ushort y) => x < Bools.Length && y < Bools[x].Length && Bools[x][y];
			public void SetLit(ushort x, ushort y, bool @bool = true) => Bools[x][y] = @bool;
		}

	}
}
