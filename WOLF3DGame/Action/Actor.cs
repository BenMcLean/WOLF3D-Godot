using Godot;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Actor : Billboard
    {
        public XElement ActorXML;
        public int ArrayIndex { get; set; }

        public Actor(XElement spawn) : base()
        {
            XML = spawn;
            Name = XML?.Attribute("Actor")?.Value;
            if (!string.IsNullOrWhiteSpace(Name))
            {
                CollisionShape.Name = "Collision " + Name;
                ActorXML = Assets.XML.Element("VSwap")?.Element("Objects").Elements("Actor").Where(e => e.Attribute("Name")?.Value?.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase) ?? false).FirstOrDefault();
                if (ushort.TryParse(ActorXML?.Attribute("Speed")?.Value, out ushort speed))
                    ActorSpeed = speed;
            }
            Direction = Direction8.From(XML?.Attribute("Direction")?.Value);
            State = Assets.States[XML?.Attribute("State")?.Value];
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (!Main.Room.Paused)
            {
                if (Main.ActionRoom.Level.GetActorAt(TileX, TileZ) == this)
                    Main.ActionRoom.Level.SetActorAt(TileX, TileZ);

                Seconds += delta;
                if (Seconds > State.Seconds)
                    State = State.Next;

                if (NewState)
                {
                    State?.Act?.Invoke(this, delta);
                    NewState = false;
                }

                State?.Think?.Invoke(this, delta);
                if (MeshInstance.Visible && State != null
                    && State.Shape is short shape
                    && (ushort)(shape + (State.Rotate ?
                    Direction8.Modulus(
                        Direction8.AngleToPoint(
                            GlobalTransform.origin.x,
                            GlobalTransform.origin.z,
                            GetViewport().GetCamera().GlobalTransform.origin.x,
                            GetViewport().GetCamera().GlobalTransform.origin.z
                        ).MirrorZ + (Direction ?? 0),
                        8)
                    : 0)) is ushort newFrame
                    && newFrame != Page)
                    Page = newFrame;

                if (State.Mark)
                    Main.ActionRoom.Level.SetActorAt(TileX, TileZ, this);
            }
        }

        public ushort? FloorCode => Main.ActionRoom.Level.Walls.IsNavigable(X, Z)
            && Main.ActionRoom.Level.Walls.Map.GetMapData((ushort)X, (ushort)Z) is ushort floorCode
            && floorCode >= Assets.FloorCodeFirst
            && floorCode < Assets.FloorCodeFirst + Assets.FloorCodes ?
            (ushort)(floorCode - Assets.FloorCodeFirst)
            : (ushort?)null;

        #region objstruct
        //typedef struct objstruct
        //{
        //    activetype active;
        /*
        typedef enum {
            ac_badobject = -1,
            ac_no,
            ac_yes,
            ac_allways
        }
        activetype;
        */
        public short Tics
        {
            get => Assets.SecondsToTics(Seconds);
            set => Seconds = Assets.TicsToSeconds(value);
        }
        public float Seconds { get; set; } = 0f;
        public State State
        {
            get => state;
            set
            {
                state = value;
                Seconds = 0f;
                NewState = true;
            }
        }
        private State state;
        public bool NewState = false;
        //    byte flags;                //    FL_SHOOTABLE, etc
        //#define FL_SHOOTABLE	1
        public bool Shootable = false;
        //#define FL_BONUS		2
        //#define FL_NEVERMARK	4
        public bool NeverMark = false;
        //#define FL_VISABLE		8
        //#define FL_ATTACKMODE	16
        public bool AttackMode = false;
        //#define FL_FIRSTATTACK	32
        public bool FirstAttack = false;
        //#define FL_AMBUSH		64
        public bool Ambush = false;
        //#define FL_NONMARK		128
        //    long distance;            // if negative, wait for that door to open
        public float Distance { get; set; } = Assets.WallWidth;
        //    dirtype dir;
        public Direction8 Direction { get; set; } = Direction8.SOUTH;
        //    fixed x, y;
        //    unsigned tilex, tiley;
        ushort TileX { get; set; } = 0;
        ushort TileZ { get; set; } = 0;
        //    byte areanumber;
        //    int viewx;
        //    unsigned viewheight;
        //    fixed transx, transy;        // in global coord

        //    int angle;
        //    int hitpoints;
        public ushort HitPoints = 0;
        //    long speed;
        public uint ActorSpeed
        {
            get => (uint)(Speed / Assets.ActorSpeedConversion);
            set => Speed = value * Assets.ActorSpeedConversion;
        }
        public float Speed = 0f;

        //    int temp1, temp2, temp3;
        //    struct objstruct    *next,*prev;
        //}
        //objtype;
        #endregion objstruct

        #region StateDelegates
        public static void T_Stand(Actor actor, float delta = 0f) => actor.T_Stand(delta);
        public Actor T_Stand(float delta = 0f)
        {
            return this;
        }
        public static void T_Path(Actor actor, float delta = 0f) => actor.T_Path(delta);
        public Actor T_Path(float delta = 0f)
        {
            // TODO: Check if player is sighted.
            if (Direction == null)
            {
                SelectPathDir();
                if (Direction == null)
                    return this; // All movement is blocked
            }
            float move = Speed * delta;
            Vector3 newPosition = GlobalTransform.origin + Assets.Vector3(Direction + move);
            if (!Main.ActionRoom.ARVRPlayer.IsWithin(newPosition.x, newPosition.z, Assets.HalfWallWidth))
            {
                GlobalTransform = new Transform(GlobalTransform.basis, newPosition);
                Distance -= move;
            }
            if (Distance <= 0f)
            {
                Recenter();
                SelectPathDir();
                if (Direction == null)
                    return this; // All movement is blocked
                else if (Main.ActionRoom.Level.GetDoor(X + Direction.X, Z + Direction.Z) is Door door)
                    door.ActorPush();
            }
            return this;
        }
        public static void T_Chase(Actor actor, float delta = 0f) => actor.T_Chase(delta);
        public Actor T_Chase(float delta = 0f)
        {
            return this;
        }
        public static void T_Shoot(Actor actor, float delta = 0f) => actor.T_Shoot(delta);
        public Actor T_Shoot(float delta = 0f)
        {
            return this;
        }
        #endregion StateDelegates

        public Actor SelectPathDir()
        {
            if (Main.ActionRoom.Map.WithinMap(X, Z)
                && Assets.Turns.TryGetValue(Main.ActionRoom.Map.GetObjectData((ushort)X, (ushort)Z), out Direction8 direction))
                Direction = direction;
            if (Direction != null && Main.ActionRoom.Level.CanWalk(X + Direction.X, Z + Direction.Z)
                && !(Main.ActionRoom.ARVRPlayer.X == X + Direction.X && Main.ActionRoom.ARVRPlayer.Z == Z + Direction.Z))
            {
                Distance = Assets.WallWidth;
                TileX = (ushort)(X + Direction.X);
                TileZ = (ushort)(Z + Direction.Z);
            }
            else
            {
                TileX = (ushort)X;
                TileZ = (ushort)Z;
            }
            return this;
        }

        public Actor Recenter()
        {
            GlobalTransform = new Transform(GlobalTransform.basis, new Vector3(Assets.CenterSquare(X), 0f, Assets.CenterSquare(Z)));
            return this;
        }

        public Actor Kill()
        {
            if (Assets.States.TryGetValue(ActorXML?.Attribute("Death")?.Value, out State deathState))
                State = deathState;
            return this;
        }
    }
}
