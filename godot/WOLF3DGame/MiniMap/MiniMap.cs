using Godot;
using GoRogue;
using GoRogue.MapViews;
using System;
using System.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MiniMap : GridContainer, IMapView<bool>
	{
		public readonly Container[][] Cell;
		private readonly static Vector2 cellSize = new Vector2(Assets.VSwap.TileSqrt, Assets.VSwap.TileSqrt);
		public readonly GameMap GameMap;
		public readonly MapAnalyzer.MapAnalysis MapAnalysis;
		public MiniMap(GameMap gameMap, MapAnalyzer.MapAnalysis mapAnalysis)
		{
			GameMap = gameMap;
			MapAnalysis = mapAnalysis;
			Name = "MiniMap " + GameMap.Name;
			Columns = GameMap.Width;
			Set("custom_constants/hseparation", Assets.VSwap.TileSqrt);
			Set("custom_constants/vseparation", Assets.VSwap.TileSqrt);
			Cell = new Container[GameMap.Width][];
			for (ushort x = 0; x < GameMap.Width; x++)
			{
				Cell[x] = new Container[GameMap.Depth];
				for (ushort z = 0; z < GameMap.Depth; z++)
				{
					ushort mapData = GameMap.GetMapData(x, z),
						objectData = GameMap.GetObjectData(x, z);
					Cell[x][z] = new Container()
					{
						Name = "MiniMap " + GameMap.Name + " Container " + x + ", " + z,
						RectSize = cellSize,
						//Modulate = invisibleColor,
					};
					if (MapAnalysis.IsMappable(x, z))
						if (MapAnalysis.IsTransparent(x, z) && !Assets.MapAnalyzer.PushWalls.Contains(objectData))
						{
							if (MapAnalysis.Ground is byte ground)
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
						else if (Assets.MapAnalyzer.Walls.Contains(mapData)
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
			for (ushort z = 0; z < GameMap.Depth; z++)
				for (ushort x = 0; x < GameMap.Width; x++)
					AddChild(Cell[x][z]);
		}
		#region IMapView<bool>
		public int Height => GameMap.Depth;

		public int Width => GameMap.Width;

		public bool this[int index1D] => IsVisible(GameMap.X((uint)index1D), GameMap.Z((uint)index1D));

		public bool this[Coord pos] => IsVisible((ushort)pos.X, (ushort)pos.Y);

		public bool this[int x, int y] => IsVisible((ushort)x, (ushort)y);
		#endregion IMapView<bool>
		#region Visibility
		public bool IsVisible(ushort x, ushort z) => MapAnalysis.IsMappable(x, z) && x < Cell.Length && z < Cell[x].Length && Cell[x][z].Modulate.a > 0.5f;
		private readonly static Color visibleColor = new Color(1f, 1f, 1f, 1f);
		private readonly static Color invisibleColor = new Color(0f, 0f, 0f, 0f);
		public MiniMap SetVisible(ushort x, ushort z, bool visible = true)
		{
			if (MapAnalysis.IsMappable(x, z) && x < Cell.Length && z < Cell[x].Length)
				Cell[x][z].Modulate = visible ? visibleColor : invisibleColor;
			return this;
		}
		public MiniMap SetInvisible(ushort x, ushort z) => SetVisible(x, z, false);
		public MiniMap Illuminate(bool[][] lit)
		{
			for (ushort x = 0; x < lit.Length; x++)
				for (ushort z = 0; z < lit[x].Length; z++)
					if (lit[x][z])
						SetVisible(x, z);
			return this;
		}
		#endregion Visibility
	}
}
