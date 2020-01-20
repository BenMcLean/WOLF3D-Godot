using Godot;
using System;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class MapWalls : StaticBody
    {
        public GameMap Map { get; set; }
        public MeshInstance Floor { get; private set; }
        public MeshInstance Ceiling { get; private set; }

        public MapWalls(GameMap map)
        {
            Map = map;
            AddChild(Floor = new MeshInstance()
            {
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Map.Width * Assets.WallWidth, Map.Depth * Assets.WallWidth),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Game.Assets.Palette[Map.Floor],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
                Transform = new Transform(
                    new Basis(Vector3.Right, Mathf.Pi / 2f).Orthonormalized(),
                    new Vector3(
                        Map.Width * Assets.HalfWallWidth,
                        0f,
                        Map.Depth * Assets.HalfWallWidth
                    )
                ),
            });

            AddChild(Ceiling = new MeshInstance()
            {
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Map.Width * Assets.WallWidth, Map.Depth * Assets.WallWidth),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Game.Assets.Palette[Map.Ceiling],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
                Transform = new Transform(
                    new Basis(Vector3.Right, Mathf.Pi / 2f).Orthonormalized(),
                    new Vector3(
                        Map.Width * Assets.HalfWallWidth,
                        (float)Assets.WallHeight,
                        Map.Depth * Assets.HalfWallWidth
                    )
                ),
            });

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
                if (x < map.Width - 1 && Level.IsWall(wall = Map.GetMapData(x + 1, z)))
                    AddChild(WestWall(x + 1, z, Level.WallTexture(wall), true));
                if (x > 0 && Level.IsWall(wall = Map.GetMapData(x - 1, z)))
                    AddChild(WestWall(x, z, Level.WallTexture(wall)));
            }
            void VerticalCheck(uint x, uint z)
            {
                uint wall;
                if (z > 0 && Level.IsWall(wall = Map.GetMapData(x, z - 1)))
                    AddChild(SouthWall(x, z - 1, Level.DarkSide(wall)));
                if (z < map.Depth - 1 && Level.IsWall(wall = Map.GetMapData(x, z + 1)))
                    AddChild(SouthWall(x, z, Level.DarkSide(wall), true));
            }
            for (uint i = 0; i < Map.MapData.Length; i++)
            {
                uint x = map.X(i), z = map.Z(i), here = Map.GetMapData(x, z);
                if (Level.IsDoor(here))
                {
                    if (here % 2 == 0) // Even numbered doors are vertical
                    {
                        AddChild(WestWall(x + 1, z, doorFrame, true));
                        AddChild(WestWall(x, z, doorFrame));
                        VerticalCheck(x, z);
                        //AddChild(HorizontalDoor(x, z, Level.DoorTexture(here)));
                    }
                    else // Odd numbered doors are horizontal
                    {
                        AddChild(SouthWall(x, z - 1, darkFrame));
                        AddChild(SouthWall(x, z, darkFrame, true));
                        HorizontalCheck(x, z);
                        //AddChild(VerticalDoor(x, z, Level.DoorTexture(here)));
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
            BuildWall(wall, false, (int)x, (int)z, flipH);
        public static Spatial WestWall(uint x, uint z, uint wall, bool flipH = false) =>
            BuildWall(wall, true, (int)x, (int)z, flipH);

        //public static Spatial HorizontalDoor(uint x, uint z, uint wall, bool flipH = false) =>
        //    BuildWall(wall, Vector3.Axis.Z, new Vector3(Assets.FloatCoordinate(x), 0, Assets.CenterSquare((int)z)), flipH);
        //public static Spatial VerticalDoor(uint x, uint z, uint wall, bool flipH = false) =>
        //    BuildWall(wall, Vector3.Axis.X, new Vector3(Assets.CenterSquare(x), 0, Assets.FloatCoordinate(z + 1)), flipH);

        public static MeshInstance BuildWall(uint wall, bool WesternWall, int x, int z, bool flipH = false) => BuildWall(wall, WesternWall ? Direction8.WEST : Direction8.SOUTH, x, z, flipH);

        /// <summary>
        /// "Of course Momma's gonna help build the wall." - Pink Floyd
        /// </summary>
        /// <param name="direction">Either SOUTH or WEST</param>
        public static MeshInstance BuildWall(uint wall, Direction8 direction, int x, int z, bool flipH = false) =>
            new MeshInstance()
            {
                MaterialOverride = Game.Assets.VSwapMaterials[wall],
                Mesh = Assets.Wall,
                Transform = new Transform(
                    direction == Direction8.WEST ?
                        flipH ? direction.Counter90.Basis : direction.Clock90.Basis
                    : flipH ? direction.Clock90.Basis : direction.Counter90.Basis,
                    new Vector3(
                            direction == Direction8.WEST ? Assets.FloatCoordinate(x) : Assets.CenterSquare(x),
                            (float)Assets.HalfWallHeight,
                            direction == Direction8.SOUTH ? Assets.FloatCoordinate(z + 1) : Assets.CenterSquare(z)
                        )
                    )
            };
    }
}
