using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class MenuItem : Target2D
	{
		public Sprite Text { get; set; }
		public Sprite Selected
		{
			get => selected;
			set
			{
				if (selected != null)
					RemoveChild(selected);
				selected = value;
				if (selected != null)
					AddChild(selected);
			}
		}
		private Sprite selected = null;
		public Color? Color
		{
			get => Text?.Modulate;
			set
			{
				if (Text is Sprite)
					Text.Modulate = value == null ? Assets.White : (Color)value;
				if (PixelRect is PixelRect)
					PixelRect.NWColor = PixelRect.SEColor = Text.Modulate;
			}
		}
		public Color TextColor { get; set; } = Assets.White;
		public Color SelectedColor { get; set; } = Assets.White;
		public bool? IsSelected
		{
			get => isSelected;
			set
			{
				if (isSelected == value)
					return;
				isSelected = value;
				if (isSelected == null)
					Selected = null;
				else
				{
					if (Assets.PicTextureSafe(
								XML?.Attribute(PictureName)?.Value
								?? Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute(PictureName)?.Value
							) is Texture texture)
						Selected = new Sprite()
						{
							Texture = texture,
							Position = new Vector2(
								(Text?.Position.x ?? 0) - (Text?.Texture?.GetWidth() ?? 0) / 2 - (texture?.GetWidth() ?? 0) / 2,
								(Text?.Position.y ?? 0) - (Text?.Texture?.GetHeight() ?? 0) / 2 + (texture?.GetHeight() ?? 0) / 2 + 2
								),
						};
				}
			}
		}
		private bool? isSelected = null;
		public string PictureName => IsSelected == null ? null : (IsSelected ?? false) ? "Selected" : "NotSelected";
		public string Condition { get; set; } = null;
		public PixelRect PixelRect
		{
			get => pixelRect;
			set
			{
				if (pixelRect != null)
					RemoveChild(pixelRect);
				AddChild(pixelRect = value);
			}
		}
		private PixelRect pixelRect = null;
		public MenuItem UpdateSelected()
		{
			if (string.IsNullOrWhiteSpace(Condition))
				return this;
			if (typeof(Settings).GetProperty(Condition, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) is PropertyInfo propertyInfo)
				IsSelected = (bool)propertyInfo.GetValue(null, null);
			return this;
		}
		public MenuItem(XElement xml, VgaGraph.Font? defaultFont = null, uint xPadding = 0, Color? defaultTextColor = null, Color? defaultSelectedColor = null)
		{
			XML = xml;
			Condition = xml.Attribute("On")?.Value;
			VgaGraph.Font font = uint.TryParse(xml.Attribute("Font")?.Value, out uint result) ? Assets.Font(result) : defaultFont ?? Assets.Font(0);
			TextColor = byte.TryParse(xml.Attribute("TextColor")?.Value, out byte textColor) ? Assets.Palettes[0][textColor] : defaultTextColor ?? Assets.White;
			SelectedColor = byte.TryParse(xml.Attribute("SelectedColor")?.Value, out byte selectedColor) ? Assets.Palettes[0][selectedColor] : defaultSelectedColor ?? Assets.White;
			if (uint.TryParse(xml.Attribute("BoxColor")?.Value, out uint boxColor))
				PixelRect = new PixelRect()
				{
					Color = Assets.Palettes[0][boxColor],
					NWColor = TextColor,
					SEColor = TextColor,
					Size = new Vector2((uint)xml.Attribute("BoxWidth"), (uint)xml.Attribute("BoxHeight")),
					Position = new Vector2(xPadding + (int.TryParse(xml.Attribute("BoxX")?.Value, out int x) ? x : 0f), int.TryParse(xml.Attribute("BoxY")?.Value, out int y) ? y : 0f),
				};
			string text = xml.Attribute("Text")?.Value is string t && !string.IsNullOrWhiteSpace(t) ? t : string.Empty;
			Name = text.FirstLine() is string firstLine ? firstLine : "MenuItem";
			ImageTexture texture = Assets.Text(font, text);
			AddChild(Text = new Sprite()
			{
				Texture = texture,
				Position = new Vector2(texture.GetWidth() / 2 + xPadding, texture.GetHeight() / 2),
			});
			Size = new Vector2(xPadding + texture.GetWidth(), texture.GetHeight());
			Color = TextColor;
			UpdateSelected();
		}
		public static IEnumerable<MenuItem> MenuItems(XElement menuItems, VgaGraph.Font font, Color? TextColor = null, Color? SelectedColor = null)
		{
			if (uint.TryParse(menuItems.Attribute("Font")?.Value, out uint result))
				font = Assets.Font(result);
			if (byte.TryParse(menuItems.Attribute("TextColor")?.Value, out byte textColor))
				TextColor = Assets.Palettes[0][textColor];
			if (byte.TryParse(menuItems.Attribute("SelectedColor")?.Value, out byte selectedColor))
				SelectedColor = Assets.Palettes[0][selectedColor];
			uint startX = uint.TryParse(menuItems.Attribute("StartX")?.Value, out result) ? result : 0,
				startY = uint.TryParse(menuItems.Attribute("StartY")?.Value, out result) ? result : 0,
				paddingX = uint.TryParse(menuItems.Attribute("PaddingX")?.Value, out result) ? result : 0,
				paddingY = uint.TryParse(menuItems.Attribute("PaddingY")?.Value, out result) ? result : 0,
				count = 0;
			foreach (XElement menuItem in menuItems.Elements("MenuItem"))
				if (Main.InGameMatch(menuItem))
					yield return new MenuItem(
						menuItem,
						uint.TryParse(menuItem.Attribute("Font")?.Value, out result) ? Assets.Font(result) : font,
						paddingX,
						TextColor,
						SelectedColor
						)
					{
						Position = new Vector2(
							startX,
							startY + count++ * (font.Height + paddingY)
							),
					};
		}
	}
}
