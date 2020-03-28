using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.Menu
{
    public class MenuItem : Node2D
    {
        public Sprite Text { get; set; }
        public MenuItem(string text, VgaGraph.Font font)
        {
            AddChild(Text = new Sprite()
            {
                Texture = Assets.Text(text, font),
            });
        }
        public static MenuItem[] MenuItems(XElement menu)
        {
            VgaGraph.Font font = Assets.VgaGraph.Fonts[uint.TryParse(menu.Attribute("Font")?.Value, out uint result) ? result : 0];
            List<MenuItem> items = new List<MenuItem>();
            foreach (XElement option in menu.Elements("Option"))
                items.Add(new MenuItem(option.Attribute("Name").Value, font)
                {
                    Position = new Vector2(
                        (uint)menu.Attribute("StartX"),
                        (uint)menu.Attribute("StartY") + items.Count() * font.Height
                        ),
                });
            return items.ToArray();
        }
    }
}
