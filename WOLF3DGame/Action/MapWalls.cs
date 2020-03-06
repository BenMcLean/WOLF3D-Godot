using Godot;
using System;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3DGame.Action
{
    /// <summary>
    /// This StaticBody contains the ceiling, floor and all the non-moving walls for a level.
    /// <para />
    /// The idea here is to "set it and forget it" since nothing in here ever changes during gameplay. Save games also don't have to get any information from any children of MapWalls, since MapWalls does not change no matter what the state of gameplay is.
    /// </summary>
    public class MapWalls : StaticBody
    {
        public GameMap Map { get; set; }
        public CollisionShape Floor { get; private set; }
        public MeshInstance FloorMesh { get; private set; }
        public CollisionShape Ceiling { get; private set; }
        public MeshInstance CeilingMesh { get; private set; }
        public float DiagonalLength { get; private set; }

        public MapWalls(GameMap map)
        {
            Map = map;
            AddChild(Floor = new CollisionShape()
            {
                Name = "Floor",
                Shape = new BoxShape()
                {
                    Extents = new Vector3(Map.Width * Assets.HalfWallWidth, Map.Depth * Assets.HalfWallWidth, Assets.PixelHeight)
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
            Floor.AddChild(FloorMesh = new MeshInstance()
            {
                Name = "Floor Mesh",
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Map.Width * Assets.WallWidth, Map.Depth * Assets.WallWidth),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Assets.Palette[Map.Floor],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
            });
            AddChild(Ceiling = new CollisionShape()
            {
                Name = "Ceiling",
                Shape = new BoxShape()
                {
                    Extents = new Vector3(Map.Width * Assets.HalfWallWidth, Map.Depth * Assets.HalfWallWidth, Assets.PixelHeight)
                },
                Transform = new Transform(
                    new Basis(Vector3.Right, Mathf.Pi / 2f).Orthonormalized(),
                    new Vector3(
                        Map.Width * Assets.HalfWallWidth,
                        Assets.WallHeight,
                        Map.Depth * Assets.HalfWallWidth
                    )
                ),
            });
            Ceiling.AddChild(CeilingMesh = new MeshInstance()
            {
                Name = "Ceiling Mesh",
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Map.Width * Assets.WallWidth, Map.Depth * Assets.WallWidth),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Assets.Palette[Map.Ceiling],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
            });

            XElement doorFrameX = (from e in Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall") ?? Enumerable.Empty<XElement>()
                                   where e.Attribute("Name").Value.Equals("Door Frame")
                                   select e).FirstOrDefault();
            if (doorFrameX == null)
                throw new NullReferenceException("Could not find \"Door Frame\" in walls!");
            ushort doorFrame = (ushort)(uint)doorFrameX.Attribute("Page"),
                darkFrame = (ushort)(uint)doorFrameX.Attribute("DarkSide");
            void HorizontalCheck(ushort x, ushort z)
            {
                ushort wall;
                if (x < map.Width - 1 && Assets.Walls.Contains(wall = Map.GetMapData((ushort)(x + 1), z)))
                    AddChild(BuildWall(Level.WallTexture(wall), false, x + 1, z, true));
                if (x > 0 && Assets.Walls.Contains(wall = Map.GetMapData((ushort)(x - 1), z)))
                    AddChild(BuildWall(Level.WallTexture(wall), false, x, z));
            }
            void VerticalCheck(ushort x, ushort z)
            {
                ushort wall;
                if (z > 0 && Assets.Walls.Contains(wall = Map.GetMapData(x, (ushort)(z - 1))))
                    AddChild(BuildWall(Level.DarkSide(wall), true, x, z - 1));
                if (z < map.Depth - 1 && Assets.Walls.Contains(wall = Map.GetMapData(x, (ushort)(z + 1))))
                    AddChild(BuildWall(Level.DarkSide(wall), true, x, z, true));
            }
            for (ushort i = 0; i < Map.MapData.Length; i++)
            {
                ushort x = map.X(i), z = map.Z(i), here = Map.GetMapData(x, z);
                if (Assets.Doors.Contains(here))
                {
                    if (here % 2 == 0) // Even numbered doors are vertical
                    {
                        AddChild(BuildWall(doorFrame, false, x + 1, z, true));
                        AddChild(BuildWall(doorFrame, false, x, z));
                        VerticalCheck(x, z);
                        //AddChild(HorizontalDoor(x, z, Level.DoorTexture(here)));
                    }
                    else // Odd numbered doors are horizontal
                    {
                        AddChild(BuildWall(darkFrame, true, x, z - 1));
                        AddChild(BuildWall(darkFrame, true, x, z, true));
                        HorizontalCheck(x, z);
                        //AddChild(VerticalDoor(x, z, Level.DoorTexture(here)));
                    }
                }
                else if (!Assets.Walls.Contains(here))
                {
                    HorizontalCheck(x, z);
                    VerticalCheck(x, z);
                }
            }

            DiagonalLength = Mathf.Sqrt(Mathf.Pow(Map.Width * Assets.WallWidth, 2) + Mathf.Pow((float)Assets.WallHeight, 2) + Mathf.Pow(Map.Depth * Assets.WallWidth, 2));
        }

        //public static Spatial HorizontalDoor(uint x, uint z, uint wall, bool flipH = false) =>
        //    BuildWall(wall, Vector3.Axis.Z, new Vector3(Assets.FloatCoordinate(x), 0, Assets.CenterSquare((int)z)), flipH);
        //public static Spatial VerticalDoor(uint x, uint z, uint wall, bool flipH = false) =>
        //    BuildWall(wall, Vector3.Axis.X, new Vector3(Assets.CenterSquare(x), 0, Assets.FloatCoordinate(z + 1)), flipH);

        /// <summary>
        /// "Of course Momma's gonna help build the wall." - Pink Floyd
        /// </summary>
        public static CollisionShape BuildWall(ushort wall, bool westernWall, int x, int z, bool flipH = false)
        {
            CollisionShape result = new CollisionShape()
            {
                Name = (westernWall ? "West" : "South") + " wall shape at [" + x + ", " + z + "]",
                Transform = new Transform(
                    westernWall ?
                        flipH ? Direction8.SOUTH.Basis : Direction8.NORTH.Basis
                        : flipH ? Direction8.WEST.Basis : Direction8.EAST.Basis,
                    new Vector3(
                            westernWall ? Assets.CenterSquare(x) : Assets.FloatCoordinate(x),
                            (float)Assets.HalfWallHeight,
                            westernWall ? Assets.FloatCoordinate(z + 1) : Assets.CenterSquare(z)
                        )
                    ),
                Shape = Assets.WallShape,
            };
            result.AddChild(new MeshInstance()
            {
                Name = (westernWall ? "West" : "South") + " wall mesh instance at [" + x + ", " + z + "]",
                MaterialOverride = Assets.VSwapMaterials[wall],
                Mesh = Assets.WallMesh,
            });
            return result;
        }
    }
}
