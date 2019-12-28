using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Level : Spatial
    {
        public GameMap Map { get; private set; }
        public WorldEnvironment WorldEnvironment { get; private set; }
        public MeshInstance Floor { get; private set; }
        public MeshInstance Ceiling { get; private set; }
        public Area Area { get; private set; }

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

            Area = new Area()
            {

            };

            CollisionShape CollisionShape = new CollisionShape()
            {
                Shape = Assets.BoxShape,
                Disabled = false,
            };
        }

        public static bool IsWall(uint cell) => XWall(cell).Any();

        public static uint WallTexture(uint cell) =>
            (uint)XWall(cell).FirstOrDefault()?.Attribute("Page");

        /// <summary>
        /// Never underestimate the power of the Dark Side
        /// </summary>
        public static uint DarkSide(uint cell) =>
            (uint)XWall(cell).FirstOrDefault()?.Attribute("DarkSide");

        public static IEnumerable<XElement> XWall(uint cell) =>
            from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall") ?? Enumerable.Empty<XElement>()
            where (uint)e.Attribute("Number") == cell
            select e;

        public static IEnumerable<XElement> XDoor(uint cell) =>
            from e in Game.Assets?.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door") ?? Enumerable.Empty<XElement>()
            where (uint)e.Attribute("Number") == cell
            select e;

        public static uint DoorTexture(uint cell) =>
            (uint)XDoor(cell).FirstOrDefault()?.Attribute("Page");

        public static bool IsDoor(uint cell) =>
            XDoor(cell).Any();
    }
}
