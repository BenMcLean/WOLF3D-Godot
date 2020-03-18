using Godot;

namespace WOLF3DGame.Menu
{
    public class Crosshairs : Node2D
    {
        public static readonly Color White = Color.Color8(255, 255, 255, 255);
        public ColorRect West { get; set; } = new ColorRect()
        {
            RectSize = new Vector2(5, 1),
            RectPosition = new Vector2(-6, 0),
            Color = White,
        };
        public ColorRect North { get; set; } = new ColorRect()
        {
            RectSize = new Vector2(1, 4),
            RectPosition = new Vector2(0, -5),
            Color = White,
        };
        public ColorRect East { get; set; } = new ColorRect()
        {
            RectSize = new Vector2(5, 1),
            RectPosition = new Vector2(2, 0),
            Color = White,
        };
        public ColorRect South { get; set; } = new ColorRect()
        {
            RectSize = new Vector2(1, 4),
            RectPosition = new Vector2(0, 2),
            Color = White,
        };
        public Crosshairs()
        {
            AddChild(West);
            AddChild(North);
            AddChild(East);
            AddChild(South);
            //AddChild(new ColorRect()
            //{
            //    RectSize = new Vector2(1, 1),
            //    Color = Color.Color8(0, 0, 255, 255),
            //});
        }
    }
}
