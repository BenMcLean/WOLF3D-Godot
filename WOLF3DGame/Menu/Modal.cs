using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class Modal : Node2D
    {
        public Modal(Sprite text)
        {
            AddChild(PixelRect = new PixelRect()
            {
                Size = new Vector2(text.Texture.GetSize().x + 10, text.Texture.GetSize().y + 12),
                Position = new Vector2(text.Texture.GetSize().x / -2 - 5, text.Texture.GetSize().y / -2 - 6),
            });
            AddChild(Text = text);
        }
        public Modal SetColors(XElement xElement)
        {
            if (xElement == null)
                return this;
            if (uint.TryParse(xElement.Attribute("TextColor")?.Value, out uint textColor))
                TextColor = Assets.Palette[textColor];
            if (uint.TryParse(xElement.Attribute("BordColor")?.Value, out uint bordColor))
                NWColor = Assets.Palette[bordColor];
            if (uint.TryParse(xElement.Attribute("Bord2Color")?.Value, out uint bord2Color))
                SEColor = Assets.Palette[bord2Color];
            if (uint.TryParse(xElement.Attribute("Color")?.Value, out uint color))
                Color = Assets.Palette[color];
            return this;
        }
        public PixelRect PixelRect { get; set; }
        public Sprite Text { get; set; }
        public Color SEColor
        {
            get => PixelRect.SEColor;
            set => PixelRect.SEColor = value;
        }
        public Color NWColor
        {
            get => PixelRect.NWColor;
            set => PixelRect.NWColor = value;
        }
        public Color Color
        {
            get => PixelRect.Color;
            set => PixelRect.Color = value;
        }
        public Color TextColor
        {
            get => Text.Modulate;
            set => Text.Modulate = value;
        }
    }
}
