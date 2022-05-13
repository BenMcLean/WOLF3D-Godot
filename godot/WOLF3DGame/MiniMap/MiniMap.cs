using Godot;
using System.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MiniMap : TileMap
	{
		public MiniMap(GameMap map, MapAnalyzer.MapAnalysis mapAnalysis)
		{
			TileSet = Assets.TileSet;
			CellSize = new Vector2(Assets.VSwap.TileSqrt, Assets.VSwap.TileSqrt);
			for (ushort x = 0; x < map.Width; x++)
				for (ushort z = 0; z < map.Depth; z++)
					if (mapAnalysis.IsMappable(x, z))
						if (mapAnalysis.IsTransparent(map.GetMapData(x, z), map.GetObjectData(x, z))
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
	}
}
