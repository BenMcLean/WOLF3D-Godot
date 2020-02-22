using Godot;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class MenuScreen : StaticBody
    {
        public const uint ScreenWidth = 320;
        public const uint ScreenHeight = 200;
        public const float Width = Assets.WallWidth;
        public const float Height = Width / 3f * 4f;
        public static readonly BoxShape MenuScreenShape = new BoxShape()
        {
            Extents = new Vector3(Width / 2f, Height / 2f, Assets.PixelWidth / 2f),
        };
        public WorldEnvironment WorldEnvironment { get; private set; }
        public Viewport Viewport { get; private set; }
        public ColorRect Background { get; private set; }
        public CollisionShape Shape { get; private set; }
        public MeshInstance MeshInstance { get; private set; }

        public Color Color
        {
            get => WorldEnvironment.Environment.BackgroundColor;
            set => WorldEnvironment.Environment.BackgroundColor = Background.Color = value;
        }

        public MenuScreen()
        {
            AddChild(WorldEnvironment = new WorldEnvironment()
            {
                Environment = new Godot.Environment()
                {
                    BackgroundColor = Color.Color8(0, 0, 0, 255),
                    BackgroundMode = Godot.Environment.BGMode.Color,
                },
            });
            AddChild(Viewport = new Viewport()
            {
                Size = new Vector2(ScreenWidth, ScreenHeight),
                Disable3d = true,
                RenderTargetClearMode = Viewport.ClearMode.OnlyNextFrame,
                RenderTargetVFlip = true,
            });
            Viewport.AddChild(Background = new ColorRect()
            {
                Color = Color.Color8(0, 0, 0, 255),
                RectSize = Viewport.Size,
            });
            AddChild(Shape = new CollisionShape()
            {
                Shape = MenuScreenShape,
                Transform = new Transform(Basis.Identity, new Vector3(0f, Assets.HalfWallHeight, -Assets.PixelWidth)),
            });
            Shape.AddChild(MeshInstance = new MeshInstance()
            {
                Mesh = new QuadMesh()
                {
                    Size = new Vector2(Width, Height),
                },
                MaterialOverride = new SpatialMaterial()
                {
                    AlbedoTexture = Viewport.GetTexture(),
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    ParamsCullMode = SpatialMaterial.CullMode.Back,
                    FlagsTransparent = false,
                },
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, Assets.PixelWidth)),
            });
            Color = Assets.Palette[(uint)Assets.XML.Element("Menus").Attribute("BkgdColor")];

        }
    }
}
