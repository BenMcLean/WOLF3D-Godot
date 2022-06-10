using Godot;
using System;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MiniMap : GridContainer
	{
		public readonly Container[][] Cell;
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
					Cell[x][z] = new Container()
					{
						Name = "MiniMap " + map.Name + " Container " + x + ", " + z,
						RectSize = cellSize,
					};
					if (mapAnalysis.IsTransparent(x, z))
					{
						if (mapAnalysis.Ground is byte ground)
							Cell[x][z].AddChild(new ColorRect()
							{
								Name = Name + " Floor at " + x + ", " + z,
								Color = Assets.Palettes[0][ground],
								RectSize = Cell[x][z].RectSize,
							});
						if (map.GetMapData(x, z) is ushort mapCell
							&& ushort.TryParse(Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door")
								?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == mapCell)
								?.FirstOrDefault()
								?.Attribute("Page")
								?.Value, out ushort page)
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& page < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[page] is AtlasTexture doorTexture)
							Cell[x][z].AddChild(new TextureRect()
							{
								Name = Name + " Door at " + x + ", " + z,
								Texture = doorTexture,
								RectSize = Cell[x][z].RectSize,
							});
						if (map.GetObjectData(x, z) is ushort cell
							&& ushort.TryParse(Assets.XML?.Element("VSwap")?.Element("Objects")?.Elements("Billboard")
								?.Where(e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == cell)
								?.FirstOrDefault()
								?.Attribute("Page")
								?.Value, out page)
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& page < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[page] is AtlasTexture atlasTexture)
							Cell[x][z].AddChild(new TextureRect()
							{
								Name = Name + " Billboard at " + x + ", " + z,
								Texture = atlasTexture,
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
			for (ushort z = 0; z < map.Depth; z++)
				for (ushort x = 0; x < map.Width; x++)
					AddChild(Cell[x][z]);
		}
	}
}
