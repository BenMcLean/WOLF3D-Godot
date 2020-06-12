using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class PushWall : StaticBody
    {
        public XElement XML { get; set; } = null;
        public PushWall(XElement xml) : this(
            (ushort)(uint)xml.Attribute("Page"),
            ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page")
            )
            => XML = xml;

        public PushWall(ushort wall, ushort darkSide)
        {
            Name = "Pushwall";
            AddChild(Walls.BuildWall(wall, false, 0, 0, true));
            AddChild(Walls.BuildWall(wall, false, 1, 0));
            AddChild(Walls.BuildWall(darkSide, true, 0, 0));
            AddChild(Walls.BuildWall(darkSide, true, 0, -1, true));
        }

        public bool Push() => Push(Direction8.CardinalToPoint(
            Main.ActionRoom.ARVRPlayer.GlobalTransform.origin,
            GlobalTransform.origin + new Vector3(Assets.HalfWallWidth, 0, Assets.HalfWallWidth)
            ));

        public bool Push(Direction8 direction)
        {
            GlobalTransform = new Transform(GlobalTransform.basis, GlobalTransform.origin + direction.Vector3 * Assets.WallWidth * 2);
            return Pushed = true;
        }

        public uint X { get; set; } = 0;
        public const uint Y = 0; // Wolfenstein 3-D isn't vertical.
        public uint Z { get; set; } = 0;
        public bool Pushed { get; set; } = false;
        public Level Level { get; set; } = null;
        public Direction8 Direction { get; set; }

        public bool Inside(Vector3 vector3) => Inside(vector3.x, vector3.z);
        public bool Inside(Vector2 vector2) => Inside(vector2.x, vector2.y);
        public bool Inside(float x, float y) =>
            Mathf.Abs(GlobalTransform.origin.x + Assets.HalfWallWidth - x) < Assets.HalfWallWidth &&
            Mathf.Abs(GlobalTransform.origin.z + Assets.HalfWallWidth - y) < Assets.HalfWallWidth;
    }
}
