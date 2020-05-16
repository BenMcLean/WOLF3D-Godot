using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame
{
    /// <summary>
    /// Assets takes the bytes extracted from the Wolfenstein 3-D files and creates the corresponding Godot objects for them to be used throughout the game.
    /// </summary>
    public static class Assets
    {
        #region Math
        //Tom Hall's Doom Bible and also tweets from John Carmack state that the walls in Wolfenstein 3-D were always eight feet thick. The wall textures are 64x64 pixels, which means that the ratio is 8 pixels per foot.
        //However, VR uses the metric system, where 1 game unit is 1 meter in real space. One foot equals 0.3048 meters.
        public const float Foot = 0.3048f;
        public const float Inch = Foot / 12;
        //Now unless I am a complete failure at basic math (quite possible) this means that to scale Wolfenstein 3D correctly in VR, one pixel must equal 0.0381 in game units, and a Wolfenstein 3D wall must be 2.4384 game units thick.
        public const float PixelWidth = 0.0381f;
        public const float WallWidth = 2.4384f;
        public const float HalfWallWidth = 1.2192f;

        /// <param name="x">A distance in meters</param>
        /// <returns>The corresponding map coordinate</returns>
        public static int IntCoordinate(float x) => Mathf.FloorToInt(x / WallWidth);
        /// <param name="x">A map coordinate</param>
        /// <returns>Center of the map square in meters</returns>
        public static float CenterSquare(int x) => FloatCoordinate(x) + HalfWallWidth;
        public static float CenterSquare(uint x) => CenterSquare((int)x);
        public static float CenterSquare(ushort x) => CenterSquare((int)x);
        public static float CenterSquare(short x) => CenterSquare((int)x);
        /// <param name="x">A map coordinate</param>
        /// <returns>North or east corner of map square in meters</returns>
        public static float FloatCoordinate(int x) => x * WallWidth;
        public static float FloatCoordinate(uint x) => FloatCoordinate((int)x);
        public static float FloatCoordinate(ushort x) => FloatCoordinate((int)x);
        public static float FloatCoordinate(short x) => FloatCoordinate((int)x);

        // However, Wolfenstein 3D ran in SVGA screen mode 13h, which has a 320x200 resolution in a 4:3 aspect ratio.
        // This means that the pixels are not square! They have a 1.2:1 aspect ratio.
        public static readonly Vector3 Scale = new Vector3(1f, 1.2f, 1f);
        public const float PixelHeight = 0.04572f;
        public const float WallHeight = (float)2.92608;
        public const float HalfWallHeight = (float)1.46304;
        public static readonly Transform WallTransform = new Transform(Basis.Identity, new Vector3(HalfWallWidth, HalfWallHeight, 0));
        public static readonly Transform WallTransformFlipped = new Transform(Basis.Identity.Rotated(Godot.Vector3.Up, Mathf.Pi), WallTransform.origin);
        public static readonly BoxShape WallShape = new BoxShape()
        {
            Extents = new Vector3(HalfWallWidth, HalfWallHeight, PixelWidth),
        };

        // Tests reveal that BJ's run speed is 11.2152 tiles/sec. http://diehardwolfers.areyep.com/viewtopic.php?p=82938#82938
        // 11.2152 tiles per second * 2.4384 meters per tile = 27.34714368 meters per second
        // Walking speed is half of running speed.
        public const float RunSpeed = 27.34714368f;
        public const float WalkSpeed = 13.67357184f;
        public const float DeadZone = 0.5f;

        public static readonly QuadMesh WallMesh = new QuadMesh()
        {
            Size = new Vector2(WallWidth, WallHeight),
        };
        public static readonly BoxShape BoxShape = new BoxShape()
        {
            Extents = new Vector3(WallWidth, WallHeight, WallWidth),
        };

        public static readonly Vector3 Rotate90 = new Vector3(0, Godot.Mathf.Pi / 2f, 0);

        /// <summary>
        /// This value is used to determine how big the player's head is for collision detection
        /// </summary>
        public const float HeadXZ = PixelWidth * 3f;
        public static readonly float HeadDiagonal = Mathf.Sqrt(Mathf.Pow(HeadXZ, 2) * 2f); // Pythagorean theorem

        public static readonly float ShotRange = Mathf.Sqrt(Mathf.Pow(64 * WallWidth, 2) * 2f + Mathf.Pow(WallHeight, 2));

        public static Vector2 Vector2(Vector3 vector3) => new Vector2(vector3.x, vector3.z);
        public static Vector3 Vector3(Vector2 vector2) => new Vector3(vector2.x, 0f, vector2.y);

        public static Vector3 Axis(Vector3.Axis axis)
        {
            switch (axis)
            {
                case Godot.Vector3.Axis.X:
                    return Rotate90;
                case Godot.Vector3.Axis.Y:
                    return Godot.Vector3.Up;
                case Godot.Vector3.Axis.Z:
                default:
                    return Godot.Vector3.Zero;
            }
        }
        public static readonly Color White = Color.Color8(255, 255, 255, 255);
        #endregion Math

        #region Game assets
        public static void Load() => Load(Main.Folder);
        public static void Load(string folder, string file = "game.xml") => Load(folder, LoadXML(folder, file));

        public static void Load(string folder, XElement xml)
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
                foreach (XElement animation in actor.Elements("Animation") ?? Enumerable.Empty<XElement>())
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

            List<ushort> walls = new List<ushort>();
            foreach (XElement wall in XML.Element("VSwap")?.Element("Walls")?.Elements("Wall") ?? Enumerable.Empty<XElement>())
                walls.Add((ushort)(int)wall.Attribute("Number"));
            Walls = walls.ToArray();

            List<ushort> doors = new List<ushort>();
            foreach (XElement door in XML.Element("VSwap")?.Element("Walls")?.Elements("Door") ?? Enumerable.Empty<XElement>())
                doors.Add((ushort)(int)door.Attribute("Number"));
            Doors = doors.ToArray();

            EndStrings = XML?.Element("VgaGraph")?.Element("Menus")?.Elements("EndString")?.Select(a => a.Value)?.ToArray() ?? new string[] { "Sure you want to quit? Y/N" };
        }

        public static ushort[] Walls { get; set; }
        public static ushort[] Doors { get; set; }

        public static bool IsTrue(XElement xElement, string attribute) =>
            bool.TryParse(xElement?.Attribute(attribute)?.Value, out bool @bool) && @bool;

        public static XElement LoadXML(string folder, string file = "game.xml")
        {
            string path = System.IO.Path.Combine(folder, file);
            if (!System.IO.File.Exists(path))
                return null;
            else using (FileStream xmlStream = new FileStream(path, FileMode.Open))
                    return XElement.Load(xmlStream);
        }

        public static XElement XML { get; set; }
        public static GameMap[] Maps { get; set; }
        public static AudioT AudioT
        {
            get => audioT;
            set
            {
                audioT = value;
                if (XML.Element("VgaGraph")?.Element("Menus") is XElement menus && menus != null)
                {
                    if (menus.Attribute("SelectSound")?.Value is string selectSound && !string.IsNullOrWhiteSpace(selectSound))
                        SelectSound = Sound(selectSound);
                    if (menus.Attribute("ScrollSound")?.Value is string scrollSound && !string.IsNullOrWhiteSpace(scrollSound))
                        ScrollSound = Sound(scrollSound);
                }
            }
        }
        private static AudioT audioT;

        public static Adl SelectSound { get; set; }
        public static Adl ScrollSound { get; set; }


        public static VSwap VSwap
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
                VSwapMaterials = new SpatialMaterial[VSwapTextures.Length];
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
                DigiSounds = new AudioStreamSample[VSwap.DigiSounds.Length];
                for (uint i = 0; i < DigiSounds.Length; i++)
                    if (VSwap.DigiSounds[i] != null)
                        DigiSounds[i] = new AudioStreamSample()
                        {
                            ResourceName = (from e in XML?.Element("VSwap")?.Elements("DigiSound") ?? Enumerable.Empty<XElement>()
                                            where ushort.TryParse(e.Attribute("Number")?.Value, out ushort result) && result == i
                                            select e.Attribute("Name")?.Value).FirstOrDefault(),
                            Data = VSwap.DigiSounds[i],
                            Format = AudioStreamSample.FormatEnum.Format8Bits,
                            MixRate = 7042, // Adam Biser said 7042 Hz is the correct frequency
                        };
                if (ushort.TryParse(
                    (from e in XML.Element("VSwap").Elements("DigiSound")
                     where e.Attribute("Name")?.Value.Trim().Equals("OPENDOORSND", System.StringComparison.InvariantCultureIgnoreCase) ?? false
                     select e.Attribute("Number")?.Value).FirstOrDefault(),
                    out ushort openDoor
                    ) && openDoor < DigiSounds.Length)
                    Door.OpeningSound = DigiSounds[openDoor];
                if (ushort.TryParse(
                    (from e in XML.Element("VSwap").Elements("DigiSound")
                     where e.Attribute("Name")?.Value.Trim().Equals("CLOSEDOORSND", System.StringComparison.InvariantCultureIgnoreCase) ?? false
                     select e.Attribute("Number")?.Value).FirstOrDefault(),
                    out ushort closeDoor
                    ) && closeDoor < DigiSounds.Length)
                    Door.ClosingSound = DigiSounds[closeDoor];
            }
        }
        private static VSwap vswap;

        public static VgaGraph VgaGraph
        {
            get => vgaGraph;
            set
            {
                vgaGraph = value;
                PicTextures = new ImageTexture[VgaGraph.Pics.Length];
                for (uint i = 0; i < PicTextures.Length; i++)
                    if (VgaGraph.Pics[i] != null)
                    {
                        Godot.Image image = new Image();
                        image.CreateFromData(VgaGraph.Sizes[i][0], VgaGraph.Sizes[i][1], false, Image.Format.Rgba8, VgaGraph.Pics[i]);
                        PicTextures[i] = new ImageTexture();
                        PicTextures[i].CreateFromImage(image, 0); //(int)Texture.FlagsEnum.ConvertToLinear);
                    }
                if (XML?.Element("VgaGraph")?.Element("StatusBar") is XElement statusBar && statusBar != null)
                {
                    if (statusBar.Attribute("NumberBlank")?.Value is string numberBlank && !string.IsNullOrWhiteSpace(numberBlank))
                        StatusBarBlank = PicTextureSafe(numberBlank);
                    StatusBarDigits = new ImageTexture[10];
                    for (int x = 0; x < StatusBarDigits.Length; x++)
                        StatusBarDigits[x] = PicTextureSafe(
                            statusBar.Attribute("NumberPrefix")?.Value +
                            x.ToString() +
                            statusBar.Attribute("NumberSuffix")?.Value
                            );
                }
            }
        }
        private static VgaGraph vgaGraph;
        public static Color[] Palette;
        public static ImageTexture[] VSwapTextures;
        public static SpatialMaterial[] VSwapMaterials;
        public static Dictionary<string, uint[][]> Animations;
        public static ImageTexture[] PicTextures;
        public static AudioStreamSample[] DigiSounds;
        public static ImageTexture StatusBarBlank;
        public static ImageTexture[] StatusBarDigits;

        public static AudioStreamSample DigiSound(string name) =>
            DigiSoundSafe(name) ?? throw new InvalidDataException("DigiSound not found: \"" + name + "\"");

        public static AudioStreamSample DigiSoundSafe(string name) =>
            uint.TryParse(name, out uint index) && index < DigiSounds.Length ?
            DigiSounds[index]
            : uint.TryParse((
            from e in XML.Element("VSwap").Elements("DigiSound")
            where e.Attribute("Name")?.Value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false
            select e.Attribute("Number").Value).FirstOrDefault(),
            out uint result) && result < DigiSounds.Length ?
            DigiSounds[result]
            : null;

        public static ImageTexture PicTexture(string name) =>
            PicTextureSafe(name) ?? throw new InvalidDataException("Pic not found: \"" + name + "\"");

        public static ImageTexture PicTextureSafe(string name) =>
            uint.TryParse(name, out uint index) && index < PicTextures.Length ?
            PicTextures[index]
            : uint.TryParse((
            from e in XML.Element("VgaGraph").Elements("Pic")
            where e.Attribute("Name")?.Value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false
            select e.Attribute("Number").Value).FirstOrDefault(),
            out uint result) && result < PicTextures.Length ?
            PicTextures[result]
            : null;

        public static ImageTexture LoadingPic => PicTexture(XML.Element("VgaGraph").Attribute("LoadingPic")?.Value?.Trim());

        public static Imf[] Song(string name) =>
            uint.TryParse(name, out uint index) && index < AudioT.Songs.Length ?
            AudioT.Songs[index]
            : uint.TryParse((
            from e in XML.Element("Audio").Elements("Imf")
            where e.Attribute("Name")?.Value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false
            select e.Attribute("Number").Value).FirstOrDefault(),
            out uint result) && result < AudioT.Songs.Length ?
            AudioT.Songs[result]
            : throw new InvalidDataException("Song not found: \"" + name + "\"");

        public static Adl Sound(string name) =>
            uint.TryParse(name, out uint index) && index < AudioT.Sounds.Length ?
            AudioT.Sounds[index]
            : uint.TryParse((
            from e in XML.Element("Audio").Elements("Sound")
            where e.Attribute("Name")?.Value?.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false
            select e.Attribute("Number").Value).FirstOrDefault(),
            out uint result) && result < AudioT.Sounds.Length ?
            AudioT.Sounds[result]
            : throw new InvalidDataException("Sound not found: \"" + name + "\"");

        public static VgaGraph.Font Font(uint font) => VgaGraph.Fonts[Direction8.Modulus((int)font, VgaGraph.Fonts.Length)];
        public static ImageTexture Text(string @string, uint font = 0, ushort padding = 0) => Text(Font(font), @string, padding);
        public static ImageTexture Text(VgaGraph.Font font, string @string = "", ushort padding = 0)
        {
            Image image = new Image();
            image.CreateFromData(font.CalcWidth(@string), font.CalcHeight(@string, padding), false, Image.Format.Rgba8, font.Text(@string, padding));
            ImageTexture imageTexture = new ImageTexture();
            imageTexture.CreateFromImage(image, 0);
            return imageTexture;
        }

        public static VgaGraph.Font ModalFont =>
            Font(uint.TryParse(XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("Font")?.Value, out uint font) ? font : 0);

        public static string[] EndStrings;

        public static MenuScreen Menu(string name) =>
            (from e in XML.Element("VgaGraph").Element("Menus").Elements("Menu")
             where e.Attribute("Name").Value.Equals(name, System.StringComparison.InvariantCultureIgnoreCase)
             select e).FirstOrDefault() is XElement screen && screen != null ?
            new MenuScreen(screen)
            : null;

        /*
        public static ShaderMaterial ShaderMaterial = new ShaderMaterial()
        {
            Shader = new Shader()
            {
                Code = @"
shader_type canvas_item;
uniform vec4 base : hint_color;
void fragment()
{
    COLOR.rgb = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0).rgb + dot(base.rgb * base.a);
}
",
            },
        };
        */
        #endregion Game assets
    }
}
