using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class Counter : Label
    {
        public XElement XML { get; set; } = null;
        public uint Digits { get; set; } = 3;
        public uint FinalValue { get; set; } = 0;
        public float SinceLast { get; set; } = 0f;
        public float Interval { get; set; } = 0.1f;
        public bool Started
        {
            get => started;
            set
            {
                started = value;
                if (started)
                {
                    SinceLast = 0f;
                    Value = 0;
                }
            }
        }
        private bool started = false;
        public bool Finished { get; private set; } = false;

        public Counter(XElement xml)
        {
            XML = xml;
            RectPosition = new Vector2(
                float.TryParse(XML?.Attribute("X")?.Value, out float x) ? x : 0,
                float.TryParse(XML?.Attribute("Y")?.Value, out float y) ? y : 0
                );
            Name = XML?.Attribute("Name")?.Value is string name && !string.IsNullOrWhiteSpace(name) ? name : "Counter";
            if (uint.TryParse(xml?.Attribute("BitmapFont")?.Value, out uint bitmapFont))
                AddFontOverride("font", Assets.BitmapFonts[bitmapFont]);
            if (uint.TryParse(xml?.Attribute("Digits")?.Value, out uint digits))
                Digits = digits;
            //if (uint.TryParse(XML?.Attribute("Init")?.Value, out uint init))
            //    Value = init;
            FinalValue = 100;
            Visible = !XML.IsFalse("Visible");
        }

        public uint? Value
        {
            get => val;
            set
            {
                uint? old = val;
                val = value;
                if (val != old)
                    Text = val is uint v ?
                        string.Format("{0," + Digits.ToString() + ":" + new string('#', (int)Digits) + "}", v)
                        : "";
            }
        }
        private uint? val = null;

        public override void _Process(float delta)
        {
            if (Started && !Finished)
            {
                SinceLast += delta;
                while (Value < FinalValue && SinceLast > Interval)
                {
                    SinceLast -= Interval;
                    Value++;
                }
                if (Value >= FinalValue)
                {
                    Value = FinalValue;
                    Finished = true;
                }
            }
        }
    }
}
