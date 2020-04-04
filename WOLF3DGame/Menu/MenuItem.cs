using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuItem : Node2D
    {
        public XElement XML { get; set; }
        public Sprite Text { get; set; }
        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;
        public bool Target(Vector2 vector2) => TargetLocal(ToLocal(vector2));
        public bool TargetLocal(Vector2 vector2) => TargetLocal(vector2.x, vector2.y);
        public bool TargetLocal(float x, float y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public Color Color
        {
            get => Text.Modulate;
            set => Text.Modulate = value;
        }
        public MenuItem(VgaGraph.Font font, string text = "", uint xPadding = 0)
        {
            Name = text;
            ImageTexture texture = Assets.Text(font, Name = text);
            AddChild(Text = new Sprite()
            {
                Texture = texture,
                Position = new Vector2(texture.GetWidth() / 2 + xPadding, texture.GetHeight() / 2),
            });
            Width = xPadding + texture.GetWidth();
            Height = texture.GetHeight();
        }
        public static MenuItem[] MenuItems(XElement menu)
        {
            VgaGraph.Font font = Assets.Font(uint.TryParse(menu.Attribute("Font")?.Value, out uint result) ? result : 0);
            Color color = byte.TryParse(menu.Attribute("TextColor")?.Value, out byte index) ? Assets.Palette[index] : Assets.White;
            uint startX = uint.TryParse(menu.Attribute("StartX")?.Value, out result) ? result : 0,
                startY = uint.TryParse(menu.Attribute("StartY")?.Value, out result) ? result : 0,
                paddingX = uint.TryParse(menu.Attribute("PaddingX")?.Value, out result) ? result : 0,
                paddingY = uint.TryParse(menu.Attribute("PaddingY")?.Value, out result) ? result : 0;
            List<MenuItem> items = new List<MenuItem>();
            foreach (XElement option in menu.Elements("Option"))
                items.Add(new MenuItem(font, option.Attribute("Name").Value, paddingX)
                {
                    XML = option,
                    Position = new Vector2(
                        startX,
                        startY + items.Count() * font.Height + paddingY
                        ),
                    Color = color,
                });
            return items.Count > 0 ? items.ToArray() : null;
        }
    }
}
