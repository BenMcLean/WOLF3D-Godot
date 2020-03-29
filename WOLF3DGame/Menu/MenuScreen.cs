using Godot;
using System.Collections.Generic;
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
        public Color TextColor { get; set; }
        public Color SelectedColor { get; set; }
        public Color DisabledColor { get; set; }
        public uint StartX { get; set; }
        public uint StartY { get; set; }
        public uint PaddingX { get; set; }
        public uint PaddingY { get; set; }
        public ImageTexture[] Cursors { get; set; }
        public Sprite Cursor { get; set; }

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
            if (menu.Attribute("TextColor") != null)
                TextColor = Assets.Palette[(uint)menu.Attribute("TextColor")];
            if (menu.Attribute("SelectedColor") != null)
                SelectedColor = Assets.Palette[(uint)menu.Attribute("SelectedColor")];
            if (menu.Attribute("DisabledColor") != null)
                DisabledColor = Assets.Palette[(uint)menu.Attribute("DisabledColor")];
            StartX = uint.TryParse(menu.Attribute("StartX")?.Value, out uint result) ? result : 0;
            StartY = uint.TryParse(menu.Attribute("StartY")?.Value, out result) ? result : 0;
            PaddingX = uint.TryParse(menu.Attribute("XPadding")?.Value, out result) ? result : 0;
            PaddingY = uint.TryParse(menu.Attribute("YPadding")?.Value, out result) ? result : 0;
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
            if (menu.Element("Cursor") is XElement cursor && cursor != null)
            {
                List<ImageTexture> cursors = new List<ImageTexture>();
                if (cursor.Attribute("Cursor1") != null)
                    cursors.Add(Assets.PicTexture(cursor.Attribute("Cursor1")?.Value));
                if (cursor.Attribute("Cursor2") != null)
                    cursors.Add(Assets.PicTexture(cursor.Attribute("Cursor2")?.Value));
                Cursors = cursors.ToArray();
                AddChild(Cursor = new Sprite()
                {
                    Texture = Cursors[0],
                    Position = new Vector2(StartX + Cursors[0].GetWidth() / 2, StartY + Cursors[0].GetHeight() / 2),
                });
            }
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
