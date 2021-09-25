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
			e.SetAttributeValue(XName.Get("X"), X);
			e.SetAttributeValue(XName.Get("Z"), Z);
			e.SetAttributeValue(XName.Get("Wall"), Wall);
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
		protected FourWalls Set(ushort wall, ushort darkSide)
		{
			Name = "FourWalls";
			CollisionShape shape;
			AddChild(shape = Walls.BuildWall(darkSide, true, 0, 0)); // West
			Sides[DirectionIndex(Direction8.WEST)] = (MeshInstance)shape.GetChild(0);
			AddChild(shape = Walls.BuildWall(wall, false, 0, 0, true)); // North
			Sides[DirectionIndex(Direction8.NORTH)] = (MeshInstance)shape.GetChild(0);
			AddChild(shape = Walls.BuildWall(darkSide, true, 0, -1, true)); // East
			Sides[DirectionIndex(Direction8.EAST)] = (MeshInstance)shape.GetChild(0);
			AddChild(shape = Walls.BuildWall(wall, false, 1, 0)); // South
			Sides[DirectionIndex(Direction8.SOUTH)] = (MeshInstance)shape.GetChild(0);
			Size = new Vector2(Assets.WallWidth, Assets.WallWidth);
			this.wall = wall;
			this.darkSide = darkSide;
			return this;
		}
	}
}
