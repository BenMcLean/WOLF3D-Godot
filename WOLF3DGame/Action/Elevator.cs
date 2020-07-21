using Godot;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Elevator : StaticBody
    {
        public MeshInstance[] Sides = new MeshInstance[4];

        /// <param name="direction">Cardinal direction</param>
        /// <returns>Index corresponding to that direction in Sides</returns>
        public static uint Index(Direction8 direction) => direction.Value >> 1;

        /// <param name="side">Array index from Sides</param>
        /// <returns>Cardinal direction that side is facing</returns>
        public static Direction8 Direction(uint side) => Direction8.From(side << 1);

        public Elevator(ushort wall, ushort darkSide)
        {
            Name = "Elevator";
            CollisionShape shape;
            AddChild(shape = Walls.BuildWall(darkSide, true, 0, 0)); // West
            Sides[Index(Direction8.WEST)] = (MeshInstance)shape.GetChild(0);
            AddChild(shape = Walls.BuildWall(wall, false, 0, 0, true)); // North
            Sides[Index(Direction8.NORTH)] = (MeshInstance)shape.GetChild(0);
            AddChild(shape = Walls.BuildWall(darkSide, true, 0, -1, true)); // East
            Sides[Index(Direction8.EAST)] = (MeshInstance)shape.GetChild(0);
            AddChild(shape = Walls.BuildWall(wall, false, 1, 0)); // South
            Sides[Index(Direction8.SOUTH)] = (MeshInstance)shape.GetChild(0);
        }
    }
}
