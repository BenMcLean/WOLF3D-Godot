using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class StatusNumber : Node2D
    {
        public XElement XML { get; set; }

        public StatusNumber(XElement xml) : this(
            uint.TryParse(xml?.Attribute("Digits")?.Value, out uint digits) ? digits : 0
            )
        {
            XML = xml;
            Position = new Vector2(
                float.TryParse(XML?.Attribute("X")?.Value, out float x) ? x : 0,
                float.TryParse(XML?.Attribute("Y")?.Value, out float y) ? y : 0
                );
            if (XML?.Attribute("Name")?.Value is string name && !string.IsNullOrWhiteSpace(name))
                Name = name;
            if (XML?.Attribute("Have")?.Value is string have && !string.IsNullOrWhiteSpace(have))
                Have = Assets.PicTexture(have);
            if (XML?.Attribute("Empty")?.Value is string empty && !string.IsNullOrWhiteSpace(empty))
                Empty = Assets.PicTexture(empty);
            if (Empty != null || Have != null)
            {
                ImageTexture size = Empty ?? Have;
                AddChild(Item = new Sprite()
                {
                    Name = "Item",
                    Position = new Vector2(size.GetWidth() / 2, size.GetHeight() / 2),
                });
            }
            if (uint.TryParse(XML?.Attribute("Init")?.Value, out uint init))
                Value = init;
        }

        public Sprite Item { get; set; } = null;
        public ImageTexture Have { get; set; } = null;
        public ImageTexture Empty { get; set; } = null;

        public StatusNumber(uint digits = 0)
        {
            Name = "StatusNumber";
            if (digits > 0)
            {
                Digits = new Sprite[digits];
                for (uint i = 0; i < digits; i++)
                    AddChild(Digits[i] = new Sprite()
                    {
                        Name = "Digit " + i,
                        Texture = Assets.StatusBarBlank,
                        Position = new Vector2(
                            Assets.StatusBarBlank.GetSize().x * (0.5f - i),
                            Assets.StatusBarBlank.GetSize().y / 2
                            ),
                    });
            }
        }

        public StatusNumber Blank()
        {
            for (int i = 0; i < (Digits?.Length ?? 0); i++)
                Digits[i].Texture = Assets.StatusBarBlank;
            return this;
        }

        public uint Value
        {
            get => val ?? 0;
            set
            {
                uint? old = val;
                val = uint.TryParse(XML?.Attribute("Max")?.Value, out uint max) && value > max ?
                    max
                    : value;
                if (val != old)
                {
                    string s = value.ToString();
                    for (int i = 0; i < (Digits?.Length ?? 0); i++)
                        Digits[i].Texture = i >= s.Length ?
                            Assets.StatusBarBlank
                            : Assets.StatusBarDigits[uint.Parse(s[s.Length - 1 - i].ToString())];
                    if (Item != null)
                        Item.Texture = val > 0 ? Have : Empty;
                }
            }
        }
        private uint? val = null;

        public Sprite[] Digits { get; set; }
    }
}
