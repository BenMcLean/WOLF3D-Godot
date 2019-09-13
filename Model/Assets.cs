using Godot;
using OPL;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace WOLF3D
{
    /// <summary>
    /// Assets takes the bytes extracted from VSwap and creates the corresponding Godot objects for them to be used throughout the game.
    /// </summary>
    public class Assets
    {
        //Tom Hall's Doom Bible and also tweets from John Carmack state that the walls in Wolfenstein 3D were always eight feet thick. The wall textures are 64x64 pixels, which means that the ratio is 8 pixels per foot.
        //However, VR uses the metric system, where 1 game unit is 1 meter in real space. One foot equals 0.3048 meters.
        //Now unless I am a complete failure at basic math (quite possible) this means that to scale Wolfenstein 3D correctly in VR, one pixel must equal 0.0381 in game units, and a Wolfenstein 3D wall must be 2.4384 game units thick.
        public static readonly float PixelWidth = 0.0381f;
        public static readonly float WallWidth = 2.4384f;
        public static readonly float HalfWallWidth = 1.2192f;

        // However, Wolfenstein 3D ran in SVGA screen mode 13h, which has a 320x200 resolution in a 4:3 aspect ratio.
        // This means that the pixels are not square! They have a 1.2:1 aspect ratio.
        public static readonly Vector3 Scale = new Vector3(1f, 1.2f, 1f);
        public static readonly float PixelHeight = 0.04572f;
        public static readonly double WallHeight = 2.92608;

        public static readonly Vector3 BillboardLocal = new Vector3(WallWidth / -2f, 0f, 0f);

        public Assets(string folder, string file = "game.xml")
        {
            XML = LoadXML(folder, file);
            VSwap = XML.Element("VSwap") == null ? null : VSwap.Load(folder, XML);
            GameMaps = XML.Element("Maps") == null ? null : GameMaps.Load(folder, XML);
            AudioT = XML.Element("Audio") == null ? null : AudioT.Load(folder, XML);

            if (XML.Element("VgaGraph") != null)
                using (FileStream vgaDict = new FileStream(System.IO.Path.Combine(folder, XML.Element("VgaGraph").Attribute("VgaDict").Value), FileMode.Open))
                using (FileStream vgaHead = new FileStream(System.IO.Path.Combine(folder, XML.Element("VgaGraph").Attribute("VgaHead").Value), FileMode.Open))
                using (FileStream vgaGraphStream = new FileStream(System.IO.Path.Combine(folder, XML.Element("VgaGraph").Attribute("VgaGraph").Value), FileMode.Open))
                    VgaGraph = new VgaGraph(vgaDict, vgaHead, vgaGraphStream);
            else
                VgaGraph = null;
        }

        public static XElement LoadXML(string folder, string file = "game.xml")
        {
            using (FileStream xmlStream = new FileStream(System.IO.Path.Combine(folder, file), FileMode.Open))
                return XElement.Load(xmlStream);
        }

        public XElement XML { get; set; }
        public GameMaps GameMaps { get; set; }

        public OplPlayer OplPlayer { get; set; }
        public AudioT AudioT { get; set; }

        public VSwap VSwap
        {
            get
            {
                return vswap;
            }
            set
            {
                vswap = value;
                Textures = new ImageTexture[VSwap.SoundPage];
                for (uint i = 0; i < Textures.Length; i++)
                    if (VSwap.Pages[i] != null)
                    {
                        Godot.Image image = new Image();
                        image.CreateFromData(64, 64, false, Image.Format.Rgba8, VSwap.Pages[i]);
                        Textures[i] = new ImageTexture();
                        Textures[i].CreateFromImage(image, (int)Texture.FlagsEnum.ConvertToLinear);
                    }
            }
        }
        private VSwap vswap;

        public VgaGraph VgaGraph { get; set; }

        public ImageTexture[] Textures;
    }
}
