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
                uint x = X(i), z = Z(i), wall=Get(x, z);
                if (IsWall(wall))
                {
                    if (z > 0 && !IsWall(Get(x, z - 1)))
                        Walls.Add(NorthWall(x, z, wall));
                    if (z < 63 && !IsWall(Get(x, z + 1)))
                        Walls.Add(SouthWall(x, z, wall));
                    if (x < 63 && !IsWall(Get(x + 1, z)))
                        Walls.Add(EastWall(x, z, wall));
                    if (x > 0 && !IsWall(Get(x - 1, z)))
                        Walls.Add(EastWall(x, z, wall));
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
            return Map.MapData[(x * 64) + z];
        }

        public static ushort X(uint i)
        {
            return (ushort)(i % 64);
        }

        public static ushort Z(uint i)
        {
            return (ushort)(i / 64);
        }

        public MapWalls BuildCube(uint x, uint z, uint texture = 0)
        {
            Walls.Add(NorthWall(x, z, texture));
            Walls.Add(SouthWall(x, z, texture));
            Walls.Add(EastWall(x, z, texture));
            Walls.Add(WestWall(x, z, texture));
            return this;
        }

        public Sprite3D NorthWall(uint x, uint z, uint texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture], Vector3.Axis.Z, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * (z - 1)));
        }

        public Sprite3D SouthWall(uint x, uint z, uint texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture], Vector3.Axis.Z, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * z));
        }

        public Sprite3D EastWall(uint x, uint z, uint texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture + 1], Vector3.Axis.X, new Vector3(Assets.WallSize * (x + 1), 0, Assets.WallSize * z));
        }

        public Sprite3D WestWall(uint x, uint z, uint texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture + 1], Vector3.Axis.X, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * z));
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
