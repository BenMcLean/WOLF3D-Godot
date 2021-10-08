using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class PixelRect : Target2D
	{
		public override Vector2 Size
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
				NWColor = Assets.Palettes[0][bordColor];
			if (uint.TryParse(xElement.Attribute("Bord2Color")?.Value, out uint bord2Color))
				SEColor = Assets.Palettes[0][bord2Color];
			if (uint.TryParse(xElement.Attribute("Color")?.Value, out uint color))
				Color = Assets.Palettes[0][color];
			if (float.TryParse(xElement.Attribute("X")?.Value, out float x) && float.TryParse(xElement.Attribute("Y")?.Value, out float y))
				Position = new Vector2(x, y);
			if (float.TryParse(xElement.Attribute("Width")?.Value, out float width) && float.TryParse(xElement.Attribute("Height")?.Value, out float height))
				Size = new Vector2(width, height);
			return this;
		}
	}
}
