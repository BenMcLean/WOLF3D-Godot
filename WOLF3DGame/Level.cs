using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Level : Spatial
    {
        public GameMap Map { get; private set; }
        public WorldEnvironment WorldEnvironment { get; private set; }
        public bool[][] Open { get; private set; }
        public MapWalls MapWalls { get; private set; }

        public bool CanWalk(Vector2 there) => CanWalk(Assets.IntCoordinate(there.x), Assets.IntCoordinate(there.y));

        public bool CanWalk(int x, int z) =>
            x >= 0 && z >= 0 && x < Map.Width && z < Map.Depth &&
            Open[x][z];

        public Level(GameMap map)
        {
            Map = map;
            AddChild(WorldEnvironment = new WorldEnvironment()
            {
                Environment = new Godot.Environment()
                {
                    BackgroundColor = Game.Assets.Palette[Map.Border],
                    BackgroundMode = Godot.Environment.BGMode.Color,
                },
            });
            AddChild(MapWalls = new MapWalls(Map));

            Billboard[] billboards = Billboard.MakeBillboards(Map);
            foreach (Billboard billboard in billboards)
                AddChild(billboard);

            Open = new bool[Map.Width][];
            for (ushort x = 0; x < Map.Width; x++)
            {
                Open[x] = new bool[Map.Depth];
                for (ushort z = 0; z < Map.Depth; z++)
                    Open[x][z] = !(IsWall(x, z) || !IsNavigable(x, z));
            }
        }

        public bool IsWall(ushort x, ushort z) => Game.Assets.Walls.Contains(Map.GetMapData(x, z));

        public bool IsNavigable(ushort x, ushort z) => IsNavigable(Map.GetObjectData(x, z));

        public static bool IsNavigable(uint cell)
        {
            XElement mapObject = (from e in Game.Assets?.XML?.Element("VSwap")?.Element("Objects").Elements("Billboard")
                                  where (uint)e.Attribute("Number") == cell
                                  select e).FirstOrDefault();
            return mapObject == null || Assets.IsTrue(mapObject, "Walk");
        }

        /// <returns>if the specified map coordinate is adjacent to a floor</returns>
        public bool IsByFloor(ushort x, ushort z)
        {
            ushort startX = x < 1 ? x : x > Map.Width - 1 ? (ushort)(Map.Width - 1) : (ushort)(x - 1),
                startZ = z < 1 ? z : z > Map.Depth - 1 ? (ushort)(Map.Depth - 1) : (ushort)(z - 1),
                endX = x >= Map.Width - 1 ? (ushort)(Map.Width - 1) : (ushort)(x + 1),
                endZ = z >= Map.Depth - 1 ? (ushort)(Map.Depth - 1) : (ushort)(z + 1);
            for (ushort dx = startX; dx <= endX; dx++)
                for (ushort dz = startZ; dz <= endZ; dz++)
                    if ((dx != x || dz != z) && !IsWall(dx, dz))
                        return true;
            return false;
        }

        public List<ushort> SquaresOccupied(Vector3 vector3) => SquaresOccupied(Assets.Vector2(vector3));

        public List<ushort> SquaresOccupied(Vector2 vector2)
        {
            List<ushort> list = new List<ushort>();
            void add(Vector2 here)
            {
                int x = Assets.IntCoordinate(here.x), z = Assets.IntCoordinate(here.y);
                if (x >= 0 && z >= 0 && x < Map.Depth && z < Map.Width)
                {
                    ushort square = Map.GetIndex((uint)x, (uint)z);
                    if (!list.Contains(square))
                        list.Add(square);
                }
            }
            add(vector2);
            foreach (Direction8 direction in Direction8.Diagonals)
                add(vector2 + direction.Vector2 * Assets.HeadDiagonal);
            return list;
        }

        public static ushort WallTexture(ushort cell) =>
            ushort.TryParse(XWall(cell).FirstOrDefault()?.Attribute("Page")?.Value, out ushort result) ? result : throw new InvalidDataException("Could not find wall texture " + cell + "!");

        /// <summary>
        /// If you only knew the power of the Dark Side
        /// </summary>
        public static ushort DarkSide(ushort cell) =>
            ushort.TryParse(XWall(cell).FirstOrDefault()?.Attribute("DarkSide")?.Value, out ushort result) ? result : WallTexture(cell);

        public static IEnumerable<XElement> XWall(ushort cell) =>
            from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements() ?? Enumerable.Empty<XElement>()
            where (uint)e.Attribute("Number") == cell
            select e;

        public static IEnumerable<XElement> XDoor(ushort cell) =>
            from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door") ?? Enumerable.Empty<XElement>()
            where (uint)e.Attribute("Number") == cell
            select e;

        public static ushort DoorTexture(ushort cell) =>
            (ushort)(uint)XDoor(cell).FirstOrDefault()?.Attribute("Page");

        public Transform StartTransform =>
            Start(out ushort index, out Direction8 direction) ?
            new Transform(direction.Basis, new Vector3(Assets.CenterSquare(Map.X(index)), 0f, Assets.CenterSquare(Map.Z(index))))
            : throw new InvalidDataException("Could not find start of level!");

        public bool Start(out ushort index, out Direction8 direction)
        {
            foreach (XElement start in Game.Assets?.XML?.Element("VSwap")?.Element("Objects")?.Elements("Start") ?? Enumerable.Empty<XElement>())
            {
                if (!ushort.TryParse(start.Attribute("Number")?.Value, out ushort find))
                    continue;
                int found = Array.FindIndex(Map.ObjectData, o => o == find);
                if (found > -1)
                {
                    index = (ushort)found;
                    direction = Direction8.From(start.Attribute("Direction"));
                    return true;
                }
            }
            index = 0;
            direction = null;
            return false;
        }
    }
}
