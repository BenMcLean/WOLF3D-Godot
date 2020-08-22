using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class Pickup : Billboard
    {
        public Pickup(XElement xml) : base(xml) { }
        public override void _Process(float delta)
        {
            base._Process(delta); // Billboard
            if (!Main.Room.Paused)
                Main.ActionRoom.Pickup(this);
        }
    }
}
