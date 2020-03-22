using Godot;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Door : StaticBody
    {
        public const float OpeningSeconds = 64f / 70f; // It takes 64 tics to open a door in Wolfenstein 3-D.
        public const float OpenSeconds = 300f / 70f; // Doors stay open for 300 tics before checking if time to close in Wolfenstein 3-D.
        public float Progress { get; set; }
        public float Slide
        {
            get => DoorCollider.Transform.origin.x * OpeningSeconds / Assets.WallWidth;
            set => DoorCollider.Transform = new Transform(Basis.Identity, new Vector3(value / OpeningSeconds * Assets.WallWidth, 0f, 0f));
        }

        public enum DoorEnum { CLOSED, OPENING, OPEN, CLOSING }
        public static DoorEnum NextState(DoorEnum s) =>
            s == DoorEnum.CLOSED ? DoorEnum.OPENING
            : s == DoorEnum.OPENING ? DoorEnum.OPEN
            : s == DoorEnum.OPEN ? DoorEnum.CLOSING
            : DoorEnum.CLOSED;
        public static DoorEnum PushedState(DoorEnum s) =>
            s == DoorEnum.CLOSED ? DoorEnum.OPENING
            : s == DoorEnum.OPENING ? DoorEnum.CLOSING
            : s == DoorEnum.OPEN ? DoorEnum.CLOSING
            : DoorEnum.OPENING;
        public DoorEnum State
        {
            get => state;
            set
            {
                if (State == DoorEnum.OPEN && value != DoorEnum.OPEN && !TryClose())
                    return;
                switch (value)
                {
                    case DoorEnum.CLOSED:
                        Slide = Progress = 0f;
                        break;
                    case DoorEnum.OPENING:
                        Play = OpeningSound;
                        break;
                    case DoorEnum.OPEN:
                        Slide = OpeningSeconds;
                        Progress = 0;
                        Open = true;
                        break;
                    case DoorEnum.CLOSING:
                        Play = ClosingSound;
                        if (State == DoorEnum.OPEN || Progress > OpeningSeconds)
                            Slide = Progress = OpeningSeconds;
                        break;
                }
                state = value;
                GatesEnabled = state == DoorEnum.CLOSED;
            }
        }
        private DoorEnum state = DoorEnum.CLOSED;
        public bool Moving => State == DoorEnum.OPENING || State == DoorEnum.CLOSING;
        public bool Western { get; private set; } = true;
        public ushort X { get; private set; } = 0;
        public ushort Z { get; private set; } = 0;
        public CollisionShape DoorCollider { get; private set; }
        public MeshInstance DoorMesh { get; private set; }
        public AudioStreamPlayer3D Doorknob { get; private set; }
        public CollisionShape PlusGate { get; private set; }
        public CollisionShape MinusGate { get; private set; }

        public delegate bool TryOpenDeelgate(ushort x, ushort z, bool @bool);
        public TryOpenDeelgate TryOpen { get; set; }
        public bool TryClose() => TryOpen?.Invoke(X, Z, false) ?? false;

        public delegate bool IsOpenDelegate(ushort x, ushort z);
        public IsOpenDelegate IsOpen { get; set; }
        public bool Open
        {
            get => IsOpen(X, Z);
            set => TryOpen?.Invoke(X, Z, value);
        }

        public Door SetDelegates(Level level)
        {
            TryOpen = level.TryOpen;
            IsOpen = level.IsOpen;
            return this;
        }

        public Door(Material material, ushort x, ushort z, bool western, Level level) : this(material, x, z, western) => SetDelegates(level);

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
                        (float)Assets.HalfWallHeight,
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
            DoorCollider.AddChild(Doorknob = new AudioStreamPlayer3D()
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
                            : new Door(
                                Assets.VSwapMaterials[(uint)door.Attribute("Page")],
                                x,
                                z,
                                Direction8.From(door.Attribute("Direction")) == Direction8.WEST,
                                level
                            );
            }
            return doors;
        }

        public override void _Ready()
        {
            State = TryClose() ? DoorEnum.CLOSED : DoorEnum.OPEN;
        }

        public override void _PhysicsProcess(float delta)
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
                        if (TryClose())
                            State = DoorEnum.CLOSING;
                        else
                            Progress = 0;
                    break;
            }
        }

        public DoorEnum Pushed => PushedState(State);

        public bool Push()
        {
            State = Pushed;
            return true;
        }

        public AudioStreamSample Play
        {
            get => (AudioStreamSample)Doorknob.Stream;
            set
            {
                Doorknob.Stream = value;
                if (value != null)
                    Doorknob.Play();
            }
        }

        public static AudioStreamSample OpeningSound { get; set; } = null;
        public static AudioStreamSample ClosingSound { get; set; } = null;
    }
}
