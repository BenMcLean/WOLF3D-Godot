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
                Name = "Floor",
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
                Name = "Ceiling",
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
                    AddChild(BuildWall(Level.WallTexture(wall), true, (int)x + 1, (int)z, true));
                if (x > 0 && Level.IsWall(wall = Map.GetMapData(x - 1, z)))
                    AddChild(BuildWall(Level.WallTexture(wall), true, (int)x, (int)z));
            }
            void VerticalCheck(uint x, uint z)
            {
                uint wall;
                if (z > 0 && Level.IsWall(wall = Map.GetMapData(x, z - 1)))
                    AddChild(BuildWall(Level.DarkSide(wall), false, (int)x, (int)z - 1));
                if (z < map.Depth - 1 && Level.IsWall(wall = Map.GetMapData(x, z + 1)))
                    AddChild(BuildWall(Level.DarkSide(wall), false, (int)x, (int)z, true));
            }
            for (uint i = 0; i < Map.MapData.Length; i++)
            {
                uint x = map.X(i), z = map.Z(i), here = Map.GetMapData(x, z);
                if (Level.IsDoor(here))
                {
                    if (here % 2 == 0) // Even numbered doors are vertical
                    {
                        AddChild(BuildWall(doorFrame, true, (int)x + 1, (int)z, true));
                        AddChild(BuildWall(doorFrame, true, (int)x, (int)z));
                        VerticalCheck(x, z);
                        //AddChild(HorizontalDoor(x, z, Level.DoorTexture(here)));
                    }
                    else // Odd numbered doors are horizontal
                    {
                        AddChild(BuildWall(darkFrame, false, (int)x, (int)z - 1));
                        AddChild(BuildWall(darkFrame, false, (int)x, (int)z, true));
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

        //public static Spatial HorizontalDoor(uint x, uint z, uint wall, bool flipH = false) =>
        //    BuildWall(wall, Vector3.Axis.Z, new Vector3(Assets.FloatCoordinate(x), 0, Assets.CenterSquare((int)z)), flipH);
        //public static Spatial VerticalDoor(uint x, uint z, uint wall, bool flipH = false) =>
        //    BuildWall(wall, Vector3.Axis.X, new Vector3(Assets.CenterSquare(x), 0, Assets.FloatCoordinate(z + 1)), flipH);

        /// <summary>
        /// "Of course Momma's gonna help build the wall." - Pink Floyd
        /// </summary>
        public static MeshInstance BuildWall(uint wall, bool WesternWall, int x, int z, bool flipH = false) =>
            new MeshInstance()
            {
                Name = (WesternWall ? "West" : "South") + " wall at [" + x + ", " + z + "]",
                MaterialOverride = Game.Assets.VSwapMaterials[wall],
                Mesh = Assets.Wall,
                Transform = new Transform(
                    WesternWall ?
                        flipH ? Direction8.SOUTH.Basis : Direction8.NORTH.Basis
                        : flipH ? Direction8.WEST.Basis : Direction8.EAST.Basis,
                    new Vector3(
                            WesternWall ? Assets.FloatCoordinate(x) : Assets.CenterSquare(x),
                            (float)Assets.HalfWallHeight,
                            WesternWall ? Assets.CenterSquare(z) : Assets.FloatCoordinate(z + 1)
                        )
                )
            };
    }
}
