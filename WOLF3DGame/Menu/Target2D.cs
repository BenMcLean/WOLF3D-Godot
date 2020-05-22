using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class Target2D : Node2D, ITarget
    {
        public bool Target(Vector2 vector2) => TargetLocal(vector2 - Position);
        public bool Target(float x, float y) => TargetLocal(x - Position.x, y - Position.y);
        public bool TargetLocal(Vector2 vector2) => TargetLocal(vector2.x, vector2.y);
        public bool TargetLocal(float x, float y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public float Width { get; set; } = 0;
        public float Height { get; set; } = 0;
        public XElement XML { get; set; } = null;
    }
}
