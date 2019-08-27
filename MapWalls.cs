using Godot;
using System.Collections.Generic;
using WOLF3DSim;

namespace WOLF3D
{
    public class MapWalls
    {
        public WOLF3DSim.GameMaps.Map Map { get; set; }
        public List<Sprite3D> Walls = new List<Sprite3D>();

        public MapWalls Load(GameMaps.Map map)
        {
            Map = map;
            for (uint i = 0; i < Map.MapData.Length; i++)
            {
                uint x = X(i), z = Z(i), here = Get(x, z);
                if (!IsWall(here))
                {
                    ushort wall;
                    if (z > 0 && IsWall(wall = Get(x, z - 1)))
                        Walls.Add(SouthWall(x, z - 1, (uint)(wall - 1) * 2 + 1));
                    if (z < 63 && IsWall(wall = Get(x, z + 1)))
                        Walls.Add(SouthWall(x, z, (uint)(wall - 1) * 2 + 1));
                    if (x < 63 && IsWall(wall = Get(x + 1, z)))
                        Walls.Add(WestWall(x + 1, z, (uint)(wall - 1) * 2));
                    if (x > 0 && IsWall(wall = Get(x - 1, z)))
                        Walls.Add(WestWall(x, z, (uint)(wall - 1) * 2));
                }
            }
            return this;
        }

        public bool IsWall(uint wall)
        {
            return wall < Game.Assets.VSwap.SpritePage;
        }

        public ushort Get(uint x, uint z)
        {
            return Map.MapData[(x * 64) + 63 - z];
        }

        public static ushort X(uint i)
        {
            return (ushort)(i % 64);
        }

        public static ushort Z(uint i)
        {
            return (ushort)(i / 64);
        }

        public Sprite3D SouthWall(uint x, uint z, uint texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture], Vector3.Axis.Z, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * z));
        }

        public Sprite3D WestWall(uint x, uint z, uint texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture], Vector3.Axis.X, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * z));
        }

        public Sprite3D BuildWall(Texture texture, Vector3.Axis axis, Vector3 position)
        {
            return new Sprite3D()
            {
                Texture = texture,
                PixelSize = Assets.PixelSize,
                MaterialOverride = Assets.WallMaterial,
                Axis = axis,
                Centered = false,
                Transform = new Transform(Basis.Identity, position)
            };
        }
    }
}
