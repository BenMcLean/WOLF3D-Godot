using Godot;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class PushWall : FourWalls, ISpeaker, ISavable
	{
		public const float Seconds = 128f / 70f; // It takes 128 tics for a pushwall to fully open in Wolfenstein 3-D.
		public const float HalfSeconds = Seconds / 2f;
		#region Data
		public XElement XML { get; set; } = null;
		public int ArrayIndex { get; set; }
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
		public override XElement Save()
		{
			XElement e = base.Save(); // FourWalls
			e.Name = XName.Get(GetType().Name);
			e.SetAttributeValue(XName.Get("Time"), Time);
			e.SetAttributeValue(XName.Get("Halfway"), Halfway);
			e.SetAttributeValue(XName.Get("Pushed"), Pushed);
			e.SetAttributeValue(XName.Get("Direction"), Direction.ToString());
			if (RepeatDigiSound is float)
			{
				e.SetAttributeValue(XName.Get("RepeatDigiSound"), RepeatDigiSound);
				e.SetAttributeValue(XName.Get("SinceRepeatDigiSound"), SinceRepeatDigiSound);
			}
			e.SetAttributeValue(XName.Get("XML"), XML.ToString());
			return e;
		}
		#endregion Data
		public PushWall(XElement xml)
		{
			Name = "Pushwall";
			XML = xml.Attribute("XML")?.Value is string a ? XElement.Parse(a) : xml;
			Set(
				(ushort)(uint)xml.Attribute("Page"),
				ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page")
				);
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
					Level.SetPushWallAt((ushort)(X + Direction.X), (ushort)(Z + Direction.Z));
					X = (ushort)(X + Direction.X * 2);
					Z = (ushort)(Z + Direction.Z * 2);
					Level.SetPushWallAt(X, Z, this);
					GlobalTransform = new Transform(Basis.Identity, new Vector3(
							X * Assets.WallWidth,
							0f,
							Z * Assets.WallWidth
						));
					// Check for a "secret wall" tile on the destination square and reset the pushwall to its initial state on the new square if present. This should allow chaining secrets through the same wall: a technique supported in the original engine but only used in fan-made maps AFAIK.
					foreach (XElement pushXML in Assets.PushWall ?? Enumerable.Empty<XElement>())
						if (ushort.TryParse(pushXML?.Attribute("Number")?.Value, out ushort pushNumber) && Level.Map.GetObjectData(X, Z) == pushNumber)
						{
							Time = 0f;
							Halfway = false;
							Pushed = false;
							break;
						}
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
		#region ISpeaker
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
		#endregion ISpeaker
	}
}
