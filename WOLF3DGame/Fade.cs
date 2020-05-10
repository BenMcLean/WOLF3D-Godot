using Godot;

namespace WOLF3D.WOLF3DGame
{
    public class Fade : CenterContainer
    {
        public override void _Ready()
        {
            Name = "Fade";
            SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            SizeFlagsVertical = (int)SizeFlags.ExpandFill;
            GrowHorizontal = GrowDirection.Both;
            GrowVertical = GrowDirection.Both;
            RectSize = new Vector2(float.MaxValue, float.MaxValue);
            RectMinSize = new Vector2(float.MaxValue, float.MaxValue);
            ColorRect = new ColorRect()
            {
                Name = "Fade",
                Color = Color.Color8(0, 0, 255, 64),
                SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
                SizeFlagsVertical = (int)SizeFlags.ExpandFill,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                RectSize = new Vector2(float.MaxValue, float.MaxValue),
                RectMinSize = new Vector2(float.MaxValue, float.MaxValue),
            };
        }

        public ColorRect ColorRect
        {
            get => colorRect;
            set
            {
                if (colorRect != null)
                    RemoveChild(colorRect);
                colorRect = value;
                if (colorRect != null)
                    AddChild(colorRect);
            }
        }
        private ColorRect colorRect = null;

        public Color Color
        {
            get => ColorRect.Color;
            set => ColorRect.Color = value;
        }
    }
}
