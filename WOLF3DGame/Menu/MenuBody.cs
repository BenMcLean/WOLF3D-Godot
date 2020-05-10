using Godot;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuBody : StaticBody
    {
        public const float Width = Assets.WallWidth;
        public const float Height = Width / 4f * 3f;
        public static readonly BoxShape MenuScreenShape = new BoxShape()
        {
            Extents = new Vector3(Width / 2f, Height / 2f, Assets.PixelWidth / 2f),
        };
        public MenuScreen MenuScreen
        {
            get => menuScreen;
            set
            {
                if (menuScreen != null)
                    RemoveChild(menuScreen);
                AddChild(menuScreen = value);
                ((SpatialMaterial)(MeshInstance.MaterialOverride)).AlbedoTexture = menuScreen.GetTexture();
            }
        }
        private MenuScreen menuScreen = null;
        public CollisionShape Shape { get; private set; }
        public MeshInstance MeshInstance { get; private set; }

        public MenuBody(MenuScreen menuScreen)
        {
            Name = "MenuBody";
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
                    FlagsUnshaded = true,
                    FlagsDoNotReceiveShadows = true,
                    FlagsDisableAmbientLight = true,
                    ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    ParamsCullMode = SpatialMaterial.CullMode.Back,
                    FlagsTransparent = false,
                },
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, Assets.PixelWidth)),
            });
            MenuScreen = menuScreen;
        }

        /*
        Viewport.AddChild(Sprite = new Sprite()
        {
            Transform = new Transform2D(0f, new Vector2(160f, 100f)),
        });
        Viewport.AddChild(Words = new Sprite()
        {
            Transform = new Transform2D(0f, new Vector2(160f, 180f)),
        });

        ShowSprite = 0;
    }

    private Sprite Sprite;
    private Sprite Words;
    private int ShowSprite
    {
        get => showSprite;
        set
        {
            showSprite = Direction8.Modulus(value, Assets.PicTextures.Length);
            Sprite.Texture = Assets.PicTextures[showSprite];
            XElement pic = (from e in Assets.XML.Element("VgaGraph").Elements("Pic")
                            where (uint)e.Attribute("Number") == showSprite
                            select e).FirstOrDefault();
            Words.Texture = Assets.Text("Chunk " +
                pic?.Attribute("Chunk")?.Value +
                ", Pic " + showSprite + ": \"" +
                pic?.Attribute("Name")?.Value +
                "\"");
        }
    }
    private int showSprite = 0;
    */

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("toggle_fullscreen"))
                OS.WindowFullscreen = !OS.WindowFullscreen;
            //if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_left"))
            //    ShowSprite--;
            //if (@event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_right"))
            //    ShowSprite++;
        }

        public bool Target(Vector3? position = null) => position is Vector3 vector3 ? TargetLocal(ToLocal(vector3)) : TargetLocal();

        public bool TargetLocal(Vector3? localPosition = null) =>
            MenuScreen == null ? false
            : MenuScreen.Target(localPosition == null ? MenuScreen.OffScreen
                : new Vector2(
                    (((Vector3)localPosition).x + (Width / 2f)) / Width * MenuScreen.Width,
                     MenuScreen.Height - (((Vector3)localPosition).y - Assets.HalfWallHeight + Height / 2f) / Height * MenuScreen.Height
                  ));
    }
}
