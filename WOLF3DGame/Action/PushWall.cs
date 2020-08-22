using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class PushWall : FourWalls
    {
        public const float Seconds = 128f / 70f; // It takes 128 tics for a pushwall to fully open in Wolfenstein 3-D.
        public const float HalfSeconds = Seconds / 2f;

        public XElement XML { get; set; } = null;
        public PushWall(XElement xml) : base(
            (ushort)(uint)xml.Attribute("Page"),
            ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page")
            )
        {
            XML = xml;
            AddChild(Speaker = new AudioStreamPlayer3D()
            {
                Transform = new Transform(Basis.Identity, new Vector3(Assets.HalfWallWidth, Assets.HalfWallHeight, Assets.HalfWallWidth)),
            });
        }

        public override bool Push(Direction8 direction)
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
            if (!Main.Room.Paused && Pushed == true && Time < Seconds)
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
    }
}
