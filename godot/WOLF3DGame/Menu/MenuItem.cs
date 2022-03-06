﻿using Godot;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class MenuItem : Target2D
	{
		public Label Label
		{
			get => label;
			set
			{
				if (label != null)
					RemoveChild(label);
				label = value;
				if (label != null)
					AddChild(label);
			}
		}
		private Label label;
		public string Text
		{
			get => Label?.Text;
			set
			{
				if (Label is Label)
				{
					Label.Text = (((XML.Attribute("Action")?.Value?.Equals("Save", System.StringComparison.InvariantCultureIgnoreCase) ?? false)
						|| (XML.Attribute("Action")?.Value?.Equals("Load", System.StringComparison.InvariantCultureIgnoreCase) ?? false)
						) && XML.Attribute("Argument")?.Value is string argument
						&& System.IO.Path.Combine(Main.Folder, argument) is string file
						&& System.IO.File.Exists(file)
						&& XElement.Load(file) is XElement saveGame
						&& saveGame.Attribute("Name")?.Value is string name) ?
						name.FirstLine()
						: value;
					Label.RectPosition = new Vector2(XPadding + Label.RectSize.x / 2f, Label.RectSize.y / 2f);
					if (!(PixelRect is PixelRect))
						Size = Label.RectSize;
				}
			}
		}
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
			get => Label?.Modulate;
			set
			{
				if (Label is Label)
				{
					Label.Modulate = value ?? Assets.White;
					if (PixelRect is PixelRect)
						PixelRect.NWColor = PixelRect.SEColor = Label.Modulate;
				}
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
		public MenuItem(XElement xml, BitmapFont defaultFont = null, int xPadding = 0, Color? defaultTextColor = null, Color? defaultSelectedColor = null)
		{
			XML = xml;
			Condition = xml.Attribute("On")?.Value;
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
			Label = new Label()
			{
				Theme = Assets.BitmapFontThemes[int.TryParse(xml.Attribute("Font")?.Value, out int result) ? result : 0],
			};
			Label.Set("custom_constants/line_spacing", 0);
			UpdateText();
			Color = TextColor;
			UpdateSelected();
		}
		public static IEnumerable<MenuItem> MenuItems(XElement menuItems, BitmapFont font, Color? TextColor = null, Color? SelectedColor = null)
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
