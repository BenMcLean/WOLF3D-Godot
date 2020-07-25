using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Elevator : Pushable
    {
        public XElement XML;
        public ushort X { get; set; } = 0;
        public ushort Z { get; set; } = 0;
        private readonly MeshInstance[] Sides = new MeshInstance[4];
        public Direction8 Direction { get; set; } = null;
        public bool Pushed { get; set; } = false;

        /// <param name="direction">Cardinal direction</param>
        /// <returns>Index corresponding to that direction in Sides</returns>
        public static uint Index(Direction8 direction) => direction.Value >> 1;

        /// <param name="side">Array index from Sides</param>
        /// <returns>Cardinal direction that side is facing</returns>
        public static Direction8 Cardinal(uint side) => Direction8.From(side << 1);

        public Elevator(XElement xml) : this(
            (ushort)(uint)xml.Attribute("Page"),
            ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page")
            )
        {
            XML = xml;
            if (xml?.Attribute("Name")?.Value is string name)
                Name = name;
            if (xml?.Attribute("Direction")?.Value is string direction)
                Direction = Direction8.From(direction);
        }

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

        public override bool Push() => Push(Direction8.CardinalToPoint(
            Main.ActionRoom.ARVRPlayer.GlobalTransform.origin,
            GlobalTransform.origin + new Vector3(Assets.HalfWallWidth, 0, Assets.HalfWallWidth)
            ));

        public bool Push(Direction8 direction)
        {
            if (Direction == null || Direction == direction || Direction.Opposite == direction)
            {
                Pushed = true;
                if (ushort.TryParse(XML?.Attribute("Activated")?.Value, out ushort activated)
                    && activated < Assets.VSwapMaterials.Length)
                    if (Direction == null)
                        foreach (MeshInstance side in Sides)
                            side.MaterialOverride = Assets.VSwapMaterials[activated];
                    else
                    {
                        Sides[Index(Direction)].MaterialOverride = Assets.VSwapMaterials[activated];
                        Sides[Index(Direction.Opposite)].MaterialOverride = Assets.VSwapMaterials[activated];
                    }
                Main.MenuRoom.Action(XML);
                return true;
            }
            return false;
        }
    }
}
