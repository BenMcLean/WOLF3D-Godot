using Godot;

namespace WOLF3D.WOLF3DGame.Action
{
    public abstract class FourWalls : Pushable
    {
        public ushort X { get; set; } = 0;
        public ushort Z { get; set; } = 0;
        protected readonly MeshInstance[] Sides = new MeshInstance[4];

        /// <param name="cardinal">Cardinal direction</param>
        /// <returns>Index corresponding to that direction in Sides</returns>
        public static uint Index(Direction8 cardinal) => cardinal.Value >> 1;

        /// <param name="side">Array index from Sides</param>
        /// <returns>Cardinal direction that side is facing</returns>
        public static Direction8 Cardinal(uint side) => Direction8.From(side << 1);

        public FourWalls(ushort wall, ushort darkSide)
        {
            Name = "FourWalls";
            CollisionShape shape;
            AddChild(shape = Walls.BuildWall(darkSide, true, 0, 0)); // West
            Sides[Index(Direction8.WEST)] = (MeshInstance)shape.GetChild(0);
            AddChild(shape = Walls.BuildWall(wall, false, 0, 0, true)); // North
            Sides[Index(Direction8.NORTH)] = (MeshInstance)shape.GetChild(0);
            AddChild(shape = Walls.BuildWall(darkSide, true, 0, -1, true)); // East
            Sides[Index(Direction8.EAST)] = (MeshInstance)shape.GetChild(0);
            AddChild(shape = Walls.BuildWall(wall, false, 1, 0)); // South
            Sides[Index(Direction8.SOUTH)] = (MeshInstance)shape.GetChild(0);
            Size = new Vector2(Assets.WallWidth, Assets.WallWidth);
        }
    }
}
