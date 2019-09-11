using Godot;
using NScumm.Core.Audio.OPL;
using OPL;
using System.IO;
using System.Text;
using System.Xml.Linq;
using WOLF3DSim;

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
            using (FileStream game = new FileStream(System.IO.Path.Combine(folder, file), FileMode.Open))
                XML = XElement.Load(game);

            using (MemoryStream palette = new MemoryStream(Encoding.ASCII.GetBytes(XML.Element("Palette").Value)))
            using (FileStream vswap = new FileStream(System.IO.Path.Combine(folder, XML.Element("VSwap").Attribute("Name").Value), FileMode.Open))
                VSwap = new VSwap(palette, vswap);

            using (FileStream mapHead = new FileStream(System.IO.Path.Combine(folder, XML.Element("Maps").Attribute("MapHead").Value), FileMode.Open))
            using (FileStream gameMaps = new FileStream(System.IO.Path.Combine(folder, XML.Element("Maps").Attribute("GameMaps").Value), FileMode.Open))
                GameMaps = new GameMaps(mapHead, gameMaps);

            using (FileStream audioHead = new FileStream(System.IO.Path.Combine(folder, XML.Element("Audio").Attribute("AudioHead").Value), FileMode.Open))
            using (FileStream audioTFile = new FileStream(System.IO.Path.Combine(folder, XML.Element("Audio").Attribute("AudioT").Value), FileMode.Open))
                AudioT = new AudioT(audioHead, audioTFile);
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

        public ImageTexture[] Textures;

        public static readonly SpatialMaterial WallMaterial = new SpatialMaterial()
        {
            FlagsUnshaded = true,
            FlagsDoNotReceiveShadows = true,
            FlagsDisableAmbientLight = true,
            ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            ParamsCullMode = SpatialMaterial.CullMode.Disabled,
        };
    }
}
