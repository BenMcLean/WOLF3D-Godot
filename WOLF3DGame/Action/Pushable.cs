using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
    public abstract class Pushable : StaticBody
    {
        public virtual bool Push() => Push(Direction8.CardinalToPoint(
    Main.ActionRoom.ARVRPlayer.GlobalTransform.origin,
    GlobalTransform.origin + new Vector3(Assets.HalfWallWidth, 0, Assets.HalfWallWidth)
    ));
        public abstract bool Push(Direction8 direction);
        public virtual bool Inside(Vector3 vector3) => Inside(vector3.x, vector3.z);
        public virtual bool Inside(Vector2 vector2) => Inside(vector2.x, vector2.y);
        public virtual bool Inside(float x, float z) =>
            Mathf.Abs(GlobalTransform.origin.x + Assets.HalfWallWidth - x) < Assets.HalfWallWidth &&
            Mathf.Abs(GlobalTransform.origin.z + Assets.HalfWallWidth - z) < Assets.HalfWallWidth;
    }
}
