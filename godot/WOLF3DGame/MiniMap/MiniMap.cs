using Godot;
using GoRogue;
using GoRogue.MapViews;
using System;
using System.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.MiniMap
{
	public class MiniMap : GridContainer
	{
		public readonly Container[][] Cell;
		private readonly static Vector2 cellSize = new Vector2(Assets.VSwap.TileSqrt, Assets.VSwap.TileSqrt);

		public readonly MapAnalyzer.MapAnalysis MapAnalysis;
		public GameMap GameMap => MapAnalysis.GameMap;
		public FOV FOV;
		public MiniMap(Level level) : this(level.MapAnalysis) => FOV = new FOV(level);
		public MiniMap(MapAnalyzer.MapAnalysis mapAnalysis, bool[][] map) : this(mapAnalysis, new MapViewBools(map)) { }
		public MiniMap(MapAnalyzer.MapAnalysis mapAnalysis, IMapView<bool> map) : this(mapAnalysis) => FOV = new FOV(map);
		protected MiniMap(MapAnalyzer.MapAnalysis mapAnalysis)
		{
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
						Modulate = invisibleColor,
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
		public MiniMap Cheat()
		{
			for (ushort x = 0; x < MapAnalysis.GameMap.Width; x++)
				for (ushort z = 0; z < MapAnalysis.GameMap.Depth; z++)
					SetVisible(x, z);
			return this;
		}
		public MiniMap Illuminate(ushort startX, ushort startZ)
		{
			FOV.Calculate(startX, startZ);
			foreach (Coord point in FOV.NewlySeen)
				SetVisible((ushort)point.X, (ushort)point.Y);
			return this;
		}
		#endregion Visibility
		#region Test classes
		public class MapViewBools : IMapView<bool>
		{
			#region IMapView<bool>
			public bool this[Coord pos] => Is((ushort)pos.X, (ushort)pos.Y);
			public bool this[int index1D] => Is((ushort)(index1D % Width), (ushort)(index1D / Height));
			public bool this[int x, int y] => Is((ushort)x, (ushort)y);
			public int Height => Bools[0].Length;
			public int Width => Bools.Length;
			#endregion IMapView<bool>
			public bool[][] Bools { get; set; }
			public MapViewBools(bool[][] bools) => Bools = bools;
			public MapViewBools(MapViewBools other)
			{
				Bools = new bool[other.Bools.Length][];
				for (int x = 0; x < Bools.Length; x++)
				{
					Bools[x] = new bool[other.Bools[x].Length];
					Array.Copy(other.Bools[x], Bools[x], Bools[x].Length);
				}
			}
			public MapViewBools(ushort width, ushort depth)
			{
				Bools = new bool[width][];
				for (int x = 0; x < Bools.Length; x++)
					Bools[x] = new bool[depth];
			}
			public MapViewBools(string @string)
			{
				string[] lines = @string.Split(System.Environment.NewLine);
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
			public override string ToString() => string.Join(System.Environment.NewLine, Enumerable.Range(0, Height).Select(z => string.Join(",", Enumerable.Range(0, Width).Select(x => Is((ushort)x, (ushort)z) ? "1" : "0"))));
			public bool Is(ushort x, ushort z) => x < Bools.Length && z < Bools[x].Length && Bools[x][z];
			public void Set(ushort x, ushort z, bool @bool = true) => Bools[x][z] = @bool;
		}
		public class MapView : IMapView<bool>
		{
			#region IMapView<bool>
			public bool this[Coord pos] => MapAnalysis.IsTransparent(pos.X, pos.Y);
			public bool this[int index1D] => MapAnalysis.IsTransparent(MapAnalysis.GameMap.X((ushort)index1D), MapAnalysis.GameMap.Z((ushort)index1D));
			public bool this[int x, int y] => MapAnalysis.IsTransparent(x, y);
			public int Height => MapAnalysis.GameMap.Depth;
			public int Width => MapAnalysis.GameMap.Width;
			#endregion IMapView<bool>
			public readonly MapAnalyzer.MapAnalysis MapAnalysis;
			public MapView(MapAnalyzer.MapAnalysis mapAnalysis) => MapAnalysis = mapAnalysis;
		}
		#endregion Test classes
	}
}
