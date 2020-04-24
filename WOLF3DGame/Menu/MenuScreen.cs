using Godot;
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
                if (modal != null)
                    AddChild(modal);
            }
        }
        private Modal modal = null;
        public Color Color
        {
            get => Background.Color;
            set => Main.Color = Background.Color = value;
        }
        public Color TextColor { get; set; }
        public Color SelectedColor { get; set; }
        public Color DisabledColor { get; set; }
        public ImageTexture[] Cursors { get; set; }
        public int CursorX { get; set; } = 0;
        public int CursorY { get; set; } = 0;
        public Sprite Cursor { get; set; }
        public ImageTexture[] Difficulties { get; set; }
        public Sprite Difficulty { get; set; }

        public MenuScreen()
        {
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
            XML = menu;
            Font = Assets.Font(uint.TryParse(menu.Attribute("Font")?.Value, out uint result) ? result : 0);
            Color = Assets.Palette[(uint)menu.Attribute("BkgdColor")];
            if (menu.Attribute("TextColor") != null)
                TextColor = Assets.Palette[(uint)menu.Attribute("TextColor")];
            if (menu.Attribute("SelectedColor") != null)
                SelectedColor = Assets.Palette[(uint)menu.Attribute("SelectedColor")];
            if (menu.Attribute("DisabledColor") != null)
                DisabledColor = Assets.Palette[(uint)menu.Attribute("DisabledColor")];
            foreach (XElement pixelRect in menu.Elements("PixelRect"))
                if (Main.InGameMatch(pixelRect))
                    AddChild(new PixelRect(pixelRect));
            foreach (XElement image in menu.Elements("Image"))
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
                    AddChild(new Sprite()
                    {
                        Texture = texture,
                        Position = new Vector2((float)image.Attribute("X") + texture.GetSize().x / 2f, (float)image.Attribute("Y") + texture.GetSize().y / 2f),
                    });
                }
            foreach (XElement text in menu.Elements("Text"))
                if (Main.InGameMatch(text))
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
                            (uint.TryParse(text.Attribute("X")?.Value, out uint x) ? x : 0) + texture.GetWidth() / 2,
                            (uint.TryParse(text.Attribute("Y")?.Value, out uint y) ? y : 0) + texture.GetHeight() / 2
                            ),
                        Modulate = uint.TryParse(text.Attribute("Color")?.Value, out uint color) ? Assets.Palette[color] : TextColor,
                    });
                }
            foreach (XElement menuItems in menu.Elements("MenuItems") ?? Enumerable.Empty<XElement>())
                if (Main.InGameMatch(menuItems))
                    foreach (MenuItem item in MenuItem.MenuItems(menuItems, Font, TextColor))
                    {
                        MenuItems.Add(item);
                        AddChild(item);
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
            Selection = 0;
            AddChild(Crosshairs);
        }

        public List<MenuItem> MenuItems { get; private set; } = new List<MenuItem>();

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
                    return;
                }
                MenuItems[selection].Color = TextColor;
                selection = Direction8.Modulus(value, MenuItems.Count);
                MenuItems[selection].Color = SelectedColor;
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

        public bool Target(Vector2 vector2) => TargetLocal(vector2);
        public bool Target(float x, float y) => TargetLocal(x, y);
        public bool TargetLocal(float x, float y) => TargetLocal(new Vector2(x, y));
        public bool TargetLocal(Vector2 vector2)
        {
            Crosshairs.Position = vector2;
            if (Modal == null && MenuItems != null)
                for (int x = 0; x < MenuItems.Count; x++)
                    if (Selection != x && MenuItems[x].Target(vector2))
                    {
                        Selection = x;
                        return true;
                    }
            Modal?.Target(vector2);
            return false;
        }

        public override void _Process(float delta)
        {
            Blink += delta;
            while (Blink > BlinkRate)
            {
                Blink -= BlinkRate;
                CursorSprite++;
            }
        }

        public void DoInput(InputEvent @event)
        {
            if (Modal != null && MenuItems != null)
                if (@event.IsActionPressed("ui_down"))
                    Selection++;
                else if (@event.IsActionPressed("ui_up"))
                    Selection--;
            if (@event.IsActionPressed("ui_accept"))
                Accept();
            else if (@event.IsActionPressed("ui_cancel"))
                Cancel();
        }

        public MenuScreen Accept()
        {
            if (Modal == null && SelectedItem is MenuItem selected && selected != null)
            {
                if (selected.XML.Attribute("SelectSound") is XAttribute selectSound && selectSound != null && !string.IsNullOrWhiteSpace(selectSound.Value))
                    SoundBlaster.Adl = Assets.Sound(selectSound.Value);
                else if (Assets.SelectSound != null)
                    SoundBlaster.Adl = Assets.SelectSound;
                Main.MenuRoom.Action(selected.XML);
                return this;
            }
            if (Modal != null)
                Modal = null;
            return this;
        }

        public MenuScreen Cancel()
        {
            if (Modal != null)
                Accept();
            else
                foreach (XElement cancel in XML.Elements("Cancel") ?? Enumerable.Empty<XElement>())
                    if (Main.InGameMatch(cancel))
                    {
                        Main.MenuRoom.Action(cancel);
                        break; // Only need one cancel action
                    }
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

        public void ButtonPressed(MenuRoom menuRoom, int buttonIndex, bool right = false)
        {
            if (!Room.IsVRButton(buttonIndex))
                return;
            ARVRController controller = right ? menuRoom.RightController : menuRoom.LeftController;
            if (controller != menuRoom.ActiveController)
            {
                menuRoom.ActiveController = controller;
                return;
            }
            switch (buttonIndex)
            {
                case (int)JoystickList.VrTrigger:
                    Accept();
                    break;
                case (int)JoystickList.OculusBy:
                    Cancel();
                    break;
            }
        }
    }
}
