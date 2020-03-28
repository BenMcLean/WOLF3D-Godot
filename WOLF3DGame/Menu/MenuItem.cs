using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuItem : Node2D
    {
        public Sprite Text { get; set; }
        public Color Color
        {
            get => Text.Modulate;
            set => Text.Modulate = value;
        }
        public MenuItem(VgaGraph.Font font, string text = "", uint xPadding = 0)
        {
            ImageTexture texture = Assets.Text(font, Name = text);
            AddChild(Text = new Sprite()
            {
                Texture = texture,
                Position = new Vector2(texture.GetWidth() / 2 + xPadding, texture.GetHeight() / 2),
            });
        }
        public static MenuItem[] MenuItems(XElement menu)
        {
            VgaGraph.Font font = Assets.Font(uint.TryParse(menu.Attribute("Font")?.Value, out uint result) ? result : 0);
            uint xPadding = uint.TryParse(menu.Attribute("XPadding")?.Value, out result) ? result : 0;
            uint yPadding = uint.TryParse(menu.Attribute("YPadding")?.Value, out result) ? result : 0;
            List<MenuItem> items = new List<MenuItem>();
            foreach (XElement option in menu.Elements("Option"))
                items.Add(new MenuItem(font, option.Attribute("Name").Value, xPadding)
                {
                    Position = new Vector2(
                        (uint)menu.Attribute("StartX"),
                        (uint)menu.Attribute("StartY") + items.Count() * font.Height + yPadding
                        ),
                    Color = byte.TryParse(menu.Attribute("TextColor")?.Value, out byte index) ? Assets.Palette[index] : Assets.White,
                });
            return items.ToArray();
        }
    }
}
