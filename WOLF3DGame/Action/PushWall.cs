using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class PushWall : FourWalls, ISpeaker
    {
        public const float Seconds = 128f / 70f; // It takes 128 tics for a pushwall to fully open in Wolfenstein 3-D.
        public const float HalfSeconds = Seconds / 2f;

        public XElement XML { get; set; } = null;
        public int ArrayIndex { get; set; }
        public PushWall(XElement xml) : base(
            (ushort)(uint)xml.Attribute("Page"),
            ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page")
            )
        {
            Name = "Pushwall";
            XML = xml;
            AddChild(Speaker = new AudioStreamPlayer3D()
            {
                Name = "Pushwall speaker",
                Transform = new Transform(Basis.Identity, new Vector3(Assets.HalfWallWidth, Assets.HalfWallHeight, Assets.HalfWallWidth)),
                Bus = "3D",
            });
        }

        public override bool Push(Direction8 direction)
        {
            if (Pushed
                || !PushWallOpen(X + direction.X, Z + direction.Z)
                || !PushWallOpen(X + direction.X * 2, Z + direction.Z * 2))
                return false;
            Direction = direction;
            Level.SetPushWallAt((ushort)(X + direction.X), (ushort)(Z + direction.Z), this);
            Level.SetPushWallAt((ushort)(X + direction.X * 2), (ushort)(Z + direction.Z * 2), this);
            return Pushed = true;
        }

        public bool PushWallOpen(int x, int z) => Level.Walls.IsNavigable(x, z) && !Level.IsPushWallAt((ushort)x, (ushort)z);

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
                    Level.SetPushWallAt((ushort)(X + Direction.X), (ushort)(Z + Direction.Z));
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
                        Level.SetPushWallAt(X, Z);
                        Halfway = true;
                    }
                }
                if (!Settings.DigiSoundMuted && RepeatDigiSound is float repeat)
                {
                    SinceRepeatDigiSound += delta;
                    while (SinceRepeatDigiSound >= repeat)
                    {
                        Play = Sound;
                        SinceRepeatDigiSound -= repeat;
                    }
                }
            }
        }

        public float? RepeatDigiSound = null;
        public float SinceRepeatDigiSound = 0f;
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
