using Godot;
using System.Collections.Generic;

namespace WOLF3D
{
    public class MapWalls
    {
        public WOLF3DSim.GameMaps.Map Map { get; set; }
        public List<Sprite3D> Walls = new List<Sprite3D>();

        public MapWalls BuildCube(ushort x, ushort z, ushort texture = 0)
        {
            Walls.Add(NorthWall(x, z, texture));
            Walls.Add(SouthWall(x, z, texture));
            Walls.Add(EastWall(x, z, texture));
            Walls.Add(WestWall(x, z, texture));
            return this;
        }

        public Sprite3D NorthWall(ushort x, ushort z, ushort texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture], Vector3.Axis.Z, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * (z - 1)));
        }

        public Sprite3D SouthWall(ushort x, ushort z, ushort texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture], Vector3.Axis.Z, new Vector3(Assets.WallSize * x, 0, Assets.WallSize * z));
        }

        public Sprite3D EastWall(ushort x, ushort z, ushort texture = 0)
        {
            return BuildWall(Game.Assets.Textures[texture + 1], Vector3.Axis.X, new Vector3(Assets.WallSize * (x + 1), 0, Assets.WallSize * z));
        }

        public Sprite3D WestWall(ushort x, ushort z, ushort texture = 0)
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
