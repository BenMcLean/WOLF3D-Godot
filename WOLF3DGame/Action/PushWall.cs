using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class PushWall : StaticBody
    {
        public XElement XML { get; set; } = null;
        public PushWall(XElement xml) : this(
            (ushort)(uint)xml.Attribute("Page"),
            ushort.TryParse(xml.Attribute("DarkSide")?.Value, out ushort d) ? d : (ushort)(uint)xml.Attribute("Page")
            )
            => XML = xml;

        public PushWall(ushort wall, ushort darkSide)
        {
            Name = "Pushwall";
            AddChild(Walls.BuildWall(wall, false, 0, 0, true));
            AddChild(Walls.BuildWall(wall, false, 1, 0));
            AddChild(Walls.BuildWall(darkSide, true, 0, 0));
            AddChild(Walls.BuildWall(darkSide, true, 0, -1, true));
        }
    }
}
