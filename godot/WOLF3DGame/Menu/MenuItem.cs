using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class MenuItem : Target2D
	{
		public VgaGraph.Font Font { get; set; }
		public Sprite TextSprite
		{
			get => textSprite;
			set
			{
				if (textSprite != null)
					RemoveChild(textSprite);
				textSprite = value;
				if (textSprite != null)
					AddChild(textSprite);
			}
		}
		private Sprite textSprite;
		public string Text
		{
			get => text;
			set
			{
				if (((XML.Attribute("Action")?.Value?.Equals("Save", System.StringComparison.InvariantCultureIgnoreCase) ?? false)
					|| (XML.Attribute("Action")?.Value?.Equals("Load", System.StringComparison.InvariantCultureIgnoreCase) ?? false)
					) && XML.Attribute("Argument")?.Value is string argument
					&& System.IO.Path.Combine(Main.Folder, argument) is string file
					&& System.IO.File.Exists(file)
					&& XElement.Load(file) is XElement saveGame
					&& saveGame.Attribute("Name")?.Value is string name)
					text = name.FirstLine();
				else
					text = value?.FirstLine();
				if (string.IsNullOrWhiteSpace(text))
				{
					TextSprite = null;
					return;
				}
				if (PixelRect is PixelRect)
					while (text.Length > 1 && Font.CalcWidthLine(text) > PixelRect.Size.x - 4)
						text = text.Substring(0, text.Length - 1);
				Name = "MenuItem " + text.Trim();
				ImageTexture texture = Assets.Text(Font, string.IsNullOrWhiteSpace(text) ? " " : text);
				uint textWidth = (uint)texture.GetWidth();
				if (textWidth % 2 > 0) textWidth++;
				TextSprite = new Sprite()
				{
					Texture = texture,
					Position = new Vector2((textWidth / 2) + XPadding, texture.GetHeight() / 2),
				};
				if (PixelRect is Target2D target2D)
				{
					Offset = target2D.Position + target2D.Offset;
					Size = target2D.Size;
				}
				else
					Size = new Vector2(XPadding + texture.GetWidth(), texture.GetHeight());
			}
		}
		private string text = null;
		public MenuItem UpdateText()
		{
			Text = XML.Attribute("Text")?.Value;
			return this;
		}
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
			get => TextSprite?.Modulate;
			set
			{
				if (TextSprite is Sprite)
					TextSprite.Modulate = value == null ? Assets.White : (Color)value;
				if (PixelRect is PixelRect)
					PixelRect.NWColor = PixelRect.SEColor = TextSprite.Modulate;
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
								(TextSprite?.Position.x ?? 0) - (TextSprite?.Texture?.GetWidth() ?? 0) / 2 - (texture?.GetWidth() ?? 0) / 2,
								(TextSprite?.Position.y ?? 0) - (TextSprite?.Texture?.GetHeight() ?? 0) / 2 + (texture?.GetHeight() ?? 0) / 2 + 2
								),
						};
				}
			}
		}
		private bool? isSelected = null;
		public string PictureName => IsSelected == null ? null : (IsSelected ?? false) ? "Selected" : "NotSelected";
		public string Condition { get; set; } = null;
		public int XPadding = 0;
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
		public MenuItem(XElement xml, VgaGraph.Font? defaultFont = null, int xPadding = 0, Color? defaultTextColor = null, Color? defaultSelectedColor = null)
		{
			XML = xml;
			Condition = xml.Attribute("On")?.Value;
			Font = uint.TryParse(xml.Attribute("Font")?.Value, out uint result) ? Assets.Font(result) : defaultFont ?? Assets.Font(0);
			XPadding = xPadding;
			TextColor = byte.TryParse(xml.Attribute("TextColor")?.Value, out byte textColor) ? Assets.Palettes[0][textColor] : defaultTextColor ?? Assets.White;
			SelectedColor = byte.TryParse(xml.Attribute("SelectedColor")?.Value, out byte selectedColor) ? Assets.Palettes[0][selectedColor] : defaultSelectedColor ?? Assets.White;
			if (uint.TryParse(xml.Attribute("BoxColor")?.Value, out uint boxColor))
				PixelRect = new PixelRect()
				{
					Color = Assets.Palettes[0][boxColor],
					NWColor = TextColor,
					SEColor = TextColor,
					Size = new Vector2((uint)xml.Attribute("BoxWidth"), (uint)xml.Attribute("BoxHeight")),
					Position = new Vector2(XPadding + (int.TryParse(xml.Attribute("BoxX")?.Value, out int x) ? x : 0f), int.TryParse(xml.Attribute("BoxY")?.Value, out int y) ? y : 0f),
				};
			UpdateText();
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
				count = 0;
			int paddingX = int.TryParse(menuItems.Attribute("PaddingX")?.Value, out int i) ? i : 0,
				paddingY = int.TryParse(menuItems.Attribute("PaddingY")?.Value, out i) ? i : 0;
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
