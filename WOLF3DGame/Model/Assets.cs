using Godot;
using OPL;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WOLF3DGame.Model
{
    /// <summary>
    /// Assets takes the bytes extracted from the Wolfenstein 3-D files and creates the corresponding Godot objects for them to be used throughout the game.
    /// </summary>
    public class Assets
    {
        #region Math
        //Tom Hall's Doom Bible and also tweets from John Carmack state that the walls in Wolfenstein 3-D were always eight feet thick. The wall textures are 64x64 pixels, which means that the ratio is 8 pixels per foot.
        //However, VR uses the metric system, where 1 game unit is 1 meter in real space. One foot equals 0.3048 meters.
        //Now unless I am a complete failure at basic math (quite possible) this means that to scale Wolfenstein 3D correctly in VR, one pixel must equal 0.0381 in game units, and a Wolfenstein 3D wall must be 2.4384 game units thick.
        public const float PixelWidth = 0.0381f;
        public const float WallWidth = 2.4384f;
        public const float HalfWallWidth = 1.2192f;

        /// <param name="x">A distance in meters</param>
        /// <returns>The corresponding map coordinate</returns>
        public static int IntCoordinate(float x) => Mathf.FloorToInt(x / WallWidth);
        /// <param name="x">A map coordinate</param>
        /// <returns>Center of the map square in meters</returns>
        public static float CenterSquare(int x) => x * WallWidth + HalfWallWidth;
        /// <param name="x">A map coordinate</param>
        /// <returns>North or east corner of map square in meters</returns>
        public static float FloatCoordinate(int x) => x * WallWidth;

        // However, Wolfenstein 3D ran in SVGA screen mode 13h, which has a 320x200 resolution in a 4:3 aspect ratio.
        // This means that the pixels are not square! They have a 1.2:1 aspect ratio.
        public static readonly Vector3 Scale = new Vector3(1f, 1.2f, 1f);
        public const float PixelHeight = 0.04572f;
        public const double WallHeight = 2.92608;
        public const double HalfWallHeight = 1.46304;
        public static readonly Transform WallTransform = new Transform(Basis.Identity, new Vector3(HalfWallWidth, (float)HalfWallHeight, 0));
        public static readonly Transform WallTransformFlipped = new Transform(Basis.Identity.Rotated(Vector3.Up, Mathf.Pi), WallTransform.origin);
        public static readonly Transform BillboardTransform = new Transform(Basis.Identity, new Vector3(0f, (float)HalfWallHeight, 0f));

        // Tests reveal that BJ's run speed is 11.2152 tiles/sec. http://diehardwolfers.areyep.com/viewtopic.php?p=82938#82938
        // 11.2152 tiles per second * 2.4384 meters per tile = 27.34714368 meters per second
        // Walking speed is half of running speed.
        public const float RunSpeed = 27.34714368f;
        public const float WalkSpeed = 13.67357184f;

        public const float DeadZone = 0.65f;

        public static readonly QuadMesh Wall = new QuadMesh()
        {
            Size = new Vector2(WallWidth, (float)WallHeight),
        };

        public static readonly BoxShape BoxShape = new BoxShape()
        {
            Extents = new Vector3(WallWidth, (float)WallHeight, WallWidth),
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
        #endregion Math

        #region Game assets
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
            Animations = new Dictionary<string, uint[][]>();
            foreach (XElement actor in XML.Element("VSwap")?.Element("Objects")?.Elements("Actor") ?? Enumerable.Empty<XElement>())
                foreach (XElement animation in actor.Elements("Animation"))
                {
                    bool directional = IsTrue(animation, "Directional");
                    IEnumerable<XElement> framesX = animation.Elements("Frame");
                    uint[][] frames = new uint[framesX.Count()][];
                    for (uint frame = 0; frame < frames.Length; frame++)
                        if (directional)
                        {
                            frames[frame] = new uint[Direction8.Values.Count];
                            uint east = (from e in framesX
                                         where (uint)e.Attribute("Number") == frame
                                         select (uint)e.Attribute("Page")).First();
                            for (uint direction = 0; direction < frames[frame].Length; direction++)
                                frames[frame][direction] = east + direction;
                        }
                        else
                            frames[frame] = new uint[1] {
                            (from e in animation.Elements("Frame")
                            where (uint)e.Attribute("Number") == frame
                            select (uint)e.Attribute("Page")).First()
                            };
                    Animations.Add(actor.Attribute("Name").Value + "/" + animation.Attribute("Name").Value, frames);
                }
        }

        public static bool IsTrue(XElement xElement, string attribute) =>
            bool.TryParse(xElement?.Attribute(attribute)?.Value, out bool @bool) && @bool;

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
            get => vswap;
            set
            {
                vswap = value;
                Palette = new Color[VSwap.Palette.Length];
                for (uint i = 0; i < Palette.Length; i++)
                    Palette[i] = Color.Color8(
                            VSwap.R(VSwap.Palette[i]),
                            VSwap.G(VSwap.Palette[i]),
                            VSwap.B(VSwap.Palette[i]),
                            VSwap.A(VSwap.Palette[i])
                        );
                VSwapTextures = new ImageTexture[VSwap.Pages.Length];
                VSwapMaterials = new Material[VSwapTextures.Length];
                for (uint i = 0; i < VSwapTextures.Length; i++)
                    if (VSwap.Pages[i] != null)
                    {
                        Godot.Image image = new Image();
                        image.CreateFromData(64, 64, false, Image.Format.Rgba8, VSwap.Pages[i]);
                        VSwapTextures[i] = new ImageTexture();
                        VSwapTextures[i].CreateFromImage(image, (int)Texture.FlagsEnum.ConvertToLinear);
                        VSwapMaterials[i] = new SpatialMaterial()
                        {
                            AlbedoTexture = VSwapTextures[i],
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
            get => vgaGraph;
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
        public Color[] Palette;
        public ImageTexture[] VSwapTextures;
        public Material[] VSwapMaterials;
        public Dictionary<string, uint[][]> Animations;
        public ImageTexture[] Pics;
        #endregion Game assets
    }
}
