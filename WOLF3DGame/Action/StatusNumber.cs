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
            if (XML?.Attribute("Name")?.Value is string name && !string.IsNullOrWhiteSpace(name))
                Name = name;
            if (uint.TryParse(XML?.Attribute("Init")?.Value, out uint init))
                Value = init;
            Position = new Vector2(
                float.TryParse(XML?.Attribute("X")?.Value, out float x) ? x : 0,
                float.TryParse(XML?.Attribute("Y")?.Value, out float y) ? y : 0
                );
        }

        public StatusNumber(uint digits = 0)
        {
            Name = "StatusNumber";
            if (digits > 0)
            {
                Digits = new Sprite[digits];
                for (uint i = 0; i < digits; i++)
                    AddChild(Digits[i] = new Sprite()
                    {
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
            get => val;
            set
            {
                val = uint.TryParse(XML?.Attribute("Max")?.Value, out uint max) && value > max ?
                    max
                    : value;
                string s = value.ToString();
                for (int i = 0; i < (Digits?.Length ?? 0); i++)
                    Digits[i].Texture = i >= s.Length ?
                        Assets.StatusBarBlank
                        : Assets.StatusBarDigits[uint.Parse(s[s.Length - 1 - i].ToString())];
            }
        }
        private uint val = 0;

        public Sprite[] Digits { get; set; }
    }
}
