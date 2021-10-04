using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Pickup : Billboard, ISavable
	{
		public bool Treasure = false;
		public Godot.Color? Flash = null;
		public Pickup(XElement xml) : base(xml)
		{
			Treasure = xml.IsTrue("Treasure");
			if (xml.Attribute("Flash")?.Value is string flash
				&& new Godot.Color(flash) is Godot.Color color)
				Flash = color;
		}
		public override void _Process(float delta)
		{
			base._Process(delta); // Billboard
			if (!Main.Room.Paused)
				Main.ActionRoom.Pickup(this);
		}
		public override XElement Save()
		{
			XElement e = base.Save(); // Billboard
			e.Name = XName.Get(GetType().Name);
			e.SetAttributeValue(XName.Get("Treasure"), Treasure);
			if (Flash is Godot.Color flash)
				e.SetAttributeValue(XName.Get("Flash"), flash.ToHtml());
			return e;
		}
	}
}
