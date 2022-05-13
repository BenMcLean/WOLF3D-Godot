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
					if (IsMappable(map, x, z))
						if (!Assets.IsTransparent(map.GetMapData(x, z), map.GetObjectData(x, z))
							&& map.GetMapData(x, z) is ushort cell
							&& Assets.Walls.Contains(cell)
							&& Level.WallPage(cell) is ushort page
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& page < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[page] is AtlasTexture)
							SetCell(x, z, page);
			/*
						else if (map.GetObjectData(x, z) is ushort cell2
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& cell2 < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[cell2] is AtlasTexture)
							SetCell(x, z, cell2);
			*/
		}
		public bool[][] IsMappable(GameMap map)
		{
			bool[][] result = new bool[map.Width][];
			for (ushort x = 0; x < map.Width; x++)
			{
				result[x] = new bool[map.Depth];
				for (ushort z = 0; z < map.Depth; z++)
					result[x][z] = IsMappable(map, x, z);
			}
			return result;
		}
		public bool IsMappable(GameMap map, ushort x, ushort z) =>
			Assets.IsTransparent(map.GetMapData(x, z), map.GetObjectData(x, z))
			|| (x > 0 && Assets.IsTransparent(map.GetMapData((ushort)(x - 1), z), map.GetObjectData((ushort)(x - 1), z)))
			|| (x < map.Width - 1 && Assets.IsTransparent(map.GetMapData((ushort)(x + 1), z), map.GetObjectData((ushort)(x + 1), z)))
			|| (z > 0 && Assets.IsTransparent(map.GetMapData(x, (ushort)(z - 1)), map.GetObjectData(x, (ushort)(z - 1))))
			|| (z < map.Depth - 1 && Assets.IsTransparent(map.GetMapData(x, (ushort)(z + 1)), map.GetObjectData(x, (ushort)(z + 1))));
	}
}
