﻿using Godot;
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
			set => Text.Modulate = value == null ? Assets.White : (Color)value;
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
					ImageTexture texture = Assets.PicTextureSafe(
							XML?.Attribute(PictureName)?.Value ??
							Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute(PictureName)?.Value
							);
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

		public MenuItem UpdateSelected()
		{
			if (string.IsNullOrWhiteSpace(Condition))
				return this;
			if (typeof(Settings).GetProperty(Condition, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) is PropertyInfo propertyInfo)
				IsSelected = (bool)propertyInfo.GetValue(null, null);
			return this;
		}

		public MenuItem(VgaGraph.Font font, string text = "", uint xPadding = 0, string condition = null, Color? textColor = null, Color? selectedColor = null, XElement xml = null)
		{
			Name = text.FirstLine() is string firstLine ? firstLine : "MenuItem";
			Condition = condition;
			XML = xml;
			TextColor = byte.TryParse(XML?.Attribute("TextColor")?.Value, out byte tColor) ? Assets.Palettes[0][tColor] : textColor ?? Assets.White;
			SelectedColor = byte.TryParse(XML.Attribute("SelectedColor")?.Value, out byte sColor) ? Assets.Palettes[0][sColor] : selectedColor ?? Assets.White;
			ImageTexture texture = Assets.Text(font, Name = text);
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
						uint.TryParse(menuItem.Attribute("Font")?.Value, out result) ? Assets.Font(result) : font,
						menuItem.Attribute("Text")?.Value ?? "MenuItem",
						paddingX,
						menuItem.Attribute("On")?.Value,
						TextColor,
						SelectedColor,
						menuItem
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
