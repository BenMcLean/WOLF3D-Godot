using System;
using System.Linq;

namespace WOLF3DTest.ShadowCastTest
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
			public LitBools(string @string)
			{
				string[] lines = @string.Split(Environment.NewLine);
				Bools = new bool[lines[0].Count(c => c == ',') + 1][];
				for (int x = 0; x < Bools.Length; x++)
					Bools[x] = new bool[lines.Length];
				for (int z = 0; z < lines.Length; z++)
				{
					string[] split = lines[z].Split(",");
					for (int x = 0; x < Bools.Length; x++)
						Bools[x][z] = int.Parse(split[x]) != 0;
				}
			}
			public override string ToString() => string.Join(Environment.NewLine, Enumerable.Range(0, GetLitDepth()).Select(z => string.Join(",", Enumerable.Range(0, GetLitWidth()).Select(x => IsLit((ushort)x, (ushort)z) ? "1" : "0"))));
			public ushort GetLitWidth() => (ushort)Bools.Length;
			public ushort GetLitDepth() => (ushort)Bools[0].Length;
			public bool IsLit(ushort x, ushort z) => x < Bools.Length && z < Bools[x].Length && Bools[x][z];
			public void SetLit(ushort x, ushort z, bool @bool = true) => Bools[x][z] = @bool;
		}
	}
}
