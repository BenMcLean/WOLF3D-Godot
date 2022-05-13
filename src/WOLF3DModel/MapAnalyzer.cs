using System.Collections.Generic;
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
		public XElement Elevator(ushort number) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Elevator")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort elevator) && elevator == number)?.FirstOrDefault();
		public XElement Wall(ushort number) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort wall) && wall == number)?.FirstOrDefault();
		public IEnumerable<XElement> PushWall => XML?.Element("VSwap")?.Element("Objects")?.Elements("Pushwall");
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
		public MapAnalysis Analyze(GameMap map) => new MapAnalysis(this, map);
		public struct MapAnalysis
		{
			public MapAnalysis(MapAnalyzer mapAnalyzer, GameMap map)
			{
				Navigable = new bool[map.Width][];
				Transparent = new bool[map.Width][];
				for (ushort x = 0; x < map.Width; x++)
				{
					Navigable[x] = new bool[map.Depth];
					Transparent[x] = new bool[map.Depth];
					for (ushort z = 0; z < map.Depth; z++)
					{
						Navigable[x][z] = mapAnalyzer.IsNavigable(map.GetMapData(x, z), map.GetObjectData(x, z));
						Transparent[x][z] = mapAnalyzer.IsTransparent(map.GetMapData(x, z), map.GetObjectData(x, z));
					}
				}
				Mappable = new bool[map.Width][];
				for (ushort x = 0; x < map.Width; x++)
				{
					Mappable[x] = new bool[map.Depth];
					for (ushort z = 0; z < map.Depth; z++)
						Mappable[x][z] = Transparent[x][z]
							|| (x > 0 && Transparent[x - 1][z])
							|| (x < Transparent.Length - 1 && Transparent[x + 1][z])
							|| (z > 0 && Transparent[x][z - 1])
							|| (z < Transparent[x].Length - 1 && Transparent[x][z + 1]);
				}
			}
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
		}
	}
}
