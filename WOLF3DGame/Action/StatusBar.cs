using Godot;
using System.Linq;
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
            foreach (XElement number in XML.Elements("Number") ?? Enumerable.Empty<XElement>())
                AddChild(new StatusNumber(
                    uint.TryParse(number.Attribute("Digits")?.Value, out uint digits) ? digits : 0
                    )
                {
                    Name = number.Attribute("Name")?.Value,
                    Position = new Vector2(
                        float.TryParse(number.Attribute("X")?.Value, out float x) ? x : 0,
                        float.TryParse(number.Attribute("Y")?.Value, out float y) ? y : 0
                        ),
                    Value = 0,
                });
        }

        public XElement XML { get; set; }
    }
}
