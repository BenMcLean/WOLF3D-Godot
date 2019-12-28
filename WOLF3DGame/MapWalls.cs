using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class MapWalls
    {
        public GameMap Map { get; set; }
        public List<Spatial> Walls = new List<Spatial>();

        public MapWalls(GameMap map)
        {
            XElement doorFrameX = (from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall") ?? Enumerable.Empty<XElement>()
                                   where e.Attribute("Name").Value.Equals("Door Frame")
                                   select e).FirstOrDefault();
            if (doorFrameX == null)
                throw new NullReferenceException("Could not find \"Door Frame\" in walls!");
            uint doorFrame = (uint)doorFrameX.Attribute("Page"),
                darkFrame = (uint)doorFrameX.Attribute("DarkSide");
            void HorizontalCheck(uint x, uint z)
            {
                uint wall;
                if (x < map.Width - 1 && Level.IsWall(wall = Map.Get(x + 1, z)))
                    Walls.Add(WestWall(x + 1, z, Level.WallTexture(wall), true));
                if (x > 0 && Level.IsWall(wall = Map.Get(x - 1, z)))
                    Walls.Add(WestWall(x, z, Level.WallTexture(wall)));
            }
            void VerticalCheck(uint x, uint z)
            {
                uint wall;
                if (z > 0 && Level.IsWall(wall = Map.Get(x, z - 1)))
                    Walls.Add(SouthWall(x, z - 1, Level.DarkSide(wall)));
                if (z < map.Depth - 1 && Level.IsWall(wall = Map.Get(x, z + 1)))
                    Walls.Add(SouthWall(x, z, Level.DarkSide(wall), true));
            }
            Map = map;
            for (uint i = 0; i < Map.MapData.Length; i++)
            {
                uint x = map.X(i), z = map.Z(i), here = Map.Get(x, z);
                if (Level.IsDoor(here))
                {
                    if (here % 2 == 0) // Even numbered doors are vertical
                    {
                        Walls.Add(WestWall(x + 1, z, doorFrame, true));
                        Walls.Add(WestWall(x, z, doorFrame));
                        VerticalCheck(x, z);
                        Walls.Add(HorizontalDoor(x, z, Level.DoorTexture(here)));
                    }
                    else // Odd numbered doors are horizontal
                    {
                        Walls.Add(SouthWall(x, z - 1, darkFrame));
                        Walls.Add(SouthWall(x, z, darkFrame, true));
                        HorizontalCheck(x, z);
                        Walls.Add(VerticalDoor(x, z, Level.DoorTexture(here)));
                    }
                }
                else if (!Level.IsWall(here))
                {
                    HorizontalCheck(x, z);
                    VerticalCheck(x, z);
                }
            }
        }

        public static Spatial SouthWall(uint x, uint z, uint wall, bool flipH = false) =>
            BuildWall(wall, Vector3.Axis.Z, new Vector3(Assets.WallWidth * x, 0, Assets.WallWidth * z), flipH);

        public static Spatial WestWall(uint x, uint z, uint wall, bool flipH = false) =>
            BuildWall(wall, Vector3.Axis.X, new Vector3(Assets.WallWidth * x, 0, Assets.WallWidth * z), flipH);

        public static Spatial HorizontalDoor(uint x, uint z, uint wall, bool flipH = false) =>
            BuildWall(wall, Vector3.Axis.Z, new Vector3(Assets.WallWidth * x, 0, Assets.WallWidth * (z - 0.5f)), flipH);

        public static Spatial VerticalDoor(uint x, uint z, uint wall, bool flipH = false) =>
            BuildWall(wall, Vector3.Axis.X, new Vector3(Assets.WallWidth * (x + 0.5f), 0, Assets.WallWidth * z), flipH);

        /// <summary>
        /// "Of course Momma's gonna help build the wall." - Pink Floyd
        /// </summary>
        public static Spatial BuildWall(uint wall, Vector3.Axis axis, Vector3 position, bool flipH = false)
        {
            Spatial spatial = new Spatial()
            {
                GlobalTransform = new Transform(Basis.Identity, position),
                Rotation = Assets.Axis(axis),
            };
            spatial.AddChild(new MeshInstance()
            {
                MaterialOverride = Game.Assets.VSwapMaterials[wall],
                Mesh = Assets.Wall,
                Transform = flipH ? Assets.WallTransformFlipped : Assets.WallTransform,
            });
            return spatial;
        }
    }
}
