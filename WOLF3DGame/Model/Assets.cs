using Godot;
using OPL;
using System.IO;
using System.Xml.Linq;

namespace WOLF3DGame.Model
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
        public static readonly double HalfWallHeight = 1.46304;
        public static readonly Transform WallTransform = new Transform(Basis.Identity, new Vector3(HalfWallWidth, (float)HalfWallHeight, 0));
        public static readonly Transform BillboardTransform = new Transform(Basis.Identity, new Vector3(0f, (float)HalfWallHeight, 0f));

        // Tests reveal that BJ's run speed is 11.2152 tiles/sec. http://diehardwolfers.areyep.com/viewtopic.php?p=82938#82938
        // 11.2152 tiles per second * 2.4384 meters per tile = 27.34714368 meters per second
        // Walking speed is half of running speed.
        public static readonly float RunSpeed = 27.34714368f;
        public static readonly float WalkSpeed = 13.67357184f;

        public Assets(string folder, string file = "game.xml") : this(folder, LoadXML(folder, file))
        { }

        public Assets(string folder, XElement xml)
        {
            XML = xml;
            if (XML.Element("VSwap") != null)
                VSwap = VSwap.Load(folder, XML);
            if (XML.Element("Maps") != null)
                Maps = GameMap.Load(folder, XML);
            if (XML.Element("Audio") != null)
                AudioT = AudioT.Load(folder, XML);
            if (XML.Element("VgaGraph") != null)
                VgaGraph = VgaGraph.Load(folder, XML);
        }

        public static XElement LoadXML(string folder, string file = "game.xml")
        {
            using (FileStream xmlStream = new FileStream(System.IO.Path.Combine(folder, file), FileMode.Open))
                return XElement.Load(xmlStream);
        }

        public XElement XML { get; set; }
        public GameMap[] Maps { get; set; }
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
                Textures = new ImageTexture[VSwap.Pages.Length];
                WallMaterials = new Material[Textures.Length];
                for (uint i = 0; i < Textures.Length; i++)
                    if (VSwap.Pages[i] != null)
                    {
                        Godot.Image image = new Image();
                        image.CreateFromData(64, 64, false, Image.Format.Rgba8, VSwap.Pages[i]);
                        Textures[i] = new ImageTexture();
                        Textures[i].CreateFromImage(image, (int)Texture.FlagsEnum.ConvertToLinear);
                        WallMaterials[i] = new SpatialMaterial()
                        {
                            AlbedoTexture = Textures[i],
                            FlagsUnshaded = true,
                            FlagsDoNotReceiveShadows = true,
                            FlagsDisableAmbientLight = true,
                            FlagsTransparent = i >= VSwap.SpritePage,
                            ParamsCullMode = i >= VSwap.SpritePage ? SpatialMaterial.CullMode.Back : SpatialMaterial.CullMode.Disabled,
                            ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                        };
                    }
            }
        }
        private VSwap vswap;

        public VgaGraph VgaGraph
        {
            get
            {
                return vgaGraph;
            }
            set
            {
                vgaGraph = value;
                Pics = new ImageTexture[VgaGraph.Pics.Length];
                for (uint i = 0; i < Pics.Length; i++)
                    if (VgaGraph.Pics[i] != null)
                    {
                        Godot.Image image = new Image();
                        image.CreateFromData(VgaGraph.Sizes[i][0], VgaGraph.Sizes[i][1], false, Image.Format.Rgba8, VgaGraph.Pics[i]);
                        Pics[i] = new ImageTexture();
                        Pics[i].CreateFromImage(image, 0); //(int)Texture.FlagsEnum.ConvertToLinear);
                    }
            }
        }
        private VgaGraph vgaGraph;

        public ImageTexture[] Textures;
        public Material[] WallMaterials;
        public ImageTexture[] Pics;

        public static readonly QuadMesh Wall = new QuadMesh()
        {
            Size = new Vector2(WallWidth, (float)WallHeight),
        };

        public static readonly Vector3 Rotate90 = new Vector3(0, Godot.Mathf.Pi / 2f, 0);

        public static Vector3 Axis(Vector3.Axis axis)
        {
            switch (axis)
            {
                case Vector3.Axis.X:
                    return Rotate90;
                case Vector3.Axis.Y:
                    return Vector3.Up;
                case Vector3.Axis.Z:
                default:
                    return Vector3.Zero;
            }
        }
    }
}
