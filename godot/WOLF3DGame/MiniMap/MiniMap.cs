using Godot;
using System.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MiniMap : GridContainer
	{
		public Container[][] Cell;
		private readonly Vector2 cellSize = new Vector2(Assets.VSwap.TileSqrt, Assets.VSwap.TileSqrt);
		public MiniMap(GameMap map, MapAnalyzer.MapAnalysis mapAnalysis)
		{
			Name = "MiniMap " + map.Name;
			Columns = map.Width;
			Set("custom_constants/hseparation", Assets.VSwap.TileSqrt);
			Set("custom_constants/vseparation", Assets.VSwap.TileSqrt);
			Cell = new Container[map.Width][];
			for (ushort x = 0; x < map.Width; x++)
			{
				Cell[x] = new Container[map.Depth];
				for (ushort z = 0; z < map.Depth; z++)
				{
					AddChild(Cell[x][z] = new Container()
					{
						Name = "MiniMap " + map.Name + " Container " + x + ", " + z,
						RectSize = cellSize,
					});
					if (mapAnalysis.IsTransparent(x, z))
					{
						if (mapAnalysis.Ground is byte ground)
							Cell[x][z].AddChild(new ColorRect()
							{
								Name = Name + " Floor at " + x + ", " + z,
								Color = Assets.Palettes[0][ground],
								RectSize = Cell[x][z].RectSize,
							});
					}
					else if (mapAnalysis.IsMappable(x, z)
						&& map.GetMapData(x, z) is ushort cell
						&& Assets.MapAnalyzer.Walls.Contains(cell)
						&& Assets.MapAnalyzer.WallPage(cell) is ushort page
						&& Assets.VSwapAtlasTextures is AtlasTexture[]
						&& page < Assets.VSwapAtlasTextures.Length
						&& Assets.VSwapAtlasTextures[page] is AtlasTexture atlasTexture)
						Cell[x][z].AddChild(new TextureRect()
						{
							Name = Name + " Wall at " + x + ", " + z,
							Texture = atlasTexture,
							RectSize = Cell[x][z].RectSize,
						});
				}
			}
		}
	}
}
