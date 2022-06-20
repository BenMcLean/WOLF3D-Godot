using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3D.WOLF3DGame.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Level : Spatial, ISavable
	{
		#region Data Members
		public ushort MapNumber { get; set; } = 0;
		public GameMap Map => Assets.Maps[MapNumber];
		public MapAnalyzer.MapAnalysis MapAnalysis => Assets.MapAnalysis[MapNumber];
		public float Time { get; set; } = 0f;
		public Walls Walls { get; private set; }
		public Door[][] Doors { get; private set; }
		public readonly ArrayList PushWalls = new ArrayList();
		private readonly int[][] PushWallAt;
		public bool IsPushWallAt(ushort x, ushort z) => x < PushWallAt.Length && z < PushWallAt[x].Length && PushWallAt[x][z] != 0;
		public PushWall GetPushWallAt(ushort x, ushort z) =>
			x < PushWallAt.Length
			&& z < PushWallAt[x].Length
			&& PushWallAt[x][z] - 1 is int index
			&& index >= 0 && index < PushWalls.Count
			&& PushWalls[index] is PushWall pushWall ?
			pushWall
			: null;
		public Level SetPushWallAt(ushort x, ushort z, PushWall pushWall = null)
		{
			PushWallAt[x][z] = pushWall == null ? 0 : pushWall.ArrayIndex + 1;
			return this;
		}
		public Level ErasePushWall(PushWall pushWall) => ErasePushWall(pushWall.ArrayIndex);
		public Level ErasePushWall(int index)
		{
			for (ushort x = 0; x < PushWallAt.Length; x++)
				for (ushort z = 0; z < PushWallAt[x].Length; z++)
					if (PushWallAt[x][z] == index + 1)
						PushWallAt[x][z] = 0;
			return this;
		}
		public IEnumerable<Tuple<int, int>> PushWallMarked(PushWall pushWall) => PushWallMarked(pushWall.ArrayIndex);
		public IEnumerable<Tuple<int, int>> PushWallMarked(int index)
		{
			for (ushort x = 0; x < PushWallAt.Length; x++)
				for (ushort z = 0; z < PushWallAt[x].Length; z++)
					if (PushWallAt[x][z] == index + 1)
						yield return new Tuple<int, int>(x, z);
		}
		public readonly ArrayList Pickups = new ArrayList();
		public readonly ArrayList Actors = new ArrayList();
		private readonly int[][] ActorAt;
		public bool IsActorAt(ushort x, ushort z) => x < ActorAt.Length && z < ActorAt[x].Length && ActorAt[x][z] != 0;
		public Actor GetActorAt(ushort x, ushort z) =>
			x < ActorAt.Length
			&& z < ActorAt[x].Length
			&& ActorAt[x][z] - 1 is int index
			&& index >= 0 && index < Actors.Count
			&& Actors[index] is Actor actor ?
			actor
			: null;
		public Level SetActorAt(ushort x, ushort z, Actor actor = null)
		{
			ActorAt[x][z] = actor == null ? 0 : actor.ArrayIndex + 1;
			return this;
		}
		public Level EraseActor(Actor actor) => EraseActor(actor.ArrayIndex);
		public Level EraseActor(int index)
		{
			for (ushort x = 0; x < ActorAt.Length; x++)
				for (ushort z = 0; z < ActorAt[x].Length; z++)
					if (ActorAt[x][z] == index + 1)
						ActorAt[x][z] = 0;
			return this;
		}
		public SymmetricMatrix FloorCodes = new SymmetricMatrix(Assets.FloorCodes);
		public static bool Clipping { get; set; } = true;
		public XElement Save()
		{
			XElement e = new XElement(XName.Get(GetType().Name));
			e.SetAttributeValue(XName.Get("MapNumber"), Map.Number);
			e.SetAttributeValue(XName.Get("Time"), Time);
			e.Add(FloorCodes.Save());
			foreach (Door door in GetDoors())
				e.Add(door.Save());
			foreach (PushWall pushWall in PushWalls)
				e.Add(pushWall.Save());
			foreach (Pickup pickup in Pickups)
				e.Add(pickup.Save());
			foreach (Actor actor in Actors)
				e.Add(actor.Save());
			return e;
		}
		#endregion Data Members
		#region Loading
		public Level(XElement xml)
		{
			MapNumber = ushort.Parse(xml.Attribute("MapNumber")?.Value);
			AddChild(Walls = new Walls(MapNumber));
			Name = "Level \"" + Map.Name + "\"";
			if (float.TryParse(xml.Attribute("Time")?.Value, out float time))
				Time = time;
			FloorCodes = new SymmetricMatrix(xml.Element("SymmetricMatrix"));
			foreach (Billboard billboard in Billboard.Scenery(Map))
				AddChild(billboard);
			Doors = new Door[Map.Width][];
			PushWallAt = new int[Map.Width][];
			ActorAt = new int[Map.Width][];
			for (ushort x = 0; x < Map.Width; x++)
			{
				Doors[x] = new Door[Map.Depth];
				PushWallAt[x] = new int[Map.Depth];
				ActorAt[x] = new int[Map.Depth];
			}
			foreach (XElement xDoor in xml.Elements("Door"))
				AddChild(Doors[(int)xDoor.Attribute("X")][(int)xDoor.Attribute("Z")] = new Door(xDoor) { Level = this, }.SetFloorCodes(Map));
			foreach (XElement xPushWall in xml.Elements("PushWall"))
			{
				PushWall pushWall = new PushWall(xPushWall, XElement.Parse(xPushWall.Attribute("WallXML").Value))
				{
					Level = this,
				};
				pushWall.ArrayIndex = PushWalls.Add(pushWall);
				foreach (Tuple<int, int> tuple in xPushWall.Attribute("Marked").IntPairs())
					SetPushWallAt((ushort)tuple.Item1, (ushort)tuple.Item2, pushWall);
				AddChild(pushWall);
			}
			foreach (XElement xPickup in xml.Elements("Pickup"))
			{
				Pickup pickup = new Pickup(xPickup);
				Pickups.Add(pickup);
				AddChild(pickup);
			}
			foreach (XElement xActor in xml.Elements("Actor"))
			{
				Actor actor = new Actor(xActor);
				actor.ArrayIndex = Actors.Add(actor);
				if (!actor.NeverMark && actor.State.Mark)
					SetActorAt(actor.TileX, actor.TileZ, actor);
				AddChild(actor);
			}
		}
		public Level(ushort mapNumber, byte difficulty = 4)
		{
			MapNumber = mapNumber;
			Name = "Level \"" + Map.Name + "\"";
			AddChild(Walls = new Walls(MapNumber));
			Doors = Door.Doors(Map, this);
			foreach (Door door in GetDoors())
				AddChild(door);
			PushWallAt = new int[Map.Width][];
			ActorAt = new int[Map.Width][];
			for (ushort x = 0; x < Map.Width; x++)
			{
				PushWallAt[x] = new int[Map.Depth];
				ActorAt[x] = new int[Map.Depth];
			}
			foreach (XElement pushXML in Assets.MapAnalyzer.PushWall ?? Enumerable.Empty<XElement>())
				if (ushort.TryParse(pushXML?.Attribute("Number")?.Value, out ushort pushNumber))
					for (ushort x = 0; x < Map.Width; x++)
						for (ushort z = 0; z < Map.Depth; z++)
							if (Map.GetObjectData(x, z) == pushNumber)
							{
								PushWall pushWall = new PushWall(pushXML, Assets.MapAnalyzer.Wall(Map.GetMapData(x, z)))
								{
									Name = "Pushwall starting at " + x + ", " + z,
									Level = this,
									X = x,
									Z = z,
									Transform = new Transform(Basis.Identity, new Vector3(x * Assets.WallWidth, 0f, z * Assets.WallWidth)),
								};
								pushWall.ArrayIndex = PushWalls.Add(pushWall);
								SetPushWallAt(x, z, pushWall);
								AddChild(pushWall);
							}
			List<ushort> ambushes = new List<ushort>();
			foreach (XElement xAmbush in Assets.XML?.Element("VSwap")?.Element("Walls")?.Elements("Ambush") ?? XElement.EmptySequence)
				if (ushort.TryParse(xAmbush.Attribute("Number")?.Value, out ushort ambush))
					ambushes.Add(ambush);
			foreach (Billboard billboard in Billboard.Billboards(Map, difficulty))
			{
				AddChild(billboard);
				if (billboard is Actor actor)
				{
					actor.ArrayIndex = Actors.Add(actor);
					if (ambushes.Contains(Map.GetMapData((ushort)actor.X, (ushort)actor.Z)))
						actor.Ambush = true;
				}
				else if (billboard is Pickup pickup)
					Pickups.Add(pickup);
			}
		}
		#endregion Loading
		#region Doors
		public IEnumerable<Door> GetDoors()
		{
			for (uint x = 0; x < Doors.Length; x++)
				for (uint z = 0; z < Doors[x].Length; z++)
					if (Doors[x][z] != null)
						yield return Doors[x][z];
		}
		public Door GetDoor(int x, int z) => x >= 0 && z >= 0 && x < Map.Width && z < Map.Depth ? Doors[x][z] : null;
		public bool IsClosedDoor(int x, int z) => GetDoor(x, z)?.IsClosed ?? false;
		#endregion Doors
		#region Pushing
		public bool Push(Vector2 where)
		{
			bool Pushy()
			{
				if (GetDoor(Assets.IntCoordinate(where.x), Assets.IntCoordinate(where.y)) is Door door)
					return door.Push();
				foreach (PushWall pushWall in PushWalls)
					if (!pushWall.Pushed && pushWall.IsIn(where))
						return pushWall.Push();
				foreach (Elevator elevator in Walls.Elevators)
					if (elevator.IsIn(where))
						return elevator.Push();
				return false;
			}
			bool push = Pushy();
			if (!push && Assets.SoundSafe("DONOTHINGSND") is Adl sound)
				SoundBlaster.Adl = sound;
			if (push)
				MenuRoom.LastPushedTile = Main.ActionRoom.Level.Map.GetMapData(
					(uint)Main.ActionRoom.ARVRPlayer.X,
					(uint)Main.ActionRoom.ARVRPlayer.Z
					); // This is used to find override tiles to change the elevator destination
			return push;
		}
		#endregion Pushing
		#region Collision Detection
		public Vector2 PlayerWalk(Vector2 here, Vector2 there)
		{
			float x = TryPlayerWalk(new Vector2(there.x, here.y)) ? there.x : here.x;
			return new Vector2(x, TryPlayerWalk(new Vector2(x, there.y)) ? there.y : here.y);
		}
		public static float ToTheEdgeFromFloat(float here, int move) => move == 0 ? here : ToTheEdge(Assets.IntCoordinate(here), move);
		/// <summary>
		/// "Close to the edge, down by a river" - Yes
		/// </summary>
		public static float ToTheEdge(int here, int move) =>
			move > 0 ?
			Assets.FloatCoordinate(here + 1) - Assets.HeadXZ - float.Epsilon
			: move < 0 ?
			Assets.FloatCoordinate(here) + Assets.HeadXZ + float.Epsilon
			: Assets.CenterSquare(here);
		public bool TryPlayerWalk(Vector2 there, out Vector2 cant)
		{
			if (!Clipping)
			{
				cant = Vector2.Zero;
				return true;
			}
			foreach (Direction8 direction in Direction8.Diagonals)
				if (!TryPlayerWalkPoint(cant = there + direction.Vector2 * Assets.HeadDiagonal))
					return false;
			return TryPlayerWalkPoint(cant = there);
		}
		public bool TryWalk(Vector2 there, out Vector2 cant)

		{
			if (!Clipping)
			{
				cant = Vector2.Zero;
				return true;
			}
			foreach (Direction8 direction in Direction8.Diagonals)
				if (!TryWalkPoint(cant = there + direction.Vector2 * Assets.HeadDiagonal))
					return false;
			return TryWalkPoint(cant = there);
		}
		public bool TryPlayerWalk(Vector2 there) => TryPlayerWalk(there, out _);
		public bool TryWalk(Vector2 there) => TryWalk(there, out _);
		public bool TryPlayerWalkPoint(Vector2 there) => TryPlayerWalk(Assets.IntCoordinate(there.x), Assets.IntCoordinate(there.y)) && !IsInsideMarkedActor(there.x, there.y);
		public bool TryWalkPoint(Vector2 there) => TryWalk(Assets.IntCoordinate(there.x), Assets.IntCoordinate(there.y)) && !IsInsideMarkedActor(there.x, there.y);
		public bool TryPlayerWalk(int x, int z) =>
			Walls.IsNavigable(x, z)
			&& (!(Doors[x][z] is Door door) || door.IsOpen)
			&& !IsPushWallAt((ushort)x, (ushort)z);
		public bool TryWalk(int x, int z) =>
			TryPlayerWalk(x, z)
			&& !IsActorAt((ushort)x, (ushort)z);
		public bool TryPlayerWalk(Direction8 direction, int x, int z) =>
			direction == null ?
				TryPlayerWalk(x, z)
				: ((direction.IsCardinal || (
						TryPlayerWalk(x + direction.X, z)
						&& TryPlayerWalk(x, z + direction.Z)
					))
					&& TryPlayerWalk(x + direction.X, z + direction.Z));
		public bool TryWalk(Direction8 direction, int x, int z) =>
			direction == null ?
				TryWalk(x, z)
				: ((direction.IsCardinal || (
						TryWalk(x + direction.X, z)
						&& TryWalk(x, z + direction.Z)
					))
					&& TryWalk(x + direction.X, z + direction.Z));
		public bool CanCloseDoor(int x, int z) =>
			Walls.IsNavigable(x, z)
			&& !IsActorAt((ushort)x, (ushort)z)
			&& !IsPushWallAt((ushort)x, (ushort)z)
			&& !(Main.ActionRoom.ARVRPlayer.X == x && Main.ActionRoom.ARVRPlayer.Z == z)
			&& !IsInsideActor(Assets.CenterSquare(x), Assets.CenterSquare(z));
		public bool TryClose(ushort x, ushort z) =>
			x < Map.Width && z < Map.Depth && !Occupied.Contains(Map.GetIndex(x, z));
		public bool TryOpen(Door door, bool @bool = true) => TryOpen(door.X, door.Z, @bool);
		public bool TryOpen(ushort x, ushort z, bool @bool = true) => @bool && x < Map.Width && z < Map.Depth || TryClose(x, z);
		public bool IsWall(ushort x, ushort z) => Assets.MapAnalyzer.Walls.Contains(Map.GetMapData(x, z));
		public bool IsElevator(ushort x, ushort z) => Assets.MapAnalyzer.Elevators.Contains(Map.GetMapData(x, z));
		public bool IsTransparent(int x, int z) =>
			Walls.IsTransparent(x, z)
			&& (!(Doors[x][z] is Door door) || !door.IsClosed)
			&& !IsPushWallAt((ushort)x, (ushort)z);
		/// <returns>if the specified map coordinates are adjacent to a floor</returns>
		public bool IsByFloor(ushort x, ushort z)
		{
			ushort startX = x < 1 ? x : x > Map.Width - 1 ? (ushort)(Map.Width - 1) : (ushort)(x - 1),
				startZ = z < 1 ? z : z > Map.Depth - 1 ? (ushort)(Map.Depth - 1) : (ushort)(z - 1),
				endX = x >= Map.Width - 1 ? (ushort)(Map.Width - 1) : (ushort)(x + 1),
				endZ = z >= Map.Depth - 1 ? (ushort)(Map.Depth - 1) : (ushort)(z + 1);
			for (ushort dx = startX; dx <= endX; dx++)
				for (ushort dz = startZ; dz <= endZ; dz++)
					if ((dx != x || dz != z) && !IsWall(dx, dz))
						return true;
			return false;
		}
		public bool IsInsideActor(float x, float z) => InsideActor(x, z) != null;
		public Actor InsideActor(float x, float z)
		{
			foreach (Actor actor in Actors)
				if (actor.IsIn(x, z))
					return actor;
			return null;
		}
		public bool IsInsideMarkedActor(float x, float z) => InsideMarkedActor(x, z) != null;
		public Actor InsideMarkedActor(float x, float z)
		{
			foreach (Actor actor in Actors)
				if ((actor.State?.Mark ?? false) && actor.IsIn(x, z))
					return actor;
			return null;
		}
		public List<ushort> Occupied => SquaresOccupied(Main.ActionRoom.ARVRPlayer.Position);
		public List<ushort> SquaresOccupied(Vector3 vector3) => SquaresOccupied(Assets.Vector2(vector3));
		public List<ushort> SquaresOccupied(Vector2 vector2)
		{
			List<ushort> list = new List<ushort>();
			void add(Vector2 here)
			{
				int x = Assets.IntCoordinate(here.x), z = Assets.IntCoordinate(here.y);
				if (x >= 0 && z >= 0 && x < Map.Depth && z < Map.Width)
				{
					ushort square = Map.GetIndex((uint)x, (uint)z);
					if (!list.Contains(square))
						list.Add(square);
				}
			}
			add(vector2);
			foreach (Direction8 direction in Direction8.Diagonals)
				add(vector2 + direction.Vector2 * Assets.HeadDiagonal);
			return list;
		}
		#endregion Collision Detection
		public bool CheckLine(float ax, float ay, float bx, float by) => CheckLine(Assets.IntCoordinate(ax), Assets.IntCoordinate(ay), Assets.IntCoordinate(bx), Assets.IntCoordinate(by));
		/// <summary>
		/// trace a line to check for blocking tiles (corners)
		/// </summary>
		/// <returns>true if there are no blocking tiles</returns>
		public bool CheckLine(int ax, int ay, int bx, int by)
		{
			float x1 = ax, y1 = ay, x2 = bx, y2 = by,
				distance = Mathf.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)) * 256f,
				dx = (x2 - x1) / distance,
				dz = (y2 - y1) / distance;
			int tempX = Mathf.FloorToInt(x1),
				tempY = Mathf.FloorToInt(y1);
			for (int i = 0; i <= distance; i++)
			{
				x1 += dx;
				y1 += dz;
				if ((Mathf.FloorToInt(x1) != tempX || Mathf.FloorToInt(y1) != tempY)
					&& !IsTransparent(
						tempX = Mathf.FloorToInt(x1),
						tempY = Mathf.FloorToInt(y1)
						)
					)
					return false;
			}
			return true;
		}
		public override void _Process(float delta)
		{
			base._Process(delta);
			if (!Main.ActionRoom.Paused)
				Time += delta;
		}
		public Level PlaceItem(XElement xml, ITarget target)
		{
			Pickup pickup = target is Actor actor ?
				new Pickup(xml)
				{
					Transform = new Transform(
						actor.Transform.basis,
						new Vector3(Assets.CenterSquare(actor.TileX), 0f, Assets.CenterSquare(actor.TileZ))
						),
				}
				: new Pickup(xml)
				{
					Transform = new Transform(
						Basis.Identity,
						new Vector3(target.Position.x, 0f, target.Position.y)
						),
				};
			AddChild(pickup);
			Pickups.Add(pickup);
			return this;
		}
		#region Illuminate
		public bool IsSeeThrough(ushort x, ushort z) => MapAnalysis.IsTransparent(x, z) && !IsClosedDoor(x, z) && !IsPushWallAt(x, z);
		public bool[][] Illuminate() => Illuminate((ushort)Main.ActionRoom.ARVRPlayer.X,
					(ushort)Main.ActionRoom.ARVRPlayer.Z);
		public bool[][] Illuminate(ushort x, ushort z)
		{
			// TODO: Implement FOV algorithm https://www.gridbugs.org/visible-area-detection-recursive-shadowcast/
			bool[][] lit = new bool[Map.Width][];
			for (ushort i = 0; i < lit.Length; i++)
				lit[i] = new bool[Map.Depth];
			lit[x][z] = true;
			Illuminate(lit, x, z, 0, 1, Direction8.NORTH, Direction8.EAST, Direction8.NORTHWEST, Direction8.SOUTHWEST);
			Illuminate(lit, x, z, -1, 0, Direction8.EAST, Direction8.SOUTH, Direction8.NORTHWEST, Direction8.NORTHEAST);
			Illuminate(lit, x, z, 0, 1, Direction8.EAST, Direction8.SOUTH, Direction8.NORTHEAST, Direction8.NORTHWEST);
			Illuminate(lit, x, z, -1, 0, Direction8.SOUTH, Direction8.WEST, Direction8.NORTHEAST, Direction8.SOUTHEAST);
			Illuminate(lit, x, z, 0, 1, Direction8.SOUTH, Direction8.WEST, Direction8.SOUTHEAST, Direction8.NORTHEAST);
			Illuminate(lit, x, z, -1, 0, Direction8.WEST, Direction8.NORTH, Direction8.SOUTHEAST, Direction8.SOUTHWEST);
			Illuminate(lit, x, z, 0, 1, Direction8.WEST, Direction8.NORTH, Direction8.SOUTHWEST, Direction8.SOUTHEAST);
			Illuminate(lit, x, z, -1, 0, Direction8.NORTH, Direction8.EAST, Direction8.SOUTHWEST, Direction8.NORTHWEST);
			return lit;
		}
		protected void Illuminate(bool[][] lit, ushort startX, ushort startZ, int initialMinSlope, int initialMaxSlope, Direction8 depth, Direction8 scan, Direction8 opaqueCorner, Direction8 transparentCorner, ushort? previouxS = null, ushort? previouxZ = null)
		{
			//bool first = true;
			//if (scan.X == 0)
			//{
			//	for (int z = startZ + initialMinSlope; z >= 0 && z < lit[startX].Length && z < startZ + initialMaxSlope; z += scan.Z)
			//	{

			//	}
			//}

		}
		#endregion Illuminate
	}
}
