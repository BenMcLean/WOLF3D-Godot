using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
	public class Pickup : Billboard
	{
		public bool Treasure = false;
		public Godot.Color? Flash = null;
		public Pickup(XElement xml) : base(xml)
		{
			Treasure = XML.IsTrue("Treasure");
			if (XML.Attribute("Flash")?.Value is string flash
				&& new Godot.Color(flash) is Godot.Color color)
				Flash = color;
		}
		public override void _Process(float delta)
		{
			base._Process(delta); // Billboard
			if (!Main.Room.Paused)
				Main.ActionRoom.Pickup(this);
		}
	}
}
