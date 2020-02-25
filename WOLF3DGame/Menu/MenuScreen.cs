﻿using Godot;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame.Menu
{
    public class MenuScreen : StaticBody
    {
        public const uint ScreenWidth = 320;
        public const uint ScreenHeight = 200;
        public const float Width = Assets.WallWidth;
        public const float Height = Width / 4f * 3f;
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
            get => Background.Color;
            set => /*WorldEnvironment.Environment.BackgroundColor = */Background.Color = value;
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
            Color = Assets.Palette[(uint)Assets.XML.Element("VgaGraph").Element("Menus").Attribute("BkgdColor")];

            Viewport.AddChild(XBanner(Assets.PicTexture("C_OPTIONSPIC")));

            Viewport.AddChild(Sprite = new Sprite()
            {
                Transform = new Transform2D(0f, new Vector2(160f, 100f)),
            });
            Viewport.AddChild(Words = new Sprite()
            {
                Transform = new Transform2D(0f, new Vector2(160f, 180f)),
            });

            Viewport.AddChild(new PixelRect()
            {
                Position = new Vector2(160, 100),
                Size = new Vector2(5, 5),
                NWColor = Color.Color8(255, 0, 0, 255),
                SEColor = Color.Color8(0, 255, 0, 255),
                Color = Color.Color8(0, 0, 255, 255),
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

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event.IsActionPressed("toggle_fullscreen"))
                OS.WindowFullscreen = !OS.WindowFullscreen;
            if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_left"))
                ShowSprite--;
            if (@event.IsActionPressed("ui_down") || @event.IsActionPressed("ui_right"))
                ShowSprite++;
        }

        public Sprite XBanner(Texture texture, uint x = 0, uint y = 0) => new Sprite()
        {
            Texture = texture,
            RegionEnabled = true,
            RegionRect = new Rect2(new Vector2(x, 0f), new Vector2(1, texture.GetSize().y)),
            Transform = new Transform2D(0f, new Vector2(Viewport.Size.x / 2f, texture.GetSize().y / 2f + y)),
            Scale = new Vector2(Viewport.Size.x, 1f),
        };
    }
}
