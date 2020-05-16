using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
    public class StatusNumber : Node2D
    {
        public StatusNumber(uint digits = 1)
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

        public StatusNumber Blank()
        {
            for (int i = 0; i < Digits.Length; i++)
                Digits[i].Texture = Assets.StatusBarBlank;
            return this;
        }

        public uint Value
        {
            get => val;
            set
            {
                val = value;
                string s = value.ToString();
                for (int i = 0; i < Digits.Length; i++)
                    Digits[i].Texture = i > s.Length ?
                        Assets.StatusBarBlank
                        : Assets.StatusBarDigits[uint.Parse(s[s.Length - 1 - i].ToString())];
            }
        }
        private uint val = 0;

        public Sprite[] Digits { get; set; }
    }
}
