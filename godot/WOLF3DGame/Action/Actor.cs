using Godot;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Actor : Billboard, ISpeaker, ISavable
	{
		#region objstruct
		public XElement ActorXML;
		public int ArrayIndex { get; set; }
		//public MeshInstance DebugFloor;
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
		/// <summary>
		/// ChaseTimer is used to keep the probability of attack independent from the framerate
		/// </summary>
		public float ChaseTimer { get; set; } = 0f;
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
		public ushort TileX { get; set; } = 0;
		public ushort TileZ { get; set; } = 0;
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
		public Actor(XElement xml) : base(xml)
		{
			XML = xml.Attribute("XML")?.Value is string a ? XElement.Parse(a) : xml;
			if (XML?.Attribute("Actor")?.Value is string actorName)
			{
				Name = actorName;
				CollisionShape.Name = "Collision " + actorName;
				ActorXML = Assets.XML.Element("VSwap")?.Element("Objects").Elements("Actor").Where(e => e.Attribute("Name")?.Value?.Equals(actorName, System.StringComparison.InvariantCultureIgnoreCase) ?? false).First();
				Ambush = ActorXML.IsTrue("Ambush");
			}
			Direction = Direction8.From(xml.Attribute("Direction")?.Value);
			AddChild(Speaker = new AudioStreamPlayer3D()
			{
				Name = Name + " speaker",
				Transform = new Transform(Basis.Identity, new Vector3(0f, Assets.HalfWallHeight, 0f)),
				Bus = "3D",
			});
			State = Assets.States[xml.Attribute("State")?.Value];
			if (short.TryParse(xml.Attribute("Tics")?.Value, out short tics))
				Tics = tics;
			if (float.TryParse(xml.Attribute("Seconds")?.Value, out float seconds))
				Seconds = seconds;
			if (float.TryParse(xml.Attribute("ChaseTimer")?.Value, out float chaseTimer))
				ChaseTimer = chaseTimer;
			NewState = xml.IsTrue("NewState");
			NeverMark = xml.IsTrue("NeverMark");
			AttackMode = xml.IsTrue("AttackMode");
			Ambush = xml.IsTrue("Ambush");
			if (float.TryParse(xml.Attribute("Distance")?.Value, out float distance))
				Distance = distance;
			if (ushort.TryParse(xml.Attribute("TileX")?.Value, out ushort tileX))
				TileX = tileX;
			if (ushort.TryParse(xml.Attribute("TileZ")?.Value, out ushort tileZ))
				TileZ = tileZ;
			if (ushort.TryParse(xml.Attribute("HitPoints")?.Value, out ushort hitPoints))
				HitPoints = hitPoints;
			if (float.TryParse(xml.Attribute("ReactionTime")?.Value, out float reactionTime))
				ReactionTime = reactionTime;
			if (float.TryParse(xml.Attribute("ReactionTimer")?.Value, out float reactionTimer))
				ReactionTimer = reactionTimer;
			//AddChild(DebugFloor = new MeshInstance()
			//{
			//	Name = Name + " Debug Floor",
			//	Mesh = new QuadMesh()
			//	{
			//		Size = new Vector2(Assets.WallWidth, Assets.WallWidth),
			//	},
			//	MaterialOverride = new SpatialMaterial()
			//	{
			//		AlbedoColor = Color.Color8(255, 0, 0, 128),
			//		FlagsUnshaded = true,
			//		FlagsDoNotReceiveShadows = true,
			//		FlagsDisableAmbientLight = true,
			//		FlagsTransparent = true,
			//		ParamsCullMode = SpatialMaterial.CullMode.Disabled,
			//		ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
			//		AnisotropyEnabled = true,
			//		RenderPriority = 1,
			//	},
			//	Transform = new Transform(new Basis(Vector3.Right, Mathf.Pi / 2f).Orthonormalized(), new Vector3(0f, Assets.PixelHeight, 0f)),
			//});
		}
		public override XElement Save()
		{
			XElement e = base.Save(); // Billboard
			e.Name = XName.Get(GetType().Name);
			e.SetAttributeValue(XName.Get("Direction"), Direction.ShortName);
			e.SetAttributeValue(XName.Get("Tics"), Tics);
			e.SetAttributeValue(XName.Get("Seconds"), Seconds);
			e.SetAttributeValue(XName.Get("ChaseTimer"), ChaseTimer);
			e.SetAttributeValue(XName.Get("State"), State.Name);
			e.SetAttributeValue(XName.Get("NewState"), NewState);
			e.SetAttributeValue(XName.Get("Shootable"), Shootable);
			e.SetAttributeValue(XName.Get("NeverMark"), NeverMark);
			e.SetAttributeValue(XName.Get("AttackMode"), AttackMode);
			e.SetAttributeValue(XName.Get("FirstAttack"), FirstAttack);
			e.SetAttributeValue(XName.Get("Ambush"), Ambush);
			e.SetAttributeValue(XName.Get("Distance"), Distance);
			e.SetAttributeValue(XName.Get("TileX"), TileX);
			e.SetAttributeValue(XName.Get("TileZ"), TileZ);
			e.SetAttributeValue(XName.Get("HitPoints"), HitPoints);
			e.SetAttributeValue(XName.Get("ReactionTime"), ReactionTime);
			e.SetAttributeValue(XName.Get("ReactionTimer"), ReactionTimer);
			return e;
		}
		public override void _Process(float delta)
		{
			base._Process(delta);
			//DebugFloor.GlobalTransform = new Transform(new Basis(Vector3.Right, Mathf.Pi / 2f).Orthonormalized(), new Vector3((TileX + 0.5f) * Assets.WallWidth, 0f, (TileZ + 0.5f) * Assets.WallWidth));
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
				if (Visible && State != null
					&& State.Shape is short shape
					&& (ushort)(shape + (State.Rotate ?
					Direction8.Modulus(
						Direction8.AngleToPoint(
							GlobalTransform.origin,
							GetViewport().GetCamera().GlobalTransform.origin
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
		public static float GetReaction(XElement xElement) => xElement?.Attribute("Reaction")?.Value is string reaction ? Assets.TicsToSeconds((int)Assets.GetUInt(reaction)) : 0f;
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
			if (Direction == null || Distance <= 0f)
			{
				Recenter();
				if (Main.ActionRoom.Level.Map.WithinMap(X, Z)
					&& Assets.Turns.TryGetValue(Main.ActionRoom.Level.Map.GetObjectData((ushort)X, (ushort)Z), out Direction8 direction))
					Direction = direction;
				if (Direction == null)
					return this; // All movement is blocked
				else if (Direction.IsCardinal && Main.ActionRoom.Level.GetDoor(X + Direction.X, Z + Direction.Z) is Door door && !door.IsOpen)
				{
					door.ActorPush();
					Distance = -1f;
				}
				if (TryWalk()) // && (Main.ActionRoom.ARVRPlayer.X != X + Direction.X || Main.ActionRoom.ARVRPlayer.Z != Z + Direction.Z)) // Prevent trapping the player in the destination square
				{
					TileX = (ushort)(X + Direction.X);
					TileZ = (ushort)(Z + Direction.Z);
					Distance = Assets.WallWidth;
				}
				else
				{
					TileX = (ushort)X;
					TileZ = (ushort)Z;
				}
			}
			if (Direction != null && Distance > 0f)
			{
				float move = Speed * delta;
				Vector3 newPosition = GlobalTransform.origin + Assets.Vector3(Direction + move);
				if (!Main.ActionRoom.ARVRPlayer.IsWithin(newPosition.x, newPosition.z, Assets.HalfWallWidth))
				{
					Transform = new Transform(Transform.basis, newPosition);
					Distance -= move;
				}
			}
			return this;
		}
		public static void T_Chase(Actor actor, float delta = 0f) => actor.T_Chase(delta);
		public Actor T_Chase(float delta = 0f)
		{
			// TODO: return if gamestate.victoryflag
			bool dodge = false;
			if (CheckLine())
			{
				ChaseTimer += delta;
				while (ChaseTimer > Assets.Tic)
				{
					ChaseTimer -= Assets.Tic;
					float dx = Mathf.Abs(Transform.origin.x - Main.ActionRoom.ARVRPlayer.Transform.origin.x),
						dy = Mathf.Abs(Transform.origin.z - Main.ActionRoom.ARVRPlayer.Transform.origin.z);
					int dist = Assets.IntCoordinate(dx > dy ? dx : dy);
					if (dist == 0 || (dist == 1 && Distance < Assets.WallWidth) || Main.US_RndT() < 16 / dist)
					{
						if (Assets.States.TryGetValue(ActorXML?.Attribute("Attack")?.Value, out State attackState))
							State = attackState;
						return this;
					}
				}
				dodge = true;
			}
			if (Direction == null || Distance <= 0f)
			{
				Recenter();
				if (dodge)
					Direction = SelectDodgeDir();
				else
					Direction = SelectChaseDir();
				if (Direction == null)
					return this; // All movement is blocked
				else if (Direction.IsCardinal && Main.ActionRoom.Level.GetDoor(X + Direction.X, Z + Direction.Z) is Door door && !door.IsOpen)
				{
					door.ActorPush();
					TileX = (ushort)X;
					TileZ = (ushort)Z;
					Distance = -1f;
				}
				else if (TryWalk())// && (Main.ActionRoom.ARVRPlayer.X != X + Direction.X || Main.ActionRoom.ARVRPlayer.Z != Z + Direction.Z)) // Prevent trapping the player in the destination square
				{
					TileX = (ushort)(X + Direction.X);
					TileZ = (ushort)(Z + Direction.Z);
					Distance = Assets.WallWidth;
				}
				else
				{
					TileX = (ushort)X;
					TileZ = (ushort)Z;
				}
			}
			if (Direction != null && Distance > 0f)
			{
				float move = Speed * delta;
				Vector3 newPosition = GlobalTransform.origin + Assets.Vector3(Direction + move);
				if (!Main.ActionRoom.ARVRPlayer.IsWithin(newPosition.x, newPosition.z, Assets.HalfWallWidth))
				{
					Transform = new Transform(GlobalTransform.basis, newPosition);
					Distance -= move;
				}
			}
			return this;
		}
		public static void T_DogChase(Actor actor, float delta = 0f) => actor.T_DogChase(delta);
		public Actor T_DogChase(float delta = 0f)
		{ // Just ilke T_Chase but without the ability to open doors.
		  // TODO: return if gamestate.victoryflag
			bool dodge = false;
			if (CheckLine())
			{
				ChaseTimer += delta;
				while (ChaseTimer > Assets.Tic)
				{
					ChaseTimer -= Assets.Tic;
					float dx = Mathf.Abs(Transform.origin.x - Main.ActionRoom.ARVRPlayer.Transform.origin.x),
						dy = Mathf.Abs(Transform.origin.z - Main.ActionRoom.ARVRPlayer.Transform.origin.z);
					if ((dx > dy ? dx : dy) <= Assets.WallWidth)
					//if (Position.DistanceTo(Main.ActionRoom.ARVRPlayer.Position) < Assets.WallWidth * 2)
					{
						if (Assets.States.TryGetValue(ActorXML?.Attribute("Attack")?.Value, out State attackState))
							State = attackState;
						return this;
					}
				}
				dodge = true;
			}
			if (Direction == null || Distance <= 0f)
			{
				Recenter();
				if (dodge)
					Direction = SelectDodgeDir();
				else
					Direction = SelectChaseDir();
				if (Direction == null)
					return this; // All movement is blocked
				else if (Direction.IsCardinal && Main.ActionRoom.Level.GetDoor(X + Direction.X, Z + Direction.Z) is Door door && !door.IsOpen)
				{
					TileX = (ushort)X;
					TileZ = (ushort)Z;
					Distance = 0f;
				}
				else if (TryWalk())// && (Main.ActionRoom.ARVRPlayer.X != X + Direction.X || Main.ActionRoom.ARVRPlayer.Z != Z + Direction.Z)) // Prevent trapping the player in the destination square
				{
					TileX = (ushort)(X + Direction.X);
					TileZ = (ushort)(Z + Direction.Z);
					Distance = Assets.WallWidth;
				}
				else
				{
					TileX = (ushort)X;
					TileZ = (ushort)Z;
				}
			}
			if (Direction != null && Distance > 0f)
			{
				float move = Speed * delta;
				Vector3 newPosition = GlobalTransform.origin + Assets.Vector3(Direction + move);
				if (!Main.ActionRoom.ARVRPlayer.IsWithin(newPosition.x, newPosition.z, Assets.HalfWallWidth))
				{
					GlobalTransform = new Transform(GlobalTransform.basis, newPosition);
					Distance -= move;
				}
			}
			return this;
		}
		public static void T_Shoot(Actor actor, float delta = 0f) => actor.T_Shoot(delta);
		public Actor T_Shoot(float delta = 0f)
		{
			//int	dx,dy,dist;
			//int	hitchance,damage;

			//hitchance = 128;
			//if (!areabyplayer[ob->areanumber])
			//	return;

			//if (!CheckLine (ob))			// player is behind a wall

			//dx = abs(ob->tilex - player->tilex);
			//dy = abs(ob->tiley - player->tiley);
			//dist = dx>dy ? dx:dy;

			//// *** ALPHA RESTORATION ***
			//#if (GAMEVER_WOLFREV > GV_WR_WL920312)
			//if (ob->obclass == ssobj || ob->obclass == bossobj)
			//	dist = dist*2/3;					// ss are better shots
			//#endif

			//if (thrustspeed >= RUNSPEED)
			//{
			//	if (ob->flags&FL_VISABLE)
			//		hitchance = 160-dist*16;		// player can see to dodge
			//	else
			//		hitchance = 160-dist*8;
			//}
			//else
			//{
			//	if (ob->flags&FL_VISABLE)
			//		hitchance = 256-dist*16;		// player can see to dodge
			//	else
			//		hitchance = 256-dist*8;
			//}

			//// see if the shot was a hit

			//if (US_RndT()<hitchance)
			//{
			//	if (dist<2)
			//		damage = US_RndT()>>2;
			//	else if (dist<4)
			//		damage = US_RndT()>>3;
			//	else
			//		damage = US_RndT()>>4;

			//	TakeDamage (damage,ob);
			return this;
		}
		#endregion StateDelegates
		/// <summary>
		/// Attempts to choose and initiate a movement for ob that sends it towards the player while dodging
		/// </summary>
		public Direction8 SelectDodgeDir()
		{
			// TODO
			// 	int 		deltax,deltay,i;
			// 	unsigned	absdx,absdy;
			// 	dirtype 	dirtry[5];
			Direction8[] dirtry = new Direction8[5];
			// 	dirtype 	turnaround,tdir;
			Direction8 turnaround = Direction?.Opposite ?? null;
			// 
			// 	if (ob->flags & FL_FIRSTATTACK)
			// 	{
			// 	//
			// 	// turning around is only ok the very first time after noticing the
			// 	// player
			// 	//
			// 		turnaround = nodir;
			// 		ob->flags &= ~FL_FIRSTATTACK;
			// 	}
			// 	else
			// 		turnaround=opposite[ob->dir];
			// 
			// 	deltax = player->tilex - ob->tilex;
			int deltax = Main.ActionRoom.ARVRPlayer.X - TileX,
				// 	deltay = player->tiley - ob->tiley;
				deltay = Main.ActionRoom.ARVRPlayer.Z - TileZ;
			// 
			// arange 5 direction choices in order of preference
			// the four cardinal directions plus the diagonal straight towards the player
			//
			// 
			// 	if (deltax>0)
			if (deltax > 0)
			// 	{
			{
				// 		dirtry[1]= east;
				dirtry[1] = Direction8.EAST;
				// 		dirtry[3]= west;
				dirtry[3] = Direction8.WEST;
				// 	}
			}
			// 	else
			else
			// 	{
			{
				// 		dirtry[1]= west;
				dirtry[1] = Direction8.WEST;
				// 		dirtry[3]= east;
				dirtry[3] = Direction8.EAST;
				// 	}
			}
			// 
			// 	if (deltay>0)
			if (deltay > 0)
			// 	{
			{
				// 		dirtry[2]= south;
				dirtry[2] = Direction8.SOUTH;
				// 		dirtry[4]= north;
				dirtry[4] = Direction8.NORTH;
				// 	}
			}
			// 	else
			else
			// 	{
			{
				// 		dirtry[2]= north;
				dirtry[2] = Direction8.NORTH;
				// 		dirtry[4]= south;
				dirtry[4] = Direction8.SOUTH;
				// 	}
			}
			// 
			//
			// randomize a bit for dodging
			//
			// 	absdx = abs(deltax);
			// 	absdy = abs(deltay);
			// 
			// 	if (absdx > absdy)
			if (Mathf.Abs(deltax) > Mathf.Abs(deltay))
			// 	{
			{
				// 		tdir = dirtry[1];
				Direction8 tdir = dirtry[1];
				// 		dirtry[1] = dirtry[2];
				dirtry[1] = dirtry[2];
				// 		dirtry[2] = tdir;
				dirtry[2] = tdir;
				// 		tdir = dirtry[3];
				tdir = dirtry[3];
				// 		dirtry[3] = dirtry[4];
				dirtry[3] = dirtry[4];
				// 		dirtry[4] = tdir;
				dirtry[4] = tdir;
				// 	}
			}
			// 
			// 	if (US_RndT() < 128)
			if (Main.RNG.NextBoolean())
			// 	{
			{
				// 		tdir = dirtry[1];
				Direction8 tdir = dirtry[1];
				// 		dirtry[1] = dirtry[2];
				dirtry[1] = dirtry[2];
				// 		dirtry[2] = tdir;
				dirtry[2] = tdir;
				// 		tdir = dirtry[3];
				tdir = dirtry[3];
				// 		dirtry[3] = dirtry[4];
				dirtry[3] = dirtry[4];
				// 		dirtry[4] = tdir;
				dirtry[4] = tdir;
				// 	}
			}
			// 
			// 	dirtry[0] = diagonal [ dirtry[1] ] [ dirtry[2] ];
			dirtry[0] = Direction8.Combine(dirtry[1], dirtry[2]);
			// 
			//
			// try the directions util one works
			//
			// 	for (i=0;i<5;i++)
			foreach (Direction8 direction in dirtry)
				// 	{
				// 		if ( dirtry[i] == nodir || dirtry[i] == turnaround)
				// 			continue;
				// 
				// 		ob->dir = dirtry[i];
				// 		if (TryWalk(ob))
				// 			return;
				// 	}
				if (direction != null && direction != turnaround && TryWalk(direction))
					return direction;

			foreach (Direction8 direction in Direction8.RandomOrder(turnaround))
				if (TryWalk(direction))
					return direction;
			//
			// turn around only as a last resort
			//
			// 	if (turnaround != nodir)
			// 	{
			// 		ob->dir = turnaround;
			// 
			// 		if (TryWalk(ob))
			// 			return;
			// 	}
			if (turnaround != null && TryWalk(turnaround))
				return turnaround;
			// 
			// 	ob->dir = nodir;
			return null;
		}
		/// <summary>
		/// As SelectDodgeDir, but doesn't try to dodge
		/// </summary>
		public Direction8 SelectChaseDir()
		{
			// 	int deltax,deltay,i;
			// 	dirtype d[3];
			Direction8[] d = new Direction8[3];
			// 	dirtype tdir, olddir, turnaround;
			// 
			// 
			// 	olddir=ob->dir;
			Direction8 olddir = Direction,
				// 	turnaround=opposite[olddir];
				turnaround = olddir?.Opposite ?? null;
			// 
			// 	deltax=player->tilex - ob->tilex;
			int deltax = Main.ActionRoom.ARVRPlayer.X - TileX,
				// 	deltay=player->tiley - ob->tiley;
				deltay = Main.ActionRoom.ARVRPlayer.Z - TileZ;
			// 
			// 	d[1]=nodir;
			// 	d[2]=nodir;
			// 
			// 	if (deltax>0)
			if (deltax > 0)
				// 		d[1]= east;
				d[1] = Direction8.EAST;
			// 	else if (deltax<0)
			else if (deltax < 0)
				// 		d[1]= west;
				d[1] = Direction8.WEST;
			// 	if (deltay>0)
			if (deltay > 0)
				// 		d[2]=south;
				d[2] = Direction8.SOUTH;
			// 	else if (deltay<0)
			else if (deltay < 0)
				// 		d[2]=north;
				d[2] = Direction8.NORTH;
			// 
			// 	if (abs(deltay)>abs(deltax))
			if (Mathf.Abs(deltay) > Mathf.Abs(deltax))
			// 	{
			{
				// 		tdir=d[1];
				Direction8 tdir = d[1];
				// 		d[1]=d[2];
				d[1] = d[2];
				// 		d[2]=tdir;
				d[2] = tdir;
				// 	}
			}
			// 
			// 	if (d[1]==turnaround)
			if (d[1] == turnaround)
				// 		d[1]=nodir;
				d[1] = null;
			// 	if (d[2]==turnaround)
			if (d[2] == turnaround)
				// 		d[2]=nodir;
				d[2] = null;
			// 
			// 
			// 	if (d[1]!=nodir)
			// 	{
			// 		ob->dir=d[1];
			// 		if (TryWalk(ob))
			// 			return;     /*either moved forward or attacked*/
			// 	}
			if (d[1] != null && TryWalk(d[1]))
				return d[1]; // either moved forward or attacked
							 // 
							 // 	if (d[2]!=nodir)
							 // 	{
							 // 		ob->dir=d[2];
							 // 		if (TryWalk(ob))
							 // 			return;
			if (d[2] != null && TryWalk(d[2]))
				return d[2];
			// 	}
			// 
			// /* there is no direct path to the player, so pick another direction */
			// 
			// 	if (olddir!=nodir)
			// 	{
			// 		ob->dir=olddir;
			// 		if (TryWalk(ob))
			// 			return;
			// 	}
			if (olddir != null && TryWalk(olddir))
				return olddir;
			// 
			// randomly determine direction of search
			if (RandomDirection(turnaround) is Direction8 dir)
				return dir;
			// 	if (turnaround !=  nodir)
			// 	{
			// 		ob->dir=turnaround;
			// 		if (ob->dir != nodir)
			// 		{
			// 			if ( TryWalk(ob) )
			// 				return;
			// 		}
			// 	}
			if (turnaround != null && TryWalk(turnaround))
				return turnaround;
			// 
			// 	ob->dir = nodir;		// can't move
			return null;
		}
		public bool TryWalk() => TryWalk(Direction);
		public bool TryWalk(Direction8 direction) => Main.ActionRoom.Level.TryWalk(direction, X, Z);
		public Direction8 RandomDirection(params Direction8[] excluded)
		{
			foreach (Direction8 direction in Direction8.RandomOrder(excluded))
				if (TryWalk(direction))
					return direction;
			return null;
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
			XMLScript.Run(ActorXML, this);
			return this;
		}
		public bool CheckChase(float delta = 0f)
		{
			if (SightPlayer(delta))
			{
				if (!Settings.DigiSoundMuted
					&& ActorXML?.Attribute("AlertDigiSound")?.Value is string digiSound
					&& Assets.DigiSoundSafe(digiSound) is AudioStreamSample audioStreamSample)
					Play = audioStreamSample;
				if (Direction8.AngleToPoint(GlobalTransform.origin, Main.ActionRoom.ARVRPlayer.GlobalTransform.origin) is Direction8 towardsPlayer
					&& TryWalk(towardsPlayer))
					Direction = towardsPlayer;
				else if (!TryWalk())
					Direction = null;
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
			if (Direction != null && !Direction.InSight(Transform.origin, Main.ActionRoom.ARVRPlayer.Transform.origin))
				return false;
			return CheckLine();
		}
		public bool CheckLine() => Main.ActionRoom.Level.CheckLine(
			Transform.origin.x,
			Transform.origin.z,
			Main.ActionRoom.ARVRPlayer.Transform.origin.x,
			Main.ActionRoom.ARVRPlayer.Transform.origin.z
			);
	}
}
