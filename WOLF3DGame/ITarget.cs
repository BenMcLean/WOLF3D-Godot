using Godot;

namespace WOLF3D.WOLF3DGame
{
    public interface ITarget
    {
        bool IsIn(Vector3 vector3);
        bool IsIn(float x, float y, float z);
        bool IsInLocal(Vector3 vector3);
        bool IsInLocal(float x, float y, float z);
        bool IsIn(Vector2 vector2);
        bool IsIn(float x, float y);
        bool IsInLocal(Vector2 vector2);
        bool IsInLocal(float x, float y);
        Vector2 Position { get; set; }
        Vector2 GlobalPosition { get; set; }
        Vector2 Size { get; set; }
        Vector2 Offset { get; set; }
    }
}
