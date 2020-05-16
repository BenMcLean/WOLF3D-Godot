using Godot;
using System.Xml.Linq;

namespace WOLF3D.WOLF3DGame.Action
{
    public class StatusBar : Viewport
    {
        public StatusBar() : this(Assets.XML.Element("VgaGraph").Element("StatusBar")) { }
        public StatusBar(XElement xml)
        {
            Name = "StatusBar";
            Disable3d = true;
            RenderTargetClearMode = ClearMode.OnlyNextFrame;
            RenderTargetVFlip = true;
            XML = xml;
            ImageTexture pic = Assets.PicTextureSafe(XML.Attribute("Pic")?.Value);
            Size = pic.GetSize();
            AddChild(new Sprite()
            {
                Name = "StatusBarPic",
                Texture = pic,
                Position = Size / 2,
            });
        }

        public XElement XML { get; set; }
    }
}
