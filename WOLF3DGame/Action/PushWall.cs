using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class PushWall : StaticBody
    {
        public const float Seconds = 128f / 70f; // It takes 128 tics for a pushwall to fully open in Wolfenstein 3-D.
        public const float HalfSeconds = Seconds / 2f;

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
            AddChild(Speaker = new AudioStreamPlayer3D()
            {
                Transform = new Transform(Basis.Identity, new Vector3(Assets.HalfWallWidth, Assets.HalfWallHeight, Assets.HalfWallWidth)),
            });
        }

        public bool Push() => Push(Direction8.CardinalToPoint(
            Main.ActionRoom.ARVRPlayer.GlobalTransform.origin,
            GlobalTransform.origin + new Vector3(Assets.HalfWallWidth, 0, Assets.HalfWallWidth)
            ));

        public bool Push(Direction8 direction)
        {
            if (Pushed
                || !Level.IsOpen((ushort)(X + direction.X), (ushort)(Z + direction.Z))
                || !Level.IsOpen((ushort)(X + direction.X * 2), (ushort)(Z + direction.Z * 2)))
                return false;
            Direction = direction;
            Level.TryClose((ushort)(X + direction.X), (ushort)(Z + direction.Z));
            Level.TryClose((ushort)(X + direction.X * 2), (ushort)(Z + direction.Z * 2));
            return Pushed = true;
        }

        public override void _Process(float delta)
        {
            if (Pushed == true && Time < Seconds)
            {
                Time += delta;
                if (Time >= Seconds)
                {
                    GlobalTransform = new Transform(Basis.Identity, new Vector3(
                            (X + Direction.X * 2) * Assets.WallWidth,
                            0f,
                            (Z + Direction.Z * 2) * Assets.WallWidth
                        ));
                    Level.TryOpen((ushort)(X + Direction.X), (ushort)(Z + Direction.Z));
                }
                else
                {
                    GlobalTransform = new Transform(Basis.Identity, new Vector3(
                            (X + Direction.X * 2 * Time / Seconds) * Assets.WallWidth,
                            0f,
                            (Z + Direction.Z * 2 * Time / Seconds) * Assets.WallWidth
                        ));
                    if (!Halfway && Time > HalfSeconds)
                    {
                        Level.TryOpen(X, Z);
                        Halfway = true;
                    }
                }
            }
        }

        public ushort X { get; set; } = 0;
        public ushort Z { get; set; } = 0;
        public float Time = 0f;
        public bool Halfway = false;
        public bool Pushed
        {
            get => pushed;
            set
            {
                pushed = value;
                Play = Sound;
            }
        }
        private bool pushed = false;

        public Level Level { get; set; } = null;
        public Direction8 Direction { get; set; }
        public AudioStreamPlayer3D Speaker { get; private set; }
        public AudioStreamSample Play
        {
            get => (AudioStreamSample)Speaker.Stream;
            set
            {
                Speaker.Stream = Settings.DigiSoundMuted ? null : value;
                if (value != null)
                    Speaker.Play();
            }
        }
        public AudioStreamSample Sound { get; set; } = null;

        public bool Inside(Vector3 vector3) => Inside(vector3.x, vector3.z);
        public bool Inside(Vector2 vector2) => Inside(vector2.x, vector2.y);
        public bool Inside(float x, float y) =>
            Mathf.Abs(GlobalTransform.origin.x + Assets.HalfWallWidth - x) < Assets.HalfWallWidth &&
            Mathf.Abs(GlobalTransform.origin.z + Assets.HalfWallWidth - y) < Assets.HalfWallWidth;
    }
}
