﻿using Godot;
using System;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.OPL;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class Modal : Node2D, ITarget
	{
		public bool IsIn(Vector2 vector2) => IsInLocal(vector2 - Position);
		public bool IsIn(float x, float y) => IsInLocal(x - Position.x, y - Position.y);
		public bool IsInLocal(Vector2 vector2) => IsInLocal(vector2.x, vector2.y);
		public bool IsInLocal(float x, float y)
		{
			if (Yes?.IsIn(x, y) ?? false)
				return Answer = true;
			Answer = false;
			return (PixelRect?.IsIn(x, y) ?? false) || (No?.IsIn(x, y) ?? false);
		}
		public bool IsIn(Vector3 vector3) => IsIn(Assets.Vector2(vector3));
		public bool IsIn(float x, float y, float z) => IsIn(x, z);
		public bool IsInLocal(Vector3 vector3) => IsInLocal(Assets.Vector2(vector3));
		public bool IsInLocal(float x, float y, float z) => IsInLocal(x, z);
		public bool IsWithin(float x, float y, float distance) =>
			Math.Abs(GlobalTransform.origin.x - x) < distance && Math.Abs(GlobalTransform.origin.y - y) < distance;

		public bool Answer
		{
			get => answer;
			set
			{
				if (answer != value)
				{
					if (Assets.ScrollSound != null)
						SoundBlaster.Adl = Assets.ScrollSound;
					answer = value;
				}
			}
		}
		private bool answer = false;
		public Modal(Sprite text)
		{
			Name = "Modal";
			AddChild(PixelRect = new PixelRect()
			{
				Size = new Vector2(text.Texture.GetSize().x + 10, text.Texture.GetSize().y + 12),
				Position = new Vector2(text.Texture.GetSize().x / -2 - 5, text.Texture.GetSize().y / -2 - 6),
			});
			AddChild(Text = text);
		}
		public Modal(Sprite text, XElement xElement) : this(text) => Set(xElement);
		public Modal Set(XElement xElement)
		{
			if (xElement == null)
				return this;
			if (uint.TryParse(xElement.Attribute("TextColor")?.Value, out uint textColor))
				TextColor = Assets.Palettes[0][textColor];
			if (uint.TryParse(xElement.Attribute("BordColor")?.Value, out uint bordColor))
				NWColor = Assets.Palettes[0][bordColor];
			if (uint.TryParse(xElement.Attribute("Bord2Color")?.Value, out uint bord2Color))
				SEColor = Assets.Palettes[0][bord2Color];
			if (uint.TryParse(xElement.Attribute("Color")?.Value, out uint color))
				Color = Assets.Palettes[0][color];
			return this;
		}
		public PixelRect PixelRect { get; set; }
		public Sprite Text { get; set; }
		public bool YesNo
		{
			get => Yes != null;
			set
			{
				if (value)
				{
					Yes = new Modal(new Sprite()
					{
						Texture = Assets.Text(Assets.ModalFont, "Yes"),
					})
					{
						SEColor = SEColor,
						NWColor = NWColor,
						Color = Color,
						TextColor = TextColor,
					};
					Yes.Position = new Vector2(Size.x / -2 + Yes.Size.x / 2, Size.y / 2 + Yes.Size.y / 2);
					No = new Modal(new Sprite()
					{
						Texture = Assets.Text(Assets.ModalFont, "No"),
					})
					{
						SEColor = SEColor,
						NWColor = NWColor,
						Color = Color,
						TextColor = TextColor,
					};
					No.Position = new Vector2(Size.x / 2 - No.Size.x / 2, Size.y / 2 + No.Size.y / 2);
				}
				else
					Yes = No = null;
			}
		}
		public Modal Yes
		{
			get => yes;
			set
			{
				if (yes != null)
					RemoveChild(yes);
				yes = value;
				if (yes != null)
					AddChild(yes);
			}
		}
		private Modal yes = null;
		public Modal No
		{
			get => no;
			set
			{
				if (no != null)
					RemoveChild(no);
				no = value;
				if (no != null)
					AddChild(no);
			}
		}
		private Modal no = null;

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
		public Vector2 Size
		{
			get => PixelRect.Size;
			set => PixelRect.Size = value;
		}
		public Vector2 Offset { get; set; } = Vector2.Zero;
		public enum QuestionEnum
		{
			QUIT, END
		}
	}
}
