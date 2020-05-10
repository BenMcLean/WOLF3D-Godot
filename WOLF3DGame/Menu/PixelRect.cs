using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class PixelRect : Node2D, ITarget
    {
        public bool Target(Vector2 vector2) => TargetLocal(vector2 - Position);
        public bool Target(float x, float y) => TargetLocal(x - Position.x, y - Position.y);
        public bool TargetLocal(Vector2 vector2) => TargetLocal(vector2.x, vector2.y);
        public bool TargetLocal(float x, float y) => x >= 0 && y >= 0 && x < Size.x && y < Size.y;

        public Vector2 Size
        {
            get => SEBorder.RectSize;
            set
            {
                SEBorder.RectSize = value;
                NWBorder.RectSize = new Vector2(value.x - 1, value.y - 1);
                ColorRect.RectSize = new Vector2(value.x - 2, value.y - 2);
            }
        }
        public ColorRect SEBorder { get; private set; }
        public ColorRect NWBorder { get; private set; }
        public ColorRect ColorRect { get; private set; }
        public Color SEColor
        {
            get => SEBorder.Color;
            set => SEBorder.Color = value;
        }
        public Color NWColor
        {
            get => NWBorder.Color;
            set => NWBorder.Color = value;
        }
        public Color Color
        {
            get => ColorRect.Color;
            set => ColorRect.Color = value;
        }
        public PixelRect()
        {
            Name = "PixelRect";
            AddChild(SEBorder = new ColorRect());
            AddChild(NWBorder = new ColorRect());
            AddChild(ColorRect = new ColorRect()
            {
                RectPosition = new Vector2(1, 1),
            });
        }
        public PixelRect(XElement xElement) : this() => Set(xElement);

        public PixelRect Set(XElement xElement)
        {
            if (uint.TryParse(xElement.Attribute("BordColor")?.Value, out uint bordColor))
                NWColor = Assets.Palette[bordColor];
            if (uint.TryParse(xElement.Attribute("Bord2Color")?.Value, out uint bord2Color))
                SEColor = Assets.Palette[bord2Color];
            if (uint.TryParse(xElement.Attribute("Color")?.Value, out uint color))
                Color = Assets.Palette[color];
            if (float.TryParse(xElement.Attribute("X")?.Value, out float x) && float.TryParse(xElement.Attribute("Y")?.Value, out float y))
                Position = new Vector2(x, y);
            if (float.TryParse(xElement.Attribute("Width")?.Value, out float width) && float.TryParse(xElement.Attribute("Height")?.Value, out float height))
                Size = new Vector2(width, height);
            return this;
        }
    }
}
