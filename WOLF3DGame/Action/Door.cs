using Godot;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Door : Pushable, ISpeaker
    {
        public const float OpeningSeconds = 64f / 70f; // It takes 64 tics to open a door in Wolfenstein 3-D.
        public const float OpenSeconds = 300f / 70f; // Doors stay open for 300 tics before checking if time to close in Wolfenstein 3-D.
        public XElement XML
        {
            get => xml;
            set
            {
                xml = value;
                if (Assets.DigiSoundSafe(xml?.Attribute("DigiSound")?.Value) is AudioStreamSample open)
                    OpenDigiSound = open;
                if (Assets.DigiSoundSafe(xml?.Attribute("CloseDigiSound")?.Value) is AudioStreamSample close)
                    CloseDigiSound = close;
            }
        }
        private XElement xml = null;
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
                        Play = OpenDigiSound;
                        break;
                    case DoorEnum.OPEN:
                        Slide = OpeningSeconds;
                        Progress = 0;
                        break;
                    case DoorEnum.CLOSING:
                        Play = CloseDigiSound;
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
                GatesEnabled = state == DoorEnum.CLOSED;
            }
        }
        private DoorEnum state = DoorEnum.CLOSED;
        public bool Moving => State == DoorEnum.OPENING || State == DoorEnum.CLOSING;
        public bool Western { get; private set; } = true;
        public Direction8 Direction => Western ? Direction8.WEST : Direction8.SOUTH;
        public ushort X { get; private set; } = 0;
        public ushort Z { get; private set; } = 0;
        public CollisionShape DoorCollider { get; private set; }
        public MeshInstance DoorMesh { get; private set; }
        public AudioStreamPlayer3D Speaker { get; private set; }
        public CollisionShape PlusGate { get; private set; }
        public CollisionShape MinusGate { get; private set; }
        public ushort? FloorCodePlus { get; set; } = 0;
        public ushort? FloorCodeMinus { get; set; } = 0;
        public Level Level { get; set; } = null;
        public bool IsOpen => State == DoorEnum.OPEN;

        public Door(Material material, ushort x, ushort z, bool western, Level level) : this(material, x, z, western) => Level = level;

        public Door(Material material, ushort x, ushort z, bool western)
        {
            X = x;
            Z = z;
            Western = western;
            Name = (Western ? "West" : "South") + " door at [" + x + ", " + z + "]";
            GlobalTransform = new Transform(
                    Western ? Direction8.NORTH.Basis : Direction8.EAST.Basis,
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
                MaterialOverride = material,
                Mesh = Assets.WallMesh,
            });
            DoorCollider.AddChild(Speaker = new AudioStreamPlayer3D()
            {
                Transform = new Transform(Basis.Identity, new Vector3(-Assets.HalfWallWidth, 0f, 0f)),
            });
            AddChild(PlusGate = new CollisionShape()
            {
                Name = (Western ? "West" : "South") + " +gate shape at [" + x + ", " + z + "]",
                Shape = Assets.WallShape,
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, Assets.HalfWallWidth)),
            });
            AddChild(MinusGate = new CollisionShape()
            {
                Name = (Western ? "West" : "South") + " -zgate shape at [" + x + ", " + z + "]",
                Shape = Assets.WallShape,
                Transform = new Transform(Basis.Identity, new Vector3(0f, 0f, -Assets.HalfWallWidth)),
            });
            Size = new Vector2(Assets.WallWidth, Assets.WallWidth);
        }

        public bool GatesEnabled
        {
            get => !PlusGate.Disabled || !MinusGate.Disabled;
            set => PlusGate.Disabled = MinusGate.Disabled = !value;
        }

        public static Door[][] Doors(GameMap map, Level level = null)
        {
            XElement door;
            Door[][] doors = new Door[map.Width][];
            for (ushort x = 0; x < map.Width; x++)
            {
                doors[x] = new Door[map.Depth];
                for (ushort z = 0; z < map.Depth; z++)
                    if ((door = (from e in Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door") ?? Enumerable.Empty<XElement>()
                                 where ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == map.GetMapData(x, z)
                                 select e).FirstOrDefault()) != null)
                        doors[x][z] = level == null ?
                            new Door(
                                Assets.VSwapMaterials[(uint)door.Attribute("Page")],
                                x,
                                z,
                                Direction8.From(door.Attribute("Direction")) == Direction8.WEST
                            )
                            {
                                XML = door,
                            }.SetFloorCodes(map)
                            : new Door(
                                Assets.VSwapMaterials[(uint)door.Attribute("Page")],
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

        public DoorEnum Pushed => PushedState(State);

        public override bool Push(Direction8 direction)
        {
            if (Main.StatusBar.Conditional(XML))
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

        public AudioStreamSample Play
        {
            get => (AudioStreamSample)Speaker.Stream;
            set
            {
                Speaker.Stream = !Settings.DigiSoundMuted
                    && (!(FloorCodePlus is ushort plus
                    && FloorCodeMinus is ushort minus
                    && Main.ActionRoom.ARVRPlayer.FloorCode is ushort floorCode)
                    || (floorCode == plus || floorCode == minus || Level.FloorCodes.FloorCodes(plus, minus).Contains(floorCode))) ?
                    value
                    : null;
                if (value != null)
                    Speaker.Play();
            }
        }

        public AudioStreamSample OpenDigiSound { get; set; } = null;
        public AudioStreamSample CloseDigiSound { get; set; } = null;
    }
}
