using Godot;
using System;
using System.Linq;
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
					ushort mapData = map.GetMapData(x, z),
						objectData = map.GetObjectData(x, z);
					Cell[x][z] = new Container()
					{
						Name = "MiniMap " + map.Name + " Container " + x + ", " + z,
						RectSize = cellSize,
					};
					if (mapAnalysis.IsTransparent(x, z) && !Assets.MapAnalyzer.PushWalls.Contains(objectData))
					{
						if (mapAnalysis.Ground is byte ground)
							Cell[x][z].AddChild(new ColorRect()
							{
								Name = Name + " Floor at " + x + ", " + z,
								Color = Assets.Palettes[0][ground],
								RectSize = Cell[x][z].RectSize,
							});
						if (ushort.TryParse(Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door")
								?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == mapData)
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
								RectSize = doorTexture.GetSize(),
							});
						else if (ushort.TryParse((
								Assets.XML?.Element("VSwap")?.Element("Objects")?.Elements("Billboard")
								?.Where(e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == objectData)
								?.FirstOrDefault()
								)
								?.Attribute("Page")
								?.Value, out page)
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& page < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[page] is AtlasTexture billboardTexture)
							Cell[x][z].AddChild(new TextureRect()
							{
								Name = Name + " Billboard at " + x + ", " + z,
								Texture = billboardTexture,
								RectSize = billboardTexture.GetSize(),
							});
					}
					else if (mapAnalysis.IsMappable(x, z))
						if (Assets.MapAnalyzer.Walls.Contains(mapData)
							&& Assets.MapAnalyzer.WallPage(mapData) is ushort wallPage
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& wallPage < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[wallPage] is AtlasTexture wallTexture)
							Cell[x][z].AddChild(new TextureRect()
							{
								Name = Name + " Wall at " + x + ", " + z,
								Texture = wallTexture,
								RectSize = wallTexture.GetSize(),
							});
						else if (Assets.MapAnalyzer.Elevators.Contains(mapData)
							&& ushort.TryParse(Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Elevator")
								?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == mapData)
								?.FirstOrDefault()
								?.Attribute("DarkSide")
								?.Value, out ushort page)
							&& Assets.VSwapAtlasTextures is AtlasTexture[]
							&& page < Assets.VSwapAtlasTextures.Length
							&& Assets.VSwapAtlasTextures[page] is AtlasTexture elevatorTexture)
							Cell[x][z].AddChild(new TextureRect()
							{
								Name = Name + " Elevator at " + x + ", " + z,
								Texture = elevatorTexture,
								RectSize = elevatorTexture.GetSize(),
							});
				}
			}
			for (ushort z = 0; z < map.Depth; z++)
				for (ushort x = 0; x < map.Width; x++)
					AddChild(Cell[x][z]);
		}
	}
}
