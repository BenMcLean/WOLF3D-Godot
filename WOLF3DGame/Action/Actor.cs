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
                Seconds += delta;
                if (Seconds > State.Seconds)
                {
                    Seconds -= State.Seconds;
                    State = State.Next;
                    State?.Act?.Invoke(this, delta);
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
            }
        }

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
        //    int ticcount;
        public short Tics
        {
            get => Assets.SecondsToTics(Seconds);
            set => Seconds = Assets.TicsToSeconds(value);
        }
        public float Seconds { get; set; } = 0f;
        //    classtype obclass;
        public string ObjClass;
        //    statetype* state;
        public State State { get; set; } = null;
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
        public bool NoMark = false;
        //    long distance;            // if negative, wait for that door to open
        public float Distance { get; set; } = Assets.WallWidth;
        //    dirtype dir;
        public Direction8 Direction { get; set; } = Direction8.SOUTH;
        //    fixed x, y;
        //    unsigned tilex, tiley;
        ushort TileX { get; set; } = 0;
        ushort TileY { get; set; } = 0;
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
        public static void T_Stand(Actor actor, float delta = 0) => actor.T_Stand(delta);
        public Actor T_Stand(float delta)
        {
            return this;
        }
        public static void T_Path(Actor actor, float delta = 0) => actor.T_Path(delta);
        public Actor T_Path(float delta = 0)
        {
            // TODO: Check if player is sighted.
            if (Direction == null)
            {
                SelectPathDir();
                if (Direction == null)
                    return this; // All movement is blocked
            }
            float move = Speed * delta;
            // TODO: Wait for a door to open.
            GlobalTransform = new Transform(GlobalTransform.basis, GlobalTransform.origin + Assets.Vector3(Direction + move));
            Distance -= move;
            if (Distance < 0)
            {
                Recenter();
                Main.ActionRoom.Level.TryOpen((ushort)(X - Direction.X), (ushort)(Z - Direction.Z));
                SelectPathDir();
                if (Direction == null)
                    return this; // All movement is blocked
            }
            return this;
        }
        public static void T_Chase(Actor actor, float delta = 0) => actor.T_Chase(delta);
        public Actor T_Chase(float delta = 0)
        {
            return this;
        }
        public static void T_Shoot(Actor actor, float delta = 0) => actor.T_Shoot(delta);
        public Actor T_Shoot(float delta = 0)
        {
            return this;
        }
        #endregion StateDelegates

        public Actor SelectPathDir()
        {
            if (Main.ActionRoom.Map.WithinMap(X, Z)
                && Assets.Turns.TryGetValue(Main.ActionRoom.Map.GetObjectData((ushort)X, (ushort)Z), out Direction8 direction))
                Direction = direction;
            Distance = Assets.WallWidth;
            if (Direction != null &&
                (!Main.ActionRoom.Map.WithinMap(X + Direction.X, Z + Direction.Z)
                || !Main.ActionRoom.Level.IsOpen((ushort)(X + Direction.X), (ushort)(Z + Direction.Z))))
                Direction = null;
            return this;
        }

        public Actor Recenter()
        {
            GlobalTransform = new Transform(GlobalTransform.basis, new Vector3(Assets.CenterSquare(X), 0, Assets.CenterSquare(Z)));
            return this;
        }
    }
}
