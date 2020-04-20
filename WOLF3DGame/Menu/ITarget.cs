using Godot;

namespace WOLF3D.WOLF3DGame.Menu
{
    public interface ITarget
    {
        bool Target(Vector2 vector2);
        bool Target(float x, float y);
        bool TargetLocal(Vector2 vector2);
        bool TargetLocal(float x, float y);
    }
}
