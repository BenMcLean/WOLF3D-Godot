using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DGame.Model;

namespace WOLF3DGame
{
    public class Door : StaticBody
    {
        public const float OpeningSeconds = 64f / 70f; // It takes 64 tics to open a door in Wolfenstein 3-D.
        public const float OpenSeconds = 300f / 70f; // Doors stay open for 300 tics before checking if time to close in Wolfenstein 3-D.
        public float Progress
        {
            get => progress;
            set
            {
                progress = value;
                if (Moving) DoorCollider.Transform = new Transform(Basis.Identity, new Vector3(progress / OpeningSeconds / Assets.WallWidth, 0f, 0f));
            }
        }
        private float progress = 0f;
        public enum StateEnum { CLOSED, OPENING, OPEN, CLOSING }
        public StateEnum State
        {
            get => state;
            set
            {
                switch (value)
                {
                    case StateEnum.CLOSED:
                        Progress = 0f;
                        break;
                    case StateEnum.OPEN:
                        Progress = OpeningSeconds;
                        break;
                }
                state = value;
            }
        }
        private StateEnum state = StateEnum.CLOSED;
        public bool Moving => State == StateEnum.OPENING || State == StateEnum.CLOSING;
        public bool Western { get; private set; } = true;
        public int X { get; private set; } = 0;
        public int Z { get; private set; } = 0;
        public CollisionShape DoorCollider { get; private set; }
        public CollisionShape PlusCollider { get; private set; }
        public CollisionShape MinusCollider { get; private set; }

        public Door(Material material, int x, int z, bool western)
        {
            GD.Print("Creating " + (western ? "western" : "southern") + " door at " + x + ", " + z + "!");
            X = x;
            Z = z;
            Western = western;
            GlobalTransform = new Transform(
                    Western ? Direction8.EAST.Basis : Direction8.NORTH.Basis,
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
            DoorCollider.AddChild(new MeshInstance()
            {
                Name = (Western ? "West" : "South") + " door mesh instance at [" + x + ", " + z + "]",
                MaterialOverride = material,
                Mesh = Assets.WallMesh,
            });
        }

        public static Door[][] Doors(GameMap map)
        {
            XElement door;
            Door[][] doors = new Door[map.Width][];
            for (ushort x = 0; x < map.Width; x++)
            {
                doors[x] = new Door[map.Depth];
                for (ushort z = 0; z < map.Depth; z++)
                    if ((door = (from e in Game.Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Door") ?? Enumerable.Empty<XElement>()
                                 where ushort.TryParse(e.Attribute("Number")?.Value, out ushort number) && number == map.GetMapData(x, z)
                                 select e).FirstOrDefault()) != null)
                        doors[x][z] = new Door(
                            Game.Assets.VSwapMaterials[(uint)door.Attribute("Page")],
                            x,
                            z,
                            Direction8.From(door.Attribute("Direction")) == Direction8.WEST
                            );
            }
            return doors;
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            switch (State)
            {
                case StateEnum.OPENING:
                    Progress += delta;
                    if (Progress > OpeningSeconds)
                        State = StateEnum.OPEN;
                    break;
                case StateEnum.CLOSING:
                    Progress -= delta;
                    if (Progress < 0)
                        State = StateEnum.CLOSED;
                    break;
                case StateEnum.OPEN:
                    Progress += delta;
                    if (Progress > OpenSeconds)
                    {
                        Progress = 0;
                        //TryClose();
                    }
                    break;
            }
        }
    }
}
