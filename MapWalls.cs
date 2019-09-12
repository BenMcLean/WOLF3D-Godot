using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D
{
    public class MapWalls
    {
        public GameMaps.Map Map { get; set; }
        public List<Sprite3D> Walls = new List<Sprite3D>();

        public MapWalls(GameMaps.Map map)
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
                if (x < 63 && IsWall(wall = Get(x + 1, z)))
                    Walls.Add(WestWall(x + 1, z, Game.Assets.Textures[WallTexture(wall)], true));
                if (x > 0 && IsWall(wall = Get(x - 1, z)))
                    Walls.Add(WestWall(x, z, Game.Assets.Textures[WallTexture(wall)]));
            }
            void VerticalCheck(uint x, uint z)
            {
                uint wall;
                if (z > 0 && IsWall(wall = Get(x, z - 1)))
                    Walls.Add(SouthWall(x, z - 1, Game.Assets.Textures[DarkSide(wall)]));
                if (z < 63 && IsWall(wall = Get(x, z + 1)))
                    Walls.Add(SouthWall(x, z, Game.Assets.Textures[DarkSide(wall)], true));
            }
            Map = map;
            for (uint i = 0; i < Map.MapData.Length; i++)
            {
                uint x = map.X(i), z = map.Z(i), here = Get(x, z);
                if (IsDoor(here))
                {
                    if (here % 2 == 0) // Even numbered doors are vertical
                    {
                        Walls.Add(WestWall(x + 1, z, Game.Assets.Textures[doorFrame], true));
                        Walls.Add(WestWall(x, z, Game.Assets.Textures[doorFrame]));
                        HorizontalCheck(x, z);
                        Walls.Add(HorizontalDoor(x, z, Game.Assets.Textures[DoorTexture(here)]));
                    }
                    else // Odd numbered doors are horizontal
                    {
                        Walls.Add(SouthWall(x, z - 1, Game.Assets.Textures[darkFrame]));
                        Walls.Add(SouthWall(x, z, Game.Assets.Textures[darkFrame], true));
                        VerticalCheck(x, z);
                        Walls.Add(VerticalDoor(x, z, Game.Assets.Textures[DoorTexture(here)]));
                    }
                }
                else if (!IsWall(here))
                {
                    HorizontalCheck(x, z);
                    VerticalCheck(x, z);
                }
            }
        }

        public bool IsWall(uint cell)
        {
            //return (cell - 1) * 2 < Game.Assets.VSwap.SpritePage;
            return XWall(cell).Any();
        }

        public uint WallTexture(uint cell)
        {
            return (uint)XWall(cell).FirstOrDefault().Attribute("Page");
        }

        /// <summary>
        /// Never underestimate the power of the Dark Side
        /// </summary>
        public uint DarkSide(uint cell)
        {
            return (uint)XWall(cell).FirstOrDefault().Attribute("DarkSide");
        }

        public static IEnumerable<XElement> XWall(uint cell)
        {
            return from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall") ?? Enumerable.Empty<XElement>()
                   where (uint)e.Attribute("Number") == cell
                   select e;
        }

        public static IEnumerable<XElement> XDoor(uint cell)
        {
            return from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door") ?? Enumerable.Empty<XElement>()
                   where (uint)e.Attribute("Number") == cell
                   select e;
        }

        public static uint DoorTexture(uint cell)
        {
            return (uint)XDoor(cell).FirstOrDefault().Attribute("Page");
        }

        public static bool IsDoor(uint cell)
        {
            return XDoor(cell).Any();
        }

        public ushort Get(uint x, uint z)
        {
            return Map.MapData[(x * Map.Width) + Map.Depth - 1 - z];
        }

        public Sprite3D SouthWall(uint x, uint z, Texture texture, bool flipH = false)
        {
            return BuildWall(texture, Vector3.Axis.Z, new Vector3(Assets.WallWidth * x, 0, Assets.WallWidth * z), flipH);
        }

        public Sprite3D WestWall(uint x, uint z, Texture texture, bool flipH = false)
        {
            return BuildWall(texture, Vector3.Axis.X, new Vector3(Assets.WallWidth * x, 0, Assets.WallWidth * z), flipH);
        }

        public Sprite3D HorizontalDoor(uint x, uint z, Texture texture, bool flipH = false)
        {
            return BuildWall(texture, Vector3.Axis.Z, new Vector3(Assets.WallWidth * x, 0, Assets.WallWidth * (z - 0.5f)), flipH);
        }

        public Sprite3D VerticalDoor(uint x, uint z, Texture texture, bool flipH = false)
        {
            return BuildWall(texture, Vector3.Axis.X, new Vector3(Assets.WallWidth * (x + 0.5f), 0, Assets.WallWidth * z), flipH);
        }

        public Sprite3D BuildWall(Texture texture, Vector3.Axis axis, Vector3 position, bool flipH = false)
        {
            return new Sprite3D()
            {
                Texture = texture,
                PixelSize = Assets.PixelWidth,
                Scale = Assets.Scale,
                MaterialOverride = WallMaterial,
                Axis = axis,
                Centered = false,
                GlobalTransform = new Transform(Basis.Identity, position),
                FlipH = flipH
            };
        }

        public static readonly SpatialMaterial WallMaterial = new SpatialMaterial()
        {
            FlagsUnshaded = true,
            FlagsDoNotReceiveShadows = true,
            FlagsDisableAmbientLight = true,
            ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            ParamsCullMode = SpatialMaterial.CullMode.Disabled,
        };
    }
}
