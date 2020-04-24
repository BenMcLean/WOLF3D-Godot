using Godot;
using System.Collections.Generic;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuItem : Node2D, ITarget
    {
        public bool Target(Vector2 vector2) => TargetLocal(vector2 - Position);
        public bool Target(float x, float y) => TargetLocal(x - Position.x, y - Position.y);
        public bool TargetLocal(Vector2 vector2) => TargetLocal(vector2.x, vector2.y);
        public bool TargetLocal(float x, float y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public XElement XML { get; set; }
        public Sprite Text { get; set; }
        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;

        public Color? Color
        {
            get => Text?.Modulate;
            set => Text.Modulate = value == null ? Assets.White : (Color)value;
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

        public static IEnumerable<MenuItem> MenuItems(XElement menuItems, VgaGraph.Font font, Color? color = null)
        {
            if (uint.TryParse(menuItems.Attribute("Font")?.Value, out uint result))
                font = Assets.Font(result);
            if (byte.TryParse(menuItems.Attribute("Color")?.Value, out byte index))
                color = Assets.Palette[index];
            else if (color == null)
                color = Assets.White;
            uint startX = uint.TryParse(menuItems.Attribute("StartX")?.Value, out result) ? result : 0,
                startY = uint.TryParse(menuItems.Attribute("StartY")?.Value, out result) ? result : 0,
                paddingX = uint.TryParse(menuItems.Attribute("PaddingX")?.Value, out result) ? result : 0,
                paddingY = uint.TryParse(menuItems.Attribute("PaddingY")?.Value, out result) ? result : 0,
                count = 0;
            foreach (XElement menuItem in menuItems.Elements("MenuItem"))
                if (Main.InGameMatch(menuItem))
                    yield return new MenuItem(
                        uint.TryParse(menuItem.Attribute("Font")?.Value, out result) ? Assets.Font(result) : font,
                        menuItem.Attribute("Text").Value, paddingX)
                    {
                        XML = menuItem,
                        Position = new Vector2(
                            startX,
                            startY + count++ * (font.Height + paddingY)
                            ),
                        Color = byte.TryParse(menuItem.Attribute("Color")?.Value, out index) ? Assets.Palette[index] : color,
                    };
        }
    }
}
