using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Elevator : FourWalls, ISavable
	{
		#region Data
		public XElement XML { get; set; }
		public Direction8 Direction { get; set; } = null;
		public bool Pushed { get; set; } = false;
		public override XElement Save()
		{
			XElement e = base.Save(); // FourWalls
			e.Name = XName.Get(GetType().Name);
			e.SetAttributeValue(XName.Get("Direction"), Direction.ToString());
			e.SetAttributeValue(XName.Get("Pushed"), Pushed);
			e.SetAttributeValue(XName.Get("XML"), XML.ToString());
			return e;
		}
		#endregion Data
		public Elevator(XElement xml) : base(xml)
		{
			XML = xml.Attribute("XML")?.Value is string a ? XElement.Parse(a) : xml;
			if (XML?.Attribute("Name")?.Value is string name)
				Name = name;
			if (xml?.Attribute("Direction")?.Value is string direction)
				Direction = Direction8.From(direction);
			Pushed = xml.IsTrue("Pushed");
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
				XMLScript.Run(XML, this);
				return true;
			}
			return false;
		}
	}
}
