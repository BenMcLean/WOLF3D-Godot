using System;
using System.Collections.Generic;

namespace WOLF3D.WOLF3DGame.Action
{
	public struct MapRect
	{
		public int X;
		public int Z;
		public int Width;
		public int Depth;
		public static IEnumerable<MapRect> MapRects(bool[][] input)
		{
			bool[][] map = new bool[input.Length][];
			for (int i = 0; i < input.Length; i++)
			{
				map[i] = new bool[input[i].Length];
				Array.Copy(input[i], map[i], input[i].Length);
			}
			while (NextRect(map, input) is MapRect rect)
			{
				for (int x = rect.X; x < rect.X + rect.Width; x++)
					for (int z = rect.Z; z < rect.Z + rect.Depth; z++)
						map[x][z] = true;
				yield return rect;
			}
		}
		private static MapRect? NextRect(bool[][] map, bool[][] input)
		{
			if (!NextEmpty(map, out int x, out int z))
				return null;
			int width = 1;
			for (; x + width < input.Length && !input[x + width][z]; width++) { }
			bool done = false;
			int depth = 0;
			while (!done)
			{
				if (z + ++depth >= input[x].Length)
					done = true;
				int i = x - 1;
				while (!done && ++i < x + width)
					if (input[i][z + depth])
						done = true;
			}
			return new MapRect
			{
				X = x,
				Z = z,
				Width = width,
				Depth = depth,
			};
		}
		private static bool NextEmpty(bool[][] map, out int x, out int z)
		{
			z = 0;
			for (x = 0; x < map.Length; x++)
				for (z = 0; z < map[x].Length; z++)
					if (!map[x][z])
						return true;
			return false;
		}
	}
}
