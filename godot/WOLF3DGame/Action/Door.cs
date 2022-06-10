using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Door : Pushable, ISpeaker, ISavable
	{
		public const float OpeningSeconds = 64f / Assets.TicsPerSecond; // It takes 64 tics to open a door in Wolfenstein 3-D.
		public const float OpenSeconds = 300f / Assets.TicsPerSecond; // Doors stay open for 300 tics before checking if time to close in Wolfenstein 3-D.
		#region Data
		public XElement XML { get; set; }
		public float Progress { get; set; } = 0;
		public float Slide
		{
			get => DoorCollider.Transform.origin.x * OpeningSeconds / Assets.WallWidth;
			set => DoorCollider.Transform = new Transform(Basis.Identity, new Vector3(value / OpeningSeconds * Assets.WallWidth, 0f, 0f));
		}
		public enum DoorEnum { CLOSED, OPENING, OPEN, CLOSING }
		public DoorEnum NextState() => NextState(State);
		public static DoorEnum NextState(DoorEnum s) =>
			s == DoorEnum.CLOSED ? DoorEnum.OPENING
			: s == DoorEnum.OPENING ? DoorEnum.OPEN
			: s == DoorEnum.OPEN ? DoorEnum.CLOSING
			: DoorEnum.CLOSED;
		public DoorEnum PushedState() => PushedState(State);
		public static DoorEnum PushedState(DoorEnum s) =>
			s == DoorEnum.CLOSED ? DoorEnum.OPENING
			: s == DoorEnum.OPENING ? DoorEnum.CLOSING
			: s == DoorEnum.OPEN ? DoorEnum.CLOSING
			: DoorEnum.OPENING;
		public DoorEnum ActorPushed() => ActorPushed(State);
		public static DoorEnum ActorPushed(DoorEnum s) =>
			s == DoorEnum.CLOSED ? DoorEnum.OPENING
			: s == DoorEnum.CLOSING ? DoorEnum.OPENING
			: s;
		public DoorEnum State
		{
			get => state;
			set
			{
				if (State == DoorEnum.OPEN && value != DoorEnum.OPEN && !Level.CanCloseDoor(X, Z))
					return;
				switch (value)
				{
					case DoorEnum.CLOSED:
						Slide = Progress = 0f;
						break;
					case DoorEnum.OPENING:
						if (!Settings.DigiSoundMuted && Assets.DigiSoundSafe(XML?.Attribute("OpenDigiSound")?.Value) is AudioStreamSample openDigiSound)
							Play = openDigiSound;
						break;
					case DoorEnum.OPEN:
						Slide = OpeningSeconds;
						Progress = 0;
						break;
					case DoorEnum.CLOSING:
						if (!Settings.DigiSoundMuted && Assets.DigiSoundSafe(XML?.Attribute("CloseDigiSound")?.Value) is AudioStreamSample closeDigiSound)
							Play = closeDigiSound;
						if (State == DoorEnum.OPEN || Progress > OpeningSeconds)
							Slide = Progress = OpeningSeconds;
						break;
				}
				DoorEnum old = state;
				state = value;
				if (Level != null && FloorCodePlus is ushort plus && FloorCodeMinus is ushort minus && plus != minus)
					if (old == DoorEnum.CLOSED && state == DoorEnum.OPENING)
						Level.FloorCodes[plus, minus]++;
					else if (old == DoorEnum.CLOSING && state == DoorEnum.CLOSED)
						Level.FloorCodes[plus, minus]--;
				CollisionEnabled = state == DoorEnum.CLOSED;
			}
		}
		private DoorEnum state = DoorEnum.CLOSED;
		public bool Moving => State == DoorEnum.OPENING || State == DoorEnum.CLOSING;
		public bool Western { get; private set; } = true;
		public Direction8 Direction
		{
			get => Western ? Direction8.WEST : Direction8.SOUTH;
			private set => Western = value.X < 0;
		}
		public ushort X { get; private set; } = 0;
		public ushort Z { get; private set; } = 0;
		public ushort Page
		{
			get => page;
			set => DoorMesh.MaterialOverride = Assets.VSwapMaterials[page = value];
		}
		private ushort page;
		public CollisionShape DoorCollider { get; private set; }
		public MeshInstance DoorMesh { get; private set; }
		public CollisionShape CollisionShape { get; private set; }
		public ushort? FloorCodePlus { get; set; } = 0;
		public ushort? FloorCodeMinus { get; set; } = 0;
		public Level Level { get; set; } = null;
		public XElement Save()
		{
			XElement e = new XElement(XName.Get(GetType().Name));
			e.SetAttributeValue(XName.Get("X"), X);
			e.SetAttributeValue(XName.Get("Z"), Z);
			e.SetAttributeValue(XName.Get("Direction"), Direction.ToString());
			e.SetAttributeValue(XName.Get("Page"), Page);
			e.SetAttributeValue(XName.Get("State"), Enum.GetName(typeof(DoorEnum), State));
			e.SetAttributeValue(XName.Get("Progress"), Progress);
			e.SetAttributeValue(XName.Get("Slide"), Slide);
			e.SetAttributeValue(XName.Get("FloorCodePlus"), FloorCodePlus);
			e.SetAttributeValue(XName.Get("FloorCodeMinus"), FloorCodeMinus);
			e.SetAttributeValue(XName.Get("XML"), XML.ToString());
			return e;
		}
		#endregion Data
		public bool IsOpen => State == DoorEnum.OPEN;
		public bool IsOpening => State == DoorEnum.OPENING;
		public bool IsClosed => State == DoorEnum.CLOSED;
		public bool IsClosing => State == DoorEnum.CLOSING;
		public Door(XElement xml, Level level) : this(xml) => Level = level;
		public Door(XElement xml)
		{
			XML = xml.Attribute("XML")?.Value is string a ? XElement.Parse(a) : xml;
			Set(
				(ushort)(uint)xml.Attribute("Page"),
				(ushort)(uint)xml.Attribute("X"),
				(ushort)(uint)xml.Attribute("Z"),
				Direction8.From(xml.Attribute("Direction")) == Direction8.WEST
				);
			State = (DoorEnum)Enum.Parse(typeof(DoorEnum), xml.Attribute("State").Value, true);
			if (float.TryParse(xml.Attribute("Progress")?.Value, out float progress))
				Progress = progress;
			if (float.TryParse(xml.Attribute("Slide")?.Value, out float slide))
				Slide = slide;
			if (ushort.TryParse(xml.Attribute("FloorCodePlus")?.Value, out ushort floorCodePlus))
				FloorCodePlus = floorCodePlus;
			if (ushort.TryParse(xml.Attribute("FloorCodeMinus")?.Value, out ushort floorCodeMinus))
				FloorCodeMinus = floorCodeMinus;
		}
		public Door(ushort page, ushort x, ushort z, bool western, Level level) : this(page, x, z, western) => Level = level;
		public Door(ushort page, ushort x, ushort z, bool western) => Set(page, x, z, western);
		private Door Set(ushort page, ushort x, ushort z, bool western)
		{
			X = x;
			Z = z;
			Western = western;
			Name = (Western ? "West" : "South") + " door at [" + x + ", " + z + "]";
			GlobalTransform = new Transform(
					Western ? Direction8.EAST.Basis : Direction8.NORTH.Basis,
					new Vector3(
						Assets.CenterSquare(x),
						Assets.HalfWallHeight,
						Assets.CenterSquare(z)
					)
				);
			AddChild(DoorCollider = new CollisionShape()
			{
				Name = (Western ? "West" : "South") + " door shape at [" + x + ", " + z + "]",
				Shape = Assets.WallShape,
			});
			DoorCollider.AddChild(DoorMesh = new MeshInstance()
			{
				Name = (Western ? "West" : "South") + " door mesh instance at [" + x + ", " + z + "]",
				Mesh = Assets.WallMesh,
			});
			Page = (ushort)page;
			DoorCollider.AddChild(Speaker = new AudioStreamPlayer3D()
			{
				Name = (Western ? "West" : "South") + " door speaker at [" + x + ", " + z + "]",
				Transform = new Transform(Basis.Identity, new Vector3(-Assets.HalfWallWidth, 0f, 0f)),
				Bus = "3D",
			});
			AddChild(CollisionShape = new CollisionShape()
			{
				Name = (Western ? "West" : "South") + " door CollisionShape at [" + x + ", " + z + "]",
				Shape = Assets.BoxShape,
			});
			Size = new Vector2(Assets.WallWidth, Assets.WallWidth);
			return this;
		}
		public bool CollisionEnabled
		{
			get => !CollisionShape.Disabled;
			set => CollisionShape.Disabled = !value;
		}
		public static Door[][] Doors(GameMap map, Level level = null)
		{
			Door[][] doors = new Door[map.Width][];
			for (ushort x = 0; x < map.Width; x++)
			{
				doors[x] = new Door[map.Depth];
				for (ushort z = 0; z < map.Depth; z++)
					if (Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door")
						?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == map.GetMapData(x, z))
						?.FirstOrDefault() is XElement door)
						doors[x][z] = level == null ?
							new Door(
								(ushort)(uint)door.Attribute("Page"),
								x,
								z,
								Direction8.From(door.Attribute("Direction")) is Direction8 from && (from == Direction8.WEST || from == Direction8.EAST)
							)
							{
								XML = door,
							}.SetFloorCodes(map)
							: new Door(
								(ushort)(uint)door.Attribute("Page"),
								x,
								z,
								Direction8.From(door.Attribute("Direction")) == Direction8.WEST,
								level
							)
							{
								XML = door,
							}.SetFloorCodes(map);
			}
			return doors;
		}
		public Door SetFloorCodes(GameMap map)
		{
			ushort? FloorCode(int floorCode) => floorCode >= 0 && floorCode < Assets.FloorCodes ? (ushort?)floorCode : null;
			FloorCodePlus = FloorCode(map.GetMapData((ushort)(X + Direction.X), (ushort)(Z + Direction.Z)) - Assets.FloorCodeFirst);
			FloorCodeMinus = FloorCode(map.GetMapData((ushort)(X - Direction.X), (ushort)(Z - Direction.Z)) - Assets.FloorCodeFirst);
			return this;
		}
		public override void _PhysicsProcess(float delta)
		{
			if (!Main.Room.Paused)
			{
				switch (State)
				{
					case DoorEnum.OPENING:
						Slide = Progress += delta;
						if (Progress > OpeningSeconds)
							State = DoorEnum.OPEN;
						break;
					case DoorEnum.CLOSING:
						Slide = Progress -= delta;
						if (Progress < 0)
							State = DoorEnum.CLOSED;
						break;
					case DoorEnum.OPEN:
						Progress += delta;
						if (Progress > OpenSeconds)
						{
							State = DoorEnum.CLOSING;
							if (State != DoorEnum.CLOSING)
								Progress = 0;
						}
						break;
				}
			}
		}
		#region IPushable
		public DoorEnum Pushed => PushedState(State);
		public override bool Push(Direction8 direction)
		{
			if (XMLScript.Run(XML, this))
			{
				State = Pushed;
				return true;
			}
			return false;
		}
		public bool ActorPush()
		{
			if (State != ActorPushed())
				State = ActorPushed();
			return true;
		}
		#endregion IPushable
		#region ISpeaker
		public AudioStreamSample Play
		{
			get => (AudioStreamSample)Speaker.Stream;
			set
			{
				if (Settings.DigiSoundMuted || value == null)
				{
					Speaker.Stop();
					Speaker.Stream = null;
					return;
				}
				else
				{
					List<ushort> floorCodes = new List<ushort>();
					if (FloorCodePlus is ushort plus)
						floorCodes.Add(plus);
					if (FloorCodeMinus is ushort minus)
						floorCodes.Add(minus);
					if (Main.ActionRoom.ARVRPlayer.FloorCode is ushort floorCode
						&& (floorCode == FloorCodePlus || floorCode == FloorCodeMinus || Level.FloorCodes.FloorCodes(floorCodes.ToArray()).Contains(floorCode))
						|| Main.ActionRoom.ARVRPlayer.StandingOnOverride)
					{
						Speaker.Stream = value;
						Speaker.Play();
					}
				}
			}
		}
		public AudioStreamPlayer3D Speaker { get; private set; }
		#endregion ISpeaker
	}
}
