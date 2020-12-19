using Godot;
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
        public VgaGraph.Font Font { get; set; }
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
            set => Main.Color = Background.Color = value;
        }
        public Color TextColor { get; set; }
        public Color SelectedColor { get; set; }
        public ImageTexture[] Cursors { get; set; }
        public int CursorX { get; set; } = 0;
        public int CursorY { get; set; } = 0;
        public Sprite Cursor { get; set; }
        public ImageTexture[] Difficulties { get; set; }
        public Sprite Difficulty { get; set; }

        public MenuScreen()
        {
            Name = "MenuScreen";
            Size = new Vector2(Width, Height);
            Disable3d = true;
            RenderTargetClearMode = ClearMode.OnlyNextFrame;
            RenderTargetVFlip = true;
            AddChild(Background = new ColorRect()
            {
                Color = Color.Color8(0, 0, 0, 255),
                RectSize = Size,
            });
        }

        public MenuScreen(XElement menu) : this()
        {
            Name = menu.Attribute("Name")?.Value is string name ? name : "MenuScreen";
            XML = menu;
            Font = Assets.Font(uint.TryParse(menu.Attribute("Font")?.Value, out uint result) ? result : 0);
            if (byte.TryParse(menu.Attribute("BkgdColor")?.Value, out byte bkgdColor))
                Color = Assets.Palettes[0][bkgdColor];
            TextColor = byte.TryParse(menu.Attribute("TextColor")?.Value, out byte tColor) ? Assets.Palettes[0][tColor] : Assets.White;
            SelectedColor = byte.TryParse(menu.Attribute("SelectedColor")?.Value, out byte sColor) ? Assets.Palettes[0][sColor] : Assets.White;
            foreach (XElement pixelRect in menu.Elements("PixelRect") ?? Enumerable.Empty<XElement>())
                if (Main.InGameMatch(pixelRect))
                    AddChild(new PixelRect(pixelRect));
            foreach (XElement image in menu.Elements("Image") ?? Enumerable.Empty<XElement>())
                if (Main.InGameMatch(image))
                {
                    ImageTexture texture = Assets.PicTexture(image.Attribute("Name").Value);
                    if (image.Attribute("XBanner") != null)
                        AddChild(new Sprite()
                        {
                            Texture = texture,
                            RegionEnabled = true,
                            RegionRect = new Rect2(
                                new Vector2(
                                    uint.TryParse(image.Attribute("XBanner")?.Value, out uint x) ? x : 0,
                                    0f
                                    ),
                                new Vector2(1, texture.GetSize().y)
                                ),
                            Position = new Vector2(Width, texture.GetSize().y / 2f +
                                (float.TryParse(image.Attribute("Y")?.Value, out float y) ? y : 0)
                            ),
                            Scale = new Vector2(Width, 1f),
                        });
                    Vector2 position = new Vector2(
                            image.Attribute("X")?.Value?.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
                            Width / 2
                            : (float)image.Attribute("X") + texture.GetWidth() / 2f,
                            image.Attribute("Y")?.Value?.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
                            Height / 2
                            : (float)image.Attribute("Y") + texture.GetHeight() / 2f
                            );
                    AddChild(new Sprite()
                    {
                        Name = "Image " + image.Attribute("Name").Value,
                        Texture = texture,
                        Position = position,
                    });
                    if (image.Attribute("Action")?.Value is string action && !string.IsNullOrWhiteSpace(action))
                    {
                        Target2D target = new Target2D()
                        {
                            Name = "Target " + image.Attribute("Name").Value,
                            XML = image,
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
            foreach (XElement text in menu.Elements("Text").Where(e => Main.InGameMatch(e)) ?? Enumerable.Empty<XElement>())
                if (ushort.TryParse(text.Attribute("BitmapFont")?.Value, out ushort fontNumber))
                {
                    Label label = new Label()
                    {
                        Text = text.Attribute("String").Value,
                        RectPosition = new Vector2(
                            uint.TryParse(text.Attribute("X")?.Value, out uint x) ? x : 0,
                            uint.TryParse(text.Attribute("Y")?.Value, out uint y) ? y : 0
                            ),
                    };
                    label.AddFontOverride("font", Assets.BitmapFonts[fontNumber]);
                    AddChild(label);
                }
                else
                {
                    ImageTexture texture = Assets.Text(
                        uint.TryParse(text.Attribute("Font")?.Value, out uint font) ? Assets.Font(font) : Font,
                        text.Attribute("String").Value,
                        ushort.TryParse(text.Attribute("Padding")?.Value, out ushort padding) ? padding : (ushort)0
                        );
                    AddChild(new Sprite()
                    {
                        Texture = texture,
                        Position = new Vector2(
                                        text.Attribute("X")?.Value?.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
                                        Width / 2
                                        : ((uint.TryParse(text.Attribute("X")?.Value, out uint x) ?
                                        x
                                        : 0) + texture.GetWidth() / 2),
                                        text.Attribute("Y")?.Value?.Equals("center", StringComparison.InvariantCultureIgnoreCase) ?? false ?
                                        Height / 2
                                        : ((uint.TryParse(text.Attribute("Y")?.Value, out uint y) ?
                                        y
                                        : 0) + texture.GetHeight() / 2)
                                        ),
                        Modulate = uint.TryParse(text.Attribute("Color")?.Value, out uint color) ? Assets.Palettes[0][color] : TextColor,
                    });
                }
            foreach (XElement menuItems in menu.Elements("MenuItems") ?? Enumerable.Empty<XElement>())
                if (Main.InGameMatch(menuItems))
                    foreach (MenuItem item in MenuItem.MenuItems(menuItems, Font, TextColor, SelectedColor))
                    {
                        MenuItems.Add(item);
                        AddChild(item);
                    }
            foreach (XElement xCounter in menu.Elements("Counter") ?? Enumerable.Empty<XElement>())
            {
                Counter counter = new Counter(xCounter);
                AddChild(counter);
            }
            if (menu.Element("Cursor") is XElement cursor && cursor != null && Main.InGameMatch(cursor))
            {
                List<ImageTexture> cursors = new List<ImageTexture>();
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
                    AddChild(Cursor = new Sprite()
                    {
                        Texture = Cursors[0],
                        Position = new Vector2(MenuItems[0].Position.x + Cursors[0].GetWidth() / 2, MenuItems[0].Position.y + Cursors[0].GetHeight() / 2),
                    });
            }
            if (menu.Element("Difficulty") is XElement difficulty && difficulty != null && Main.InGameMatch(difficulty))
            {
                ImageTexture texture = Assets.PicTexture(difficulty.Attribute("Difficulty1").Value);
                AddChild(Difficulty = new Sprite()
                {
                    Position = new Vector2(
                        (uint.TryParse(difficulty.Attribute("X")?.Value, out uint x) ? x : 0) + texture.GetWidth() / 2,
                        (uint.TryParse(difficulty.Attribute("Y")?.Value, out uint y) ? y : 0) + texture.GetHeight() / 2
                        ),
                });
            }
            if (Main.InGame && menu.Element("StatusBar") is XElement statusBar)
            {
                ViewportTexture texture = Main.StatusBar.GetTexture();
                AddChild(new Sprite()
                {
                    Texture = texture,
                    Position = new Vector2(
                        (uint.TryParse(statusBar.Attribute("X")?.Value, out uint x) ? x : 0) + texture.GetWidth() / 2,
                        (uint.TryParse(statusBar.Attribute("Y")?.Value, out uint y) ? y : 0) + texture.GetHeight() / 2
                        ),
                });
            }
            Selection = uint.TryParse(XML.Attribute("Default")?.Value, out uint selection) ? (int)selection : 0;
            AddChild(Crosshairs);
        }

        public List<MenuItem> MenuItems { get; private set; } = new List<MenuItem>();
        public List<Target2D> ExtraItems { get; private set; } = new List<Target2D>();

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
                    Cursor.Position = new Vector2(
                        MenuItems[selection].Position.x + Cursor.Texture.GetWidth() / 2 + CursorX,
                        MenuItems[selection].Position.y + Cursor.Texture.GetHeight() / 2 + CursorY
                        );
                if (Difficulty != null && XML.Element("Difficulty") is XElement difficulty && difficulty != null)
                    Difficulty.Texture = Assets.PicTexture(difficulty.Attribute("Difficulty" + (SelectedItem.XML.Attribute("Difficulty")?.Value ?? ""))?.Value);
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
            Modal = new Modal(new Sprite()
            {
                Texture = Assets.Text(Assets.ModalFont, @string, padding),
            })
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
    }
}
