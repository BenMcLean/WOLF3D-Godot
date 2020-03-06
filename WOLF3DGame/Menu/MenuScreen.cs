using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3DGame.Menu
{
    public class MenuScreen : Viewport
    {
        public const uint ScreenWidth = 320;
        public const uint ScreenHeight = 200;
        public ColorRect Background { get; private set; }

        public Color Color
        {
            get => Background.Color;
            set => Background.Color = value;
        }

        public MenuScreen()
        {
            Size = new Vector2(ScreenWidth, ScreenHeight);
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
        }

        public static Sprite XBanner(Texture texture, float x = 0, float y = 0) => new Sprite()
        {
            Texture = texture,
            RegionEnabled = true,
            RegionRect = new Rect2(new Vector2(x, 0f), new Vector2(1, texture.GetSize().y)),
            Position = new Vector2(ScreenWidth / 2f, texture.GetSize().y / 2f + y),
            Scale = new Vector2(ScreenWidth, 1f),
        };
    }
}
