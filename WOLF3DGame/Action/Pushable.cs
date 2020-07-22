using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
    public abstract class Pushable : StaticBody
    {
        public abstract bool Push();
        public virtual bool Inside(Vector3 vector3) => Inside(vector3.x, vector3.z);
        public virtual bool Inside(Vector2 vector2) => Inside(vector2.x, vector2.y);
        public virtual bool Inside(float x, float z) =>
            Mathf.Abs(GlobalTransform.origin.x + Assets.HalfWallWidth - x) < Assets.HalfWallWidth &&
            Mathf.Abs(GlobalTransform.origin.z + Assets.HalfWallWidth - z) < Assets.HalfWallWidth;
    }
}
