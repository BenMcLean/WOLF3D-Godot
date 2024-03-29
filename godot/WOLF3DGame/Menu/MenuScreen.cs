﻿using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
	public class MenuScreen : Viewport, ITarget
	{
		public const ushort Width = 320;
		public const ushort Height = 200;
		public ColorRect Background { get; private set; }
		public static readonly Vector2 OffScreen = new Vector2(-2, -2);
		public Crosshairs Crosshairs { get; private set; } = new Crosshairs()
		{
			Position = OffScreen,
		};
		public XElement XML { get; set; }
		public Theme Theme { get; set; }
		public Modal Modal
		{
			get => modal;
			set
			{
				if (modal != null)
					RemoveChild(modal);
				modal = value;
				if (modal == null)
					Question = null;
				else
					AddChild(modal);
			}
		}
		private Modal modal = null;
		public Modal.QuestionEnum? Question { get; set; } = null;
		public Color Color
		{
			get => Background.Color;
			set => Background.Color = value;
		}
		public Color TextColor { get; set; }
		public Color SelectedColor { get; set; }
		public AtlasTexture[] Cursors { get; set; }
		public int CursorX { get; set; } = 0;
		public int CursorY { get; set; } = 0;
		public TextureRect Cursor { get; set; }
		public AtlasTexture[] Difficulties { get; set; }
		public TextureRect Difficulty { get; set; }
		public MenuScreen()
		{
			Name = "MenuScreen";
			Size = new Vector2(Width, Height);
			Disable3d = true;
			RenderTargetClearMode = ClearMode.OnlyNextFrame;
			RenderTargetVFlip = true;
			AddChild(Background = new ColorRect()
			{
				Color = byte.TryParse(Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("Color")?.Value, out byte color) ? Assets.Palettes[0][color] : Color.Color8(0, 0, 0, 255),
				RectSize = Size,
			});
		}
		public MenuScreen(XElement menu) : this()
		{
			Name = menu.Attribute("Name")?.Value is string name ? name : "MenuScreen";
			XML = menu;
			Theme = Assets.FontThemes[uint.TryParse(menu.Attribute("Font")?.Value, out uint result) ? result : 0];
			if (byte.TryParse(menu.Attribute("BkgdColor")?.Value, out byte bkgdColor))
				Color = Assets.Palettes[0][bkgdColor];
			TextColor = byte.TryParse(menu.Attribute("TextColor")?.Value, out byte tColor) ? Assets.Palettes[0][tColor] : Assets.White;
			SelectedColor = byte.TryParse(menu.Attribute("SelectedColor")?.Value, out byte sColor) ? Assets.Palettes[0][sColor] : Assets.White;
			foreach (XElement e in menu.Elements()?.Where(e => Main.InGameMatch(e)))
				if (e.Name.LocalName.Equals("PixelRect", StringComparison.InvariantCultureIgnoreCase))
					AddChild(new PixelRect(e));
				else if (e.Name.LocalName.Equals("Image", StringComparison.InvariantCultureIgnoreCase))
				{
					AtlasTexture texture = Assets.PicTexture(e.Attribute("Name").Value);
					if (e.Attribute("XBanner") != null)
						AddChild(new TextureRect()
						{
							RectPosition = new Vector2(0f, float.TryParse(e.Attribute("Y")?.Value, out float y) ? y : 0f),
							RectSize = new Vector2(Width, texture.GetSize().y),
							Texture = new AtlasTexture()
							{
								Atlas = texture.Atlas,
								Region = new Rect2(
									texture.Region.Position.x + (uint.TryParse(e.Attribute("XBanner")?.Value, out uint x) && x <= texture.Region.Size.x ? x : 0),
									texture.Region.Position.y,
									1f,
									texture.Region.Size.y
									),
							},
							StretchMode = TextureRect.StretchModeEnum.Tile,
						});
					Vector2 position = new Vector2(
							e.Attribute("X")?.Value?.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
							Width / 2 - texture.GetWidth() / 2f
							: (float)e.Attribute("X"),
							e.Attribute("Y")?.Value?.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
							Height / 2 - texture.GetHeight() / 2f
							: (float)e.Attribute("Y")
							);
					AddChild(new TextureRect()
					{
						Name = "Image " + e.Attribute("Name").Value,
						Texture = texture,
						RectPosition = position,
					});
					if (e.Attribute("Action")?.Value is string action && !string.IsNullOrWhiteSpace(action))
					{
						Target2D target = new Target2D()
						{
							Name = "Target " + e.Attribute("Name").Value,
							XML = e,
							Position = new Vector2(
								position.x - texture.GetWidth() / 2f,
								position.y - texture.GetHeight() / 2f
								),
							Size = texture.GetSize(),
						};
						ExtraItems.Add(target);
						AddChild(target);
					}
				}
				else if (e.Name.LocalName.Equals("Text", StringComparison.InvariantCultureIgnoreCase))
				{
					Label label = new Label()
					{
						Name = "Label " + e.Attribute("String").Value,
						Text = e.Attribute("String").Value,
						Theme = Assets.FontThemes[ushort.TryParse(e.Attribute("Font")?.Value, out ushort fontNumber) && Assets.FontThemes.Length > fontNumber ? fontNumber : 0],
						Modulate = uint.TryParse(e.Attribute("Color")?.Value, out uint color) ? Assets.Palettes[0][color] : TextColor,
					};
					label.RectPosition = new Vector2(
						e.Attribute("X")?.Value.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
							(Width - label.Theme.DefaultFont.Width(label.Text)) / 2
							: uint.TryParse(e.Attribute("X")?.Value, out uint x) ? x : 0,
						e.Attribute("Y")?.Value.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
							(Height - label.Theme.DefaultFont.Height(label.Text)) / 2
							: uint.TryParse(e.Attribute("Y")?.Value, out uint y) ? y : 0
						);
					label.Set("custom_constants/line_spacing", 0);
					AddChild(label);
				}
			foreach (XElement menuItems in menu.Elements("MenuItems") ?? Enumerable.Empty<XElement>())
				if (Main.InGameMatch(menuItems))
					foreach (MenuItem item in MenuItem.MenuItems(menuItems, Theme, TextColor, SelectedColor))
					{
						MenuItems.Add(item);
						AddChild(item);
					}
			foreach (XElement xCounter in menu.Elements("Counter") ?? Enumerable.Empty<XElement>())
			{
				Counter counter = new Counter(xCounter);
				Counters.Add(counter);
				AddChild(counter);
			}
			foreach (XElement xTimer in menu.Elements("Timer") ?? Enumerable.Empty<XElement>())
			{
				Label timer = new Label()
				{
					Name = xTimer.Attribute("Name")?.Value,
					RectPosition = new Vector2(
							uint.TryParse(xTimer.Attribute("X")?.Value, out uint x) ? x : 0,
							uint.TryParse(xTimer.Attribute("Y")?.Value, out uint y) ? y : 0
							),
				};
				if (uint.TryParse(xTimer?.Attribute("Font")?.Value, out uint bitmapFont))
					timer.Theme = Assets.FontThemes[bitmapFont];
				if (Main.InGame)
					if (timer.Name.ToUpperInvariant().Equals("PAR"))
						timer.Text = Main.ActionRoom.Level.MapAnalysis.Par.ToString(@"mm\:ss");
					else if (timer.Name.ToUpperInvariant().Equals("TIME")
						&& TimeSpan.FromSeconds(Main.ActionRoom.Level.Time) is TimeSpan timeSpan)
						timer.Text = timeSpan >= TimeSpan.FromHours(1) ?
							"59:59"
							: timeSpan.ToString(@"mm\:ss");
				AddChild(timer);
			}
			if (menu.Element("Cursor") is XElement cursor && cursor != null && Main.InGameMatch(cursor))
			{
				List<AtlasTexture> cursors = new List<AtlasTexture>();
				if (cursor.Attribute("Cursor1") != null)
					cursors.Add(Assets.PicTexture(cursor.Attribute("Cursor1")?.Value));
				if (cursor.Attribute("Cursor2") != null)
					cursors.Add(Assets.PicTexture(cursor.Attribute("Cursor2")?.Value));
				if (int.TryParse(cursor.Attribute("X")?.Value, out int cursorX))
					CursorX = cursorX;
				if (int.TryParse(cursor.Attribute("Y")?.Value, out int cursorY))
					CursorY = cursorY;
				Cursors = cursors.ToArray();
				if (Cursors.Length > 0)
					AddChild(Cursor = new TextureRect()
					{
						Texture = Cursors[0],
						RectPosition = new Vector2(MenuItems[0].Position.x, MenuItems[0].Position.y),
					});
			}
			if (menu.Element("Difficulty") is XElement difficulty && difficulty != null && Main.InGameMatch(difficulty))
			{
				AtlasTexture texture = Assets.PicTexture(difficulty.Attribute("Difficulty1").Value);
				AddChild(Difficulty = new TextureRect()
				{
					RectPosition = new Vector2(
						uint.TryParse(difficulty.Attribute("X")?.Value, out uint x) ? x : 0,
						uint.TryParse(difficulty.Attribute("Y")?.Value, out uint y) ? y : 0
						),
				});
			}
			if (Main.InGame && menu.Element("StatusBar") is XElement statusBar)
			{
				ViewportTexture texture = Main.StatusBar.GetTexture();
				AddChild(new TextureRect()
				{
					Texture = texture,
					RectPosition = new Vector2(
						uint.TryParse(statusBar.Attribute("X")?.Value, out uint x) ? x : 0,
						uint.TryParse(statusBar.Attribute("Y")?.Value, out uint y) ? y : 0
						),
				});
			}
			Selection = uint.TryParse(XML.Attribute("Default")?.Value, out uint selection) ? (int)selection : 0;
			AddChild(Crosshairs);
		}
		public readonly List<MenuItem> MenuItems = new List<MenuItem>();
		public readonly List<Target2D> ExtraItems = new List<Target2D>();
		public readonly List<Counter> Counters = new List<Counter>();
		public const float BlinkRate = 0.5f;
		public float Blink = 0f;
		public int CursorSprite
		{
			get => cursorSprite;
			set
			{
				cursorSprite = Direction8.Modulus(value, Cursors?.Length ?? 1);
				if (Cursor != null)
					Cursor.Texture = Cursors[cursorSprite];
			}
		}
		private int cursorSprite = 0;
		public int Selection
		{
			get => selection;
			set
			{
				if (selection != value && Assets.ScrollSound != null)
					SoundBlaster.Adl = Assets.ScrollSound;
				if (MenuItems == null || MenuItems.Count <= 0)
				{
					selection = 0;
					if (value > 0 && XML.Element("Down") is XElement d)
						XMLScript.Run(d);
					else if (value < 0 && XML.Element("Up") is XElement up)
						XMLScript.Run(up);
					return;
				}
				MenuItems[selection].Color = MenuItems[selection].TextColor;
				if (value >= MenuItems.Count && XML.Element("Down") is XElement down)
				{
					XMLScript.Run(down);
					return;
				}
				else if (value < 0 && XML.Element("Up") is XElement up)
				{
					XMLScript.Run(up);
					return;
				}
				selection = Direction8.Modulus(value, MenuItems.Count);
				MenuItems[selection].Color = MenuItems[selection].SelectedColor;
				if (Cursor != null)
					Cursor.RectPosition = new Vector2(
						MenuItems[selection].Position.x + CursorX,
						MenuItems[selection].Position.y + CursorY
						);
				if (Difficulty != null && XML?.Element("Difficulty") is XElement difficulty && difficulty != null)
					Difficulty.Texture = Assets.PicTexture(difficulty.Attribute("Difficulty" + (SelectedItem?.XML?.Attribute("Difficulty")?.Value ?? ""))?.Value);
			}
		}
		private int selection = 0;
		public MenuItem SelectedItem
		{
			get => MenuItems == null || MenuItems.Count <= Selection ? null : MenuItems[Selection];
			set
			{
				if (MenuItems == null) return;
				for (int x = 0; x < MenuItems.Count; x++)
					if (value == MenuItems[x])
					{
						Selection = x;
						return;
					}
			}
		}
		public Vector2 Position { get; set; } = Vector2.Zero;
		public Vector2 GlobalPosition { get; set; } = Vector2.Zero;
		public Vector2 Offset { get; set; } = Vector2.Zero;
		public bool IsIn(Vector2 vector2) => IsInLocal(vector2);
		public bool IsIn(float x, float y) => IsInLocal(x, y);
		public bool IsInLocal(float x, float y) => IsInLocal(new Vector2(x, y));
		public bool IsInLocal(Vector2 vector2)
		{
			Crosshairs.Position = vector2;
			if (Modal == null && MenuItems != null)
				for (int x = 0; x < MenuItems.Count; x++)
					if (Selection != x && MenuItems[x].IsIn(vector2))
					{
						Selection = x;
						return true;
					}
			Modal?.IsIn(vector2);
			return false;
		}
		public bool IsIn(Vector3 vector3) => IsIn(Assets.Vector2(vector3));
		public bool IsIn(float x, float y, float z) => IsIn(x, z);
		public bool IsInLocal(Vector3 vector3) => IsInLocal(Assets.Vector2(vector3));
		public bool IsInLocal(float x, float y, float z) => IsInLocal(x, z);
		public virtual bool IsWithin(float x, float y, float distance) =>
			Math.Abs(Width / 2 - x) < distance && Math.Abs(Height / 2 - y) < distance;
		public override void _Process(float delta)
		{
			Blink += delta;
			while (Blink > BlinkRate)
			{
				Blink -= BlinkRate;
				CursorSprite++;
			}
			if (Counters.Count > 1)
				for (int i = 1; i < Counters.Count; i++)
					if (Counters[i - 1].Finished && !Counters[i].Started)
					{
						Counters[i].Started = true;
						break;
					}
		}
		public MenuScreen Update()
		{
			if (MenuItems != null)
				foreach (MenuItem menuItem in MenuItems)
					menuItem.UpdateSelected();
			return this;
		}
		public void DoInput(InputEvent @event)
		{
			if (Modal == null && MenuItems != null)
			{
				if (@event.IsActionPressed("ui_down"))
					Selection++;
				else if (@event.IsActionPressed("ui_up"))
					Selection--;
			}
			else if (Modal != null)
			{
				if (@event.IsActionPressed("ui_yes"))
					Yes();
				else if (@event.IsActionPressed("ui_no"))
					Cancel();
			}
			if (@event.IsActionPressed("ui_accept"))
				Accept();
			else if (@event.IsActionPressed("ui_cancel"))
				Cancel();
		}
		public Target2D GetExtraItem()
		{
			foreach (Target2D target in ExtraItems)
				if (target.IsIn(Crosshairs.GlobalPosition))
					return target;
			return null;
		}
		public MenuScreen Accept()
		{
			if (Modal == null)
			{
				if (GetExtraItem() is Target2D target && target != null)
					XMLScript.Run(target.XML);
				else if (SelectedItem is MenuItem selected && selected != null)
				{
					if (selected.XML.Attribute("SelectSound") is XAttribute selectSound && selectSound != null && !string.IsNullOrWhiteSpace(selectSound.Value))
						SoundBlaster.Adl = Assets.Sound(selectSound.Value);
					else if (Assets.SelectSound != null)
						SoundBlaster.Adl = Assets.SelectSound;
					XMLScript.Run(selected.XML);
					selected.UpdateText().Color = selected.SelectedColor;
				}
			}
			else if (Modal.Answer)
				return Yes();
			else
				Modal = null;
			return this;
		}
		public MenuScreen Cancel()
		{
			if (Modal != null)
				return Accept();
			if (XML.Elements("Cancel")?.Where(c => Main.InGameMatch(c)).FirstOrDefault() is XElement cancel && cancel != null)
				XMLScript.Run(cancel);
			return this;
		}
		public MenuScreen AddModal(string @string = "", ushort padding = 0)
		{
			RemoveChild(Crosshairs);
			Modal = new Modal(@string)
			{
				Position = new Vector2(Width / 2, Height / 2),
			}.Set(Assets.XML?.Element("VgaGraph")?.Element("Menus"));
			AddChild(Crosshairs);
			return this;
		}
		public MenuScreen Yes()
		{
			switch (Question)
			{
				case Modal.QuestionEnum.QUIT:
					Main.Quit();
					break;
				case Modal.QuestionEnum.END:
					Main.End();
					break;
			}
			Modal = null;
			return this;
		}
		public MenuScreen ButtonPressed(MenuRoom menuRoom, int buttonIndex, bool right = false)
		{
			if (!Room.IsVRButton(buttonIndex))
				return this;
			ARVRController controller = right ? menuRoom.RightController : menuRoom.LeftController;
			if (controller != menuRoom.ActiveController)
			{
				menuRoom.ActiveController = controller;
				return this;
			}
			switch (buttonIndex)
			{
				case (int)JoystickList.VrTrigger:
					return Accept();
				case (int)JoystickList.VrGrip:
					return Cancel();
			}
			return this;
		}
		public MenuScreen OnSet()
		{
			Main.Color = Color;
			if (!Settings.MusicMuted)
				if (XML?.Attribute("Song")?.Value is string songName && !string.IsNullOrWhiteSpace(songName)
					&& Assets.AudioT.Songs.TryGetValue(songName, out AudioT.Song song))
				{
					if (SoundBlaster.Song != song)
						SoundBlaster.Song = song;
				}
				else if (Assets.XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("Song")?.Value is string defaultSongName && !string.IsNullOrWhiteSpace(defaultSongName)
					&& Assets.AudioT.Songs.TryGetValue(defaultSongName, out AudioT.Song defaultSong))
				{
					if (SoundBlaster.Song != defaultSong)
						SoundBlaster.Song = defaultSong;
				}
			return this;
		}
		public void FinishedFadeIn()
		{
			if (Counters.Count > 0)
				Counters.First().Started = true;
		}
	}
}
