using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
    /// <summary>
    /// This StaticBody contains the ceiling, floor and all the non-moving walls for a level.
    /// <para />
    /// The idea here is to "set it and forget it" since nothing in here ever changes during gameplay. Save games also don't have to get any information from any children of this, since this does not change no matter what the state of gameplay is.
    /// </summary>
    public class Walls : StaticBody
    {
        public GameMap Map { get; set; }
        protected readonly bool[][] Navigable;
        public bool IsNavigable(int x, int z) =>
            x >= 0 && z >= 0 && x < Navigable.Length && z < Navigable[x].Length
            && Navigable[x][z];
        protected readonly bool[][] Transparent;
        public bool IsTransparent(int x, int z) =>
            x >= 0 && z >= 0 && x < Transparent.Length && z < Transparent[x].Length
            && Transparent[x][z];

        public CollisionShape Ground { get; private set; }
        public MeshInstance GroundMesh { get; private set; }
        public CollisionShape Ceiling { get; private set; }
        public MeshInstance CeilingMesh { get; private set; }
        public List<Elevator> Elevators = new List<Elevator>();

        public Walls(GameMap map)
        {
            Name = "Walls for map \"" + map.Name + "\"";
            Map = map;

            Navigable = new bool[Map.Width][];
            Transparent = new bool[Map.Width][];
            for (ushort x = 0; x < Map.Width; x++)
            {
                Navigable[x] = new bool[Map.Depth];
                Transparent[x] = new bool[Map.Depth];
                for (ushort z = 0; z < Map.Depth; z++)
                {
                    Navigable[x][z] = Assets.IsNavigable(Map.GetMapData(x, z), Map.GetObjectData(x, z));
                    Transparent[x][z] = Assets.IsTransparent(Map.GetMapData(x, z), Map.GetObjectData(x, z));
                }
            }

            // realWalls replaces pushwalls with floors.
            ushort[] realWalls = new ushort[map.MapData.Length];
            Array.Copy(map.MapData, realWalls, realWalls.Length);
            for (uint i = 0; i < realWalls.Length; i++)
                if (Assets.PushWalls.Contains(Map.ObjectData[i]))
                    realWalls[i] = Assets.FloorCodeFirst;
            ushort GetMapData(ushort x, ushort z) => realWalls[Map.GetIndex(x, z)];

            AddChild(Ground = new CollisionShape()
            {
                Name = "Ground",
                Shape = new BoxShape()
                {
                    Extents = new Vector3(Map.Width * Assets.HalfWallWidth, Map.Depth * Assets.HalfWallWidth, Assets.PixelHeight)
                },
                Transform = new Transform(
                    new Basis(Vector3.Right, Mathf.Pi / 2f).Rotated(Vector3.Up, Mathf.Pi / 2f).Orthonormalized(),
                    new Vector3(
                        Map.Width * Assets.HalfWallWidth,
                        Assets.PixelHeight / -2f,
                        Map.Depth * Assets.HalfWallWidth
                    )
                ),
            });
            Ground.AddChild(GroundMesh = new MeshInstance()
            {
                Name = "Ground Mesh",
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Map.Width * Assets.WallWidth, Map.Depth * Assets.WallWidth),
                },
                MaterialOverride = Map.GroundTile is ushort groundTile && groundTile < Assets.VSwapTextures.Length ?
                new SpatialMaterial()
                {
                    AlbedoTexture = Assets.VSwapTextures[groundTile],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    AnisotropyEnabled = true,
                    RenderPriority = 1,
                    Uv1Scale = new Vector3(Map.Width, Map.Depth, 0f),
                }
                : new SpatialMaterial()
                {
                    AlbedoColor = Assets.Palettes[0][(int)Map.Ground],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    AnisotropyEnabled = true,
                    RenderPriority = 1,
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
                    new Basis(Vector3.Right, Mathf.Pi / 2f).Rotated(Vector3.Up, Mathf.Pi / 2f).Orthonormalized(),
                    new Vector3(
                        Map.Width * Assets.HalfWallWidth,
                        Assets.WallHeight + Assets.PixelHeight / 2f,
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
                MaterialOverride = Map.CeilingTile is ushort ceilingTile && ceilingTile < Assets.VSwapTextures.Length ?
                new SpatialMaterial()
                {
                    AlbedoTexture = Assets.VSwapTextures[ceilingTile],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    AnisotropyEnabled = true,
                    RenderPriority = 1,
                    Uv1Scale = new Vector3(Map.Width, Map.Depth, 0f),
                }
                : new SpatialMaterial()
                {
                    AlbedoColor = Assets.Palettes[0][(int)Map.Ceiling],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    AnisotropyEnabled = true,
                    RenderPriority = 1,
                },
            });

            XElement doorFrameX = Assets.XML?.Element("VSwap")?.Element("Walls")?.Element("DoorFrame");
            if (doorFrameX == null)
                throw new NullReferenceException("Could not find \"DoorFrame\" tag in walls!");
            ushort doorFrame = (ushort)(uint)doorFrameX.Attribute("Page"),
                darkFrame = (ushort)(uint)doorFrameX.Attribute("DarkSide");
            void HorizontalCheck(ushort x, ushort z)
            {
                ushort wall;
                if (x < map.Width - 1 && Assets.Walls.Contains(wall = GetMapData((ushort)(x + 1), z)))
                    AddChild(BuildWall(Level.WallTexture(wall), false, x + 1, z, true));
                if (x > 0 && Assets.Walls.Contains(wall = GetMapData((ushort)(x - 1), z)))
                    AddChild(BuildWall(Level.WallTexture(wall), false, x, z));
            }
            void VerticalCheck(ushort x, ushort z)
            {
                ushort wall;
                if (z > 0 && Assets.Walls.Contains(wall = GetMapData(x, (ushort)(z - 1))))
                    AddChild(BuildWall(Level.DarkSide(wall), true, x, z - 1));
                if (z < map.Depth - 1 && Assets.Walls.Contains(wall = GetMapData(x, (ushort)(z + 1))))
                    AddChild(BuildWall(Level.DarkSide(wall), true, x, z, true));
            }
            for (ushort i = 0; i < Map.MapData.Length; i++)
            {
                ushort x = map.X(i), z = map.Z(i), here = GetMapData(x, z);
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
                else if (Assets.Elevators.Contains(here))
                {
                    Elevator elevator = new Elevator(Assets.Elevator(here))
                    {
                        X = x,
                        Z = z,
                        Transform = new Transform(Basis.Identity, new Vector3(Assets.FloatCoordinate(x), 0, Assets.FloatCoordinate(z))),
                    };
                    Elevators.Add(elevator);
                    AddChild(elevator);
                }
                else if (!Assets.Walls.Contains(here))
                {
                    HorizontalCheck(x, z);
                    VerticalCheck(x, z);
                }
            }
        }

        /// <summary>
        /// "Of course Momma's gonna help build the wall." - Pink Floyd
        /// </summary>
        public static CollisionShape BuildWall(ushort wall, bool westernWall, int x, int z, bool flipH = false)
        {
            CollisionShape result = new CollisionShape()
            {
                Name = (westernWall ? "West" : "South") + " wall shape at [" + x + ", " + z + "]: " + Assets.WallName(wall),
                Transform = new Transform(
                    westernWall ?
                        flipH ? Direction8.SOUTH.Basis : Direction8.NORTH.Basis
                        : flipH ? Direction8.WEST.Basis : Direction8.EAST.Basis,
                    new Vector3(
                            westernWall ? Assets.CenterSquare(x) : Assets.FloatCoordinate(x),
                            Assets.HalfWallHeight,
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
