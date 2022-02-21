using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public abstract class FourWalls : Pushable, ISavable
	{
		#region Data
		public ushort X { get; set; } = 0;
		public ushort Z { get; set; } = 0;
		protected readonly MeshInstance[] Sides = new MeshInstance[4];
		public CollisionShape CollisionShape { get; protected set; }
		public ushort Wall
		{
			get => wall;
			private set
			{
				wall = value;
				Sides[DirectionIndex(Direction8.NORTH)].MaterialOverride = Assets.VSwapMaterials[wall];
				Sides[DirectionIndex(Direction8.SOUTH)].MaterialOverride = Assets.VSwapMaterials[wall];
			}
		}
		private ushort wall;
		public ushort DarkSide
		{
			get => darkSide;
			private set
			{
				darkSide = value;
				Sides[DirectionIndex(Direction8.EAST)].MaterialOverride = Assets.VSwapMaterials[darkSide];
				Sides[DirectionIndex(Direction8.WEST)].MaterialOverride = Assets.VSwapMaterials[darkSide];
			}
		}
		private ushort darkSide;
		public virtual XElement Save()
		{
			XElement e = new XElement(XName.Get(GetType().Name));
			e.SetAttributeValue(XName.Get("X"), Transform.origin.x);
			e.SetAttributeValue(XName.Get("Z"), Transform.origin.z);
			e.SetAttributeValue(XName.Get("TileX"), X);
			e.SetAttributeValue(XName.Get("TileZ"), Z);
			e.SetAttributeValue(XName.Get("Page"), Wall);
			e.SetAttributeValue(XName.Get("DarkSide"), DarkSide);
			return e;
		}
		#endregion Data
		/// <param name="cardinal">Cardinal direction</param>
		/// <returns>Index corresponding to that direction in Sides</returns>
		public static uint DirectionIndex(Direction8 cardinal) => cardinal.Value >> 1;
		/// <param name="side">Array index from Sides</param>
		/// <returns>Cardinal direction that side is facing</returns>
		public static Direction8 Cardinal(uint side) => Direction8.From(side << 1);
		protected FourWalls(XElement xml)
		{
			Set((ushort)(uint)xml.Attribute("Page"), ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page"));
			if (ushort.TryParse(xml.Attribute("TileX")?.Value, out ushort tileX))
				X = tileX;
			if (ushort.TryParse(xml.Attribute("TileZ")?.Value, out ushort tileZ))
				Z = tileZ;
			if (float.TryParse(xml.Attribute("X")?.Value, out float x) && float.TryParse(xml.Attribute("Z")?.Value, out float z))
				Transform = new Transform(Basis.Identity, new Vector3(x, 0f, z));
		}
		protected FourWalls Set(ushort wall, ushort darkSide)
		{
			Name = "FourWalls";
			AddChild(Sides[DirectionIndex(Direction8.WEST)] = Walls.BuildWall(darkSide, true, 0, 0)); // West
			AddChild(Sides[DirectionIndex(Direction8.NORTH)] = Walls.BuildWall(wall, false, 0, 0, true)); // North
			AddChild(Sides[DirectionIndex(Direction8.EAST)] = Walls.BuildWall(darkSide, true, 0, -1, true)); // East
			AddChild(Sides[DirectionIndex(Direction8.SOUTH)] = Walls.BuildWall(wall, false, 1, 0)); // South
			Size = new Vector2(Assets.WallWidth, Assets.WallWidth);
			AddChild(CollisionShape = new CollisionShape()
			{
				Name = "FourWalls CollisionShape at [" + X + ", " + Z + "]",
				Shape = Assets.BoxShape,
				Transform = new Transform(Basis.Identity, new Vector3(Assets.HalfWallWidth, Assets.HalfWallHeight, Assets.HalfWallWidth)),
			});
			this.wall = wall;
			this.darkSide = darkSide;
			return this;
		}
	}
}
