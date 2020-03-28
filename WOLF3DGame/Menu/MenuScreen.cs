using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuScreen : Viewport
    {
        public const uint Width = 320;
        public const uint Height = 200;
        public ColorRect Background { get; private set; }
        public static readonly Vector2 OffScreen = new Vector2(-2, -2);
        public Crosshairs Crosshairs { get; private set; } = new Crosshairs()
        {
            Position = OffScreen,
        };

        public Color Color
        {
            get => Background.Color;
            set => Main.BackgroundColor = Background.Color = value;
        }

        public MenuScreen()
        {
            Size = new Vector2(Width, Height);
            Disable3d = true;
            RenderTargetClearMode = ClearMode.OnlyNextFrame;
            RenderTargetVFlip = true;
            AddChild(Background = new ColorRect()
            {
                Color = Color.Color8(0, 0, 0, 255),
                RectSize = Size,
            });
        }

        public MenuScreen(XElement menu) : this()
        {
            Color = Assets.Palette[(uint)menu.Attribute("BkgdColor")];
            foreach (XElement image in menu.Elements("Image"))
            {
                ImageTexture texture = Assets.PicTexture(image.Attribute("Name").Value);
                if (image.Attribute("XBanner") != null)
                    AddChild(XBanner(texture, (uint)image.Attribute("XBanner"), (float)image.Attribute("Y")));
                AddChild(new Sprite()
                {
                    Texture = texture,
                    Position = new Vector2((float)image.Attribute("X") + texture.GetSize().x / 2f, (float)image.Attribute("Y") + texture.GetSize().y / 2f),
                });
            }
            foreach (XElement pixelRect in menu.Elements("PixelRect"))
                AddChild(new PixelRect(pixelRect));
            foreach (MenuItem item in MenuItems = MenuItem.MenuItems(menu))
                AddChild(item);
            AddChild(Crosshairs);
        }

        public MenuItem[] MenuItems { get; set; }

        public static Sprite XBanner(Texture texture, float x = 0, float y = 0) => new Sprite()
        {
            Texture = texture,
            RegionEnabled = true,
            RegionRect = new Rect2(new Vector2(x, 0f), new Vector2(1, texture.GetSize().y)),
            Position = new Vector2(Width, texture.GetSize().y / 2f + y),
            Scale = new Vector2(Width, 1f),
        };
    }
}
