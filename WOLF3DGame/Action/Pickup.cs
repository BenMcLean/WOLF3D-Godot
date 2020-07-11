using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Pickup : Billboard
    {
        public Pickup() : base() { }
        public Pickup(XElement xml) : base(xml) { }

        public override void _Process(float delta)
        {
            base._Process(delta); // Billboard
            if (!Main.Room.Paused)
                Main.ActionRoom.Pickup(this);
        }

        public bool IsClose(Vector3 vector3) => IsClose(vector3.x, vector3.z);
        public bool IsClose(Vector2 vector2) => IsClose(vector2.x, vector2.y);
        public bool IsClose(float x, float y) =>
            Mathf.Abs(GlobalTransform.origin.x - x) < Assets.HalfWallWidth &&
            Mathf.Abs(GlobalTransform.origin.z - y) < Assets.HalfWallWidth;
    }
}
