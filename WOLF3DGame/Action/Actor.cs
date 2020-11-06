using Godot;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Actor : Billboard, ISpeaker
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
                Ambush = ActorXML.IsTrue("Ambush");
            }
            Direction = Direction8.From(XML?.Attribute("Direction")?.Value);
            AddChild(Speaker = new AudioStreamPlayer3D()
            {
                Transform = new Transform(Basis.Identity, new Vector3(0f, Assets.HalfWallHeight, 0f)),
            });
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
                    NewState = false;
                    if (!Settings.DigiSoundMuted
                        && State?.XML?.Attribute("DigiSound")?.Value is string digiSound
                        && Assets.DigiSoundSafe(digiSound) is AudioStreamSample audioStreamSample)
                        Play = audioStreamSample;
                    State?.Act?.Invoke(this, delta); // Act methods are called once per state
                }

                State?.Think?.Invoke(this, delta); // Think methods are called once per frame -- NOT per tic!
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

                // START DEBUGGING
                /*
                if (!State.Alive && SightPlayer())
                {
                    if (!Settings.DigiSoundMuted
    && ActorXML?.Attribute("DigiSound")?.Value is string digiSound
    && Assets.DigiSoundSafe(digiSound) is AudioStreamSample audioStreamSample)
                        Play = audioStreamSample;
                    if (Assets.States.TryGetValue(ActorXML?.Attribute("Chase")?.Value, out State chase))
                        State = chase;
                }
                */
                // END DEBUGGING

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

        public float GetReaction() => GetReaction(ActorXML);

        public static float GetReaction(XElement xElement) => xElement?.Attribute("Reaction")?.Value is string reaction ? GetReaction(reaction) : 0f;

        public static float GetReaction(string reaction)
        {
            if (string.IsNullOrWhiteSpace(reaction))
                return 0f;
            if (reaction.Split(',') is string[] reactions && reactions.Length > 1)
                reaction = reactions.Random();
            return Assets.TicsToSeconds(reaction.Split('-') is string[] values
                && values.Length == 2
                && uint.TryParse(values[0], out uint min)
                && uint.TryParse(values[1], out uint max) ?
                Main.RNG.Next((int)min, (int)max)
                : uint.TryParse(reaction, out uint value) ?
                (int)value
                : 0);
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
                Speaker.Transform = new Transform(Basis.Identity, new Vector3(0f, state.SpeakerHeight, 0f));
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
        public uint ActorSpeed => State.ActorSpeed;
        public float Speed => State.Speed;

        //    int temp1, temp2, temp3;
        public float ReactionTime = 0f;
        public float ReactionTimer = 0f;

        //    struct objstruct    *next,*prev;
        //}
        //objtype;
        #endregion objstruct

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
        #endregion ISpeaker

        #region StateDelegates
        public static void T_Stand(Actor actor, float delta = 0f) => actor.T_Stand(delta);
        public Actor T_Stand(float delta = 0f)
        {
            CheckChase(delta);
            return this;
        }
        public static void T_Path(Actor actor, float delta = 0f) => actor.T_Path(delta);
        public Actor T_Path(float delta = 0f)
        {
            if (CheckChase(delta))
                return this;

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
            //long move;
            //int dx, dy, dist, chance;

            // TODO: return if gamestate.victoryflag

            //bool dodge = false;

            //if (CheckLine(ob))  // got a shot at player?
            //{
            //    dx = abs(ob->tilex - player->tilex);
            //    dy = abs(ob->tiley - player->tiley);
            //    dist = dx > dy ? dx : dy;
            //    if (!dist || (dist == 1 && ob->distance < 0x4000))
            //        chance = 300;
            //    else
            //        chance = (tics << 4) / dist;

            //    if (US_RndT() < chance)
            //    {
            //        //
            //        // go into attack frame
            //        //

            //        return;
            //    }
            //    dodge = true;
            //}

            //if (ob->dir == nodir)
            //{
            //    if (dodge)
            //        SelectDodgeDir(ob);
            //    else
            //        SelectChaseDir(ob);
            //    if (ob->dir == nodir)
            //        return;                         // object is blocked in
            //}

            //move = ob->speed * tics;

            //while (move)
            //{
            //    if (ob->distance < 0)
            //    {
            //        //
            //        // waiting for a door to open
            //        //
            //        OpenDoor(-ob->distance - 1);
            //        if (doorobjlist[-ob->distance - 1].action != dr_open)
            //            return;
            //        ob->distance = TILEGLOBAL;  // go ahead, the door is now opoen
            //    }

            //    if (move < ob->distance)
            //    {
            //        MoveObj(ob, move);
            //        break;
            //    }

            //    //
            //    // reached goal tile, so select another one
            //    //

            //    //
            //    // fix position to account for round off during moving
            //    //
            //    ob->x = ((long)ob->tilex << TILESHIFT) + TILEGLOBAL / 2;
            //    ob->y = ((long)ob->tiley << TILESHIFT) + TILEGLOBAL / 2;

            //    move -= ob->distance;

            //    if (dodge)
            //        SelectDodgeDir(ob);
            //    else
            //        SelectChaseDir(ob);

            //    if (ob->dir == nodir)
            //        return;                         // object is blocked in
            //}

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
            if (State.Alive && Assets.States.TryGetValue(ActorXML?.Attribute("Death")?.Value, out State deathState))
                State = deathState;
            return this;
        }

        public bool CheckChase(float delta = 0f)
        {
            if (SightPlayer(delta))
            {
                if (!Settings.DigiSoundMuted
                    && ActorXML?.Attribute("DigiSound")?.Value is string digiSound
                    && Assets.DigiSoundSafe(digiSound) is AudioStreamSample audioStreamSample)
                    Play = audioStreamSample;
                if (Assets.States.TryGetValue(ActorXML?.Attribute("Chase")?.Value, out State chase))
                    State = chase;
                return true;
            }
            return false;
        }

        public const float MinSight = 2f / 3f * Assets.WallWidth;

        public bool SightPlayer(float delta = 0f)
        {
            // I've decided to omit checking for "An actor in ATTACKMODE called SightPlayer!"
            ReactionTime += delta;
            if (ReactionTime > ReactionTimer)
            {
                ReactionTime = 0f;
                ReactionTimer = GetReaction();
            }
            else return false;
            return CheckSight();
        }

        public bool CheckSight()
        {
            // don't bother tracing a line if the area isn't connected to the player's
            if (FloorCode is ushort floorCode
                && Main.ActionRoom.ARVRPlayer.FloorCode is ushort playerFloorCode
                && floorCode != playerFloorCode
                && Main.ActionRoom.Level.FloorCodes[floorCode, playerFloorCode] < 1)
                return false;
            // if the player is real close, sight is automatic
            if (Mathf.Abs(Transform.origin.x - Main.ActionRoom.ARVRPlayer.Transform.origin.x) < MinSight
                && Mathf.Abs(Transform.origin.z - Main.ActionRoom.ARVRPlayer.Transform.origin.z) < MinSight)
                return true;
            // see if they are looking in the right direction
            if (!Direction.InSight(Transform.origin, Main.ActionRoom.ARVRPlayer.Transform.origin))
                return false;
            return CheckLine();
        }

        /// <summary>
        /// trace a line to check for blocking tiles (corners)
        /// </summary>
        /// <returns>true if there are no blocking tiles</returns>
        public bool CheckLine()
        {
            float x = Transform.origin.x / Assets.WallWidth,
                z = Transform.origin.z / Assets.WallWidth,
                playerX = Main.ActionRoom.ARVRPlayer.Transform.origin.x / Assets.WallWidth,
                playerZ = Main.ActionRoom.ARVRPlayer.Transform.origin.z / Assets.WallWidth,
                distance = Mathf.Sqrt((x - playerX) * (x - playerX) + (z - playerZ) * (z - playerZ)) * 256f,
                dx = (playerX - x) / distance,
                dz = (playerZ - z) / distance;
            int tempX = Mathf.FloorToInt(x),
                tempZ = Mathf.FloorToInt(z);
            for (int i = 0; i <= distance; i++)
            {
                x += dx;
                z += dz;
                if ((Mathf.FloorToInt(x) != tempX || Mathf.FloorToInt(z) != tempZ)
                    && !Main.ActionRoom.Level.IsTransparent(
                        tempX = Mathf.FloorToInt(x),
                        tempZ = Mathf.FloorToInt(z)
                        )
                    )
                    return false;
            }
            return true;
        }
    }
}
