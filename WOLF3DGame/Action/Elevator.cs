using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Elevator : FourWalls
    {
        public XElement XML;
        public Direction8 Direction { get; set; } = null;
        public bool Pushed { get; set; } = false;

        public Elevator(XElement xml) : base(
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

        public override bool Push(Direction8 direction)
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
                        Sides[DirectionIndex(Direction)].MaterialOverride = Assets.VSwapMaterials[activated];
                        Sides[DirectionIndex(Direction.Opposite)].MaterialOverride = Assets.VSwapMaterials[activated];
                    }
                XMLScript.Run(XML);
                return true;
            }
            return false;
        }
    }
}
