using Godot;
using System.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MiniMap : TileMap
	{
		public MiniMap(GameMap map)
		{
			TileSet = Assets.TileSet;
			CellSize = new Vector2(Assets.VSwap.TileSqrt, Assets.VSwap.TileSqrt);
			for (ushort x = 0; x < map.Width; x++)
				for (ushort z = 0; z < map.Depth; z++)
					if (map.GetMapData(x, z) is ushort cell
						&& Assets.Walls.Contains(cell)
						&& Level.WallPage(cell) is ushort page)
						SetCell(x, z, page);
		}
	}
}
