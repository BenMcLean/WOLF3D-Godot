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
                menuScreen = value;
                ((SpatialMaterial)(MeshInstance.MaterialOverride)).AlbedoTexture = menuScreen.GetTexture();
            }
        }
        private MenuScreen menuScreen = null;
        public CollisionShape Shape { get; private set; }
        public MeshInstance MeshInstance { get; private set; }

        public MenuBody(MenuScreen menuScreen)
        {
            Main.BackgroundColor = Color.Color8(0, 0, 0, 255);
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
            AddChild(MenuScreen = menuScreen);
            AddChild(Cube);
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
        public MenuBody TargetLocal(Vector3 localPosition)
        {
            Cube.Transform = new Transform(Cube.Transform.basis, localPosition);
            return this;
        }

        public MeshInstance Cube = new MeshInstance()
        {
            Mesh = new CubeMesh()
            {
                Size = new Vector3(Assets.PixelWidth, Assets.PixelWidth, Assets.PixelWidth),
            },
            MaterialOverride = new SpatialMaterial()
            {
                AlbedoColor = Color.Color8(255, 0, 255, 255), // Purple
                FlagsUnshaded = true,
                FlagsDoNotReceiveShadows = true,
                FlagsDisableAmbientLight = true,
                FlagsTransparent = false,
                ParamsCullMode = SpatialMaterial.CullMode.Disabled,
                ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            },
        };
    }
}
