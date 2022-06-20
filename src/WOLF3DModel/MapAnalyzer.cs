using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DModel;

namespace WOLF3DModel
{
	public class MapAnalyzer
	{
		public XElement XML;
		public ushort[] Walls { get; private set; }
		public ushort[] Doors { get; private set; }
		public ushort[] Elevators { get; private set; }
		public ushort[] PushWalls { get; private set; }
		public MapAnalyzer(XElement xml)
		{
			XML = xml;
			Walls = XML.Element("VSwap")?.Element("Walls")?.Elements("Wall").Select(e => ushort.Parse(e.Attribute("Number").Value)).ToArray();
			Doors = XML.Element("VSwap")?.Element("Walls")?.Elements("Door")?.Select(e => ushort.Parse(e.Attribute("Number").Value))?.ToArray();
			Elevators = XML.Element("VSwap")?.Element("Walls")?.Elements("Elevator")?.Select(e => ushort.Parse(e.Attribute("Number").Value))?.ToArray();
			PushWalls = PushWall?.Select(e => ushort.Parse(e.Attribute("Number").Value))?.ToArray();
		}
		public ushort MapNumber(ushort episode, ushort floor) => ushort.Parse(XML.Element("Maps").Elements("Map").Where(map =>
				ushort.TryParse(map.Attribute("Episode")?.Value, out ushort e) && e == episode
				&& ushort.TryParse(map.Attribute("Floor")?.Value, out ushort f) && f == floor
			).First().Attribute("Number")?.Value);
		public XElement Elevator(ushort number) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Elevator")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort elevator) && elevator == number)?.FirstOrDefault();
		public XElement Wall(ushort number) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort wall) && wall == number)?.FirstOrDefault();
		public IEnumerable<XElement> PushWall => XML?.Element("VSwap")?.Element("Objects")?.Elements("Pushwall");
		public ushort WallPage(ushort cell) =>
			ushort.TryParse(Wall(cell)?.Attribute("Page")?.Value, out ushort result) ? result : throw new InvalidDataException("Could not find wall texture " + cell + "!");
		public bool IsNavigable(ushort mapData, ushort objectData) =>
			IsTransparent(mapData, objectData) && (
				!(XML?.Element("VSwap")?.Element("Objects").Elements("Billboard")
					.Where(e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == objectData).FirstOrDefault() is XElement mapObject)
				|| mapObject.IsTrue("Walk")
				);
		public bool IsTransparent(ushort mapData, ushort objectData) =>
			(!Walls.Contains(mapData) || PushWalls.Contains(objectData))
			&& !Elevators.Contains(mapData);
		public bool IsMappable(GameMap map, ushort x, ushort z) =>
			IsTransparent(map.GetMapData(x, z), map.GetObjectData(x, z))
			|| (x > 0 && IsTransparent(map.GetMapData((ushort)(x - 1), z), map.GetObjectData((ushort)(x - 1), z)))
			|| (x < map.Width - 1 && IsTransparent(map.GetMapData((ushort)(x + 1), z), map.GetObjectData((ushort)(x + 1), z)))
			|| (z > 0 && IsTransparent(map.GetMapData(x, (ushort)(z - 1)), map.GetObjectData(x, (ushort)(z - 1))))
			|| (z < map.Depth - 1 && IsTransparent(map.GetMapData(x, (ushort)(z + 1)), map.GetObjectData(x, (ushort)(z + 1))));
		/// <summary>
		/// "If you only knew the power of the Dark Side." - Darth Vader
		/// </summary>
		public ushort DarkSide(ushort cell) =>
			ushort.TryParse(XWall(cell).FirstOrDefault()?.Attribute("DarkSide")?.Value, out ushort result) ? result : WallPage(cell);
		public IEnumerable<XElement> XWall(ushort cell) =>
			XML?.Element("VSwap")?.Element("Walls")?.Elements()
			?.Where(e => (uint)e.Attribute("Number") == cell);
		public IEnumerable<XElement> XDoor(ushort cell) =>
			XML?.Element("VSwap")?.Element("Walls")?.Elements("Door")
			?.Where(e => (uint)e.Attribute("Number") == cell);
		public ushort DoorTexture(ushort cell) =>
			(ushort)(uint)XDoor(cell).FirstOrDefault()?.Attribute("Page");
		public MapAnalysis Analyze(GameMap map) => new MapAnalysis(this, map);
		public IEnumerable<MapAnalysis> Analyze(params GameMap[] maps) => maps.Select(map => new MapAnalysis(this, map));
		public struct MapAnalysis
		{
			#region XML Attributes
			public MapAnalyzer MapAnalyzer { get; private set; }
			public XElement XML { get; private set; }
			public GameMap Map { get; private set; }
			public byte Episode { get; private set; }
			public byte Floor { get; private set; }
			public byte ElevatorTo { get; private set; }
			public byte? Ground { get; private set; }
			public ushort? GroundTile { get; private set; }
			public byte? Ceiling { get; private set; }
			public ushort? CeilingTile { get; private set; }
			public byte Border { get; private set; }
			public TimeSpan Par { get; private set; }
			public string Song { get; private set; }
			#endregion XML Attributes
			#region Grids
			private readonly bool[][] Navigable;
			public bool IsNavigable(int x, int z) =>
				x >= 0 && z >= 0 && x < Navigable.Length && z < Navigable[x].Length
				&& Navigable[x][z];
			private readonly bool[][] Transparent;
			public bool IsTransparent(int x, int z) =>
				x >= 0 && z >= 0 && x < Transparent.Length && z < Transparent[x].Length
				&& Transparent[x][z];
			private readonly bool[][] Mappable;
			public bool IsMappable(int x, int z) =>
				x >= 0 && z >= 0 && x < Mappable.Length && z < Mappable[x].Length
				&& Mappable[x][z];
			#endregion Grids
			public MapAnalysis(MapAnalyzer mapAnalyzer, GameMap map)
			{
				MapAnalyzer = mapAnalyzer;
				Map = map;
				Navigable = new bool[Map.Width][];
				Transparent = new bool[Map.Width][];
				for (ushort x = 0; x < Map.Width; x++)
				{
					Navigable[x] = new bool[Map.Depth];
					Transparent[x] = new bool[Map.Depth];
					for (ushort z = 0; z < Map.Depth; z++)
					{
						Navigable[x][z] = MapAnalyzer.IsNavigable(Map.GetMapData(x, z), Map.GetObjectData(x, z));
						Transparent[x][z] = MapAnalyzer.IsTransparent(Map.GetMapData(x, z), Map.GetObjectData(x, z));
					}
				}
				Mappable = new bool[Map.Width][];
				for (ushort x = 0; x < Map.Width; x++)
				{
					Mappable[x] = new bool[Map.Depth];
					for (ushort z = 0; z < Map.Depth; z++)
						Mappable[x][z] = Transparent[x][z]
							|| (x > 0 && Transparent[x - 1][z])
							|| (x < Transparent.Length - 1 && Transparent[x + 1][z])
							|| (z > 0 && Transparent[x][z - 1])
							|| (z < Transparent[x].Length - 1 && Transparent[x][z + 1]);
				}
				XML = MapAnalyzer.XML.Element("Maps").Elements("Map").Where(m => ushort.TryParse(m.Attribute("Number")?.Value, out ushort mu) && mu == map.Number).FirstOrDefault() ?? throw new InvalidDataException("XML tag for map \"" + Map.Name + "\" was not found!");
				Episode = byte.TryParse(XML?.Attribute("Episode")?.Value, out byte episode) ? episode : (byte)0;
				Floor = byte.TryParse(XML?.Attribute("Floor")?.Value, out byte floor) ? floor : (byte)0;
				ElevatorTo = byte.TryParse(XML.Attribute("ElevatorTo")?.Value, out byte elevatorTo) ? elevatorTo : (byte)(Floor + 1);
				Ground = byte.TryParse(XML?.Attribute("Ground")?.Value, out byte ground) ? ground : (byte?)null;
				GroundTile = byte.TryParse(XML?.Attribute("GroundTile")?.Value, out byte groundTile) ? groundTile : (byte?)null;
				Ceiling = byte.TryParse(XML?.Attribute("Ceiling")?.Value, out byte ceiling) ? ceiling : (byte?)null;
				CeilingTile = byte.TryParse(XML?.Attribute("CeilingTile")?.Value, out byte ceilingTile) ? ceilingTile : (ushort?)null;
				Border = byte.TryParse(XML?.Attribute("Border")?.Value, out byte border) ? border : (byte)0;
				Par = TimeSpan.TryParse(XML?.Attribute("Par")?.Value, out TimeSpan par) ? par : TimeSpan.Zero;
				Song = XML.Attribute("Song")?.Value;
			}
		}
	}
}
