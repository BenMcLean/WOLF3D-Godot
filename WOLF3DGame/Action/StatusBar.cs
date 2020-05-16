using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class StatusBar : Viewport
    {
        public StatusBar(XElement xml)
        {
            Name = "StatusBar";
            XML = xml;
            ImageTexture pic = Assets.PicTextureSafe(xml.Attribute("Pic")?.Value);
            Size = pic.GetSize();
            AddChild(new Sprite()
            {
                Texture = pic,
            });
        }

        public XElement XML { get; set; }
    }
}
