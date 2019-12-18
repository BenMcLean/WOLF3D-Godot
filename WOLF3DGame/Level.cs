using Godot;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Level : Spatial
    {
        public GameMap Map { get; private set; }
        public WorldEnvironment WorldEnvironment { get; private set; }
        public MeshInstance Floor { get; private set; }
        public MeshInstance Ceiling { get; private set; }

        public Level(GameMap map)
        {
            AddChild(WorldEnvironment = new WorldEnvironment()
            {
                Environment = new Godot.Environment()
                {
                    BackgroundColor = Game.Assets.Palette[map.Border],
                    BackgroundMode = Godot.Environment.BGMode.Color,
                },
            });

            AddChild(Floor = new MeshInstance()
            {
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(map.Width * Assets.WallWidth, map.Depth * Assets.WallWidth),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Game.Assets.Palette[map.Floor],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
                Transform = new Transform(
                    Basis.Identity.Rotated(Vector3.Right, Mathf.Pi / 2f),
                    new Vector3(
                        map.Width * Assets.HalfWallWidth,
                        0f,
                        map.Depth * Assets.HalfWallWidth
                    )
                ),
            });

            AddChild(Ceiling = new MeshInstance()
            {
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(map.Width * Assets.WallWidth, map.Depth * Assets.WallWidth),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoColor = Game.Assets.Palette[map.Ceiling],
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    FlagsTransparent = false,
                    ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                },
                Transform = new Transform(
                    Basis.Identity.Rotated(Vector3.Right, Mathf.Pi / 2f),
                    new Vector3(
                        map.Width * Assets.HalfWallWidth,
                        (float)Assets.WallHeight,
                        map.Depth * Assets.HalfWallWidth
                    )
                ),
            });

            MapWalls mapWalls = new MapWalls(map);
            foreach (Spatial sprite in mapWalls.Walls)
                AddChild(sprite);

            Billboard[] billboards = Billboard.MakeBillboards(map);
            foreach (Billboard billboard in billboards)
                AddChild(billboard);
        }
    }
}
