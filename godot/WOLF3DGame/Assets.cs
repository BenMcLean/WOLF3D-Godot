using Godot;
using RectpackSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WOLF3D.WOLF3DGame.Action;
using WOLF3D.WOLF3DGame.Menu;
using WOLF3DModel;
using static WOLF3DModel.AudioT;

namespace WOLF3D.WOLF3DGame
{
	/// <summary>
	/// Assets takes the bytes extracted from the Wolfenstein 3-D files and creates the corresponding Godot objects for them to be used throughout the game.
	/// </summary>
	public static class Assets
	{
		#region Math
		// Tom Hall's Doom Bible and also tweets from John Carmack state that the walls in Wolfenstein 3-D were always eight feet thick. The wall textures are 64x64 pixels, which means that the ratio is 8 pixels per foot.
		// However, VR uses the metric system, where 1 game unit is 1 meter in real space. One foot equals 0.3048 meters.
		public const float Foot = 0.3048f;
		public const float Inch = Foot / 12f;
		// Now, unless I am a complete failure at basic math, (quite possible) this means that to scale Wolfenstein 3-D correctly in VR, one pixel must equal 0.0381 in meters, and a Wolfenstein 3-D wall must be 2.4384 meters thick.
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
		// Wolfenstein 3-D ran in SVGA screen mode 13h, which has a 320x200 resolution in a 4:3 aspect ratio.
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
		// Wolfenstein 3-D counts time as "tics" which varies by framerate.
		// We don't want to vary, so 1 second = 70 tics, regardless of framerate.
		public const float TicsPerSecond = 70f;
		public const float Tic = 1f / TicsPerSecond;
		public static float TicsToSeconds(int tics) => tics / TicsPerSecond;
		public static short SecondsToTics(float seconds) => (short)(seconds * TicsPerSecond);
		// 1 Zenos is 1 / 65536th of a WallWidth, used by Wolfenstein 3-D to measure how far an actor is off center from their square. This number comes from the size of a 16-bit integer.
		// 65536 Zenos per wall / 512 guard speed = 128 tics per wall
		// 128 tics per wall / 70 tics per second = 1.828571428571429 seconds per wall
		// 2.4384 meters per wall / 1.828571428571429 seconds per wall = 1.3335 meters per second
		// 70 tics per second * 2.4384 meters per wall / 65536 Zenos per wall = 0.0026044921875 (meters * tic) / (Zenos * second)
		// Check: 512 guard speed * 1 second delta * 0.0026044921875 ActorSpeedConversion = 1.3335 meters per second
		public const float ActorSpeedConversion = TicsPerSecond * WallWidth / 65536f; // 0.0026044921875

		// Tests reveal that BJ's run speed is 11.2152 tiles/sec. http://diehardwolfers.areyep.com/viewtopic.php?p=82938#82938
		// 11.2152 tiles per second * 2.4384 meters per tile = 27.34714368 meters per second
		// Walking speed is half of running speed.
		public const float RunSpeed = 27.34714368f;
		public const float WalkSpeed = 13.67357184f;
		public const float DeadZone = 0.5f;
		public const float HalfPi = Mathf.Pi / 2f;
		public const float QuarterPi = Mathf.Pi / 4f;
		public static readonly QuadMesh WallMesh = new QuadMesh()
		{
			Size = new Vector2(WallWidth, WallHeight),
		};
		public static readonly BoxShape BoxShape = new BoxShape()
		{
			Extents = new Vector3(HalfWallWidth, HalfWallHeight, HalfWallWidth),
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
		public static readonly Godot.Color White = Godot.Color.Color8(255, 255, 255, 255);
		public static uint GetUInt(string @string)
		{
			if (string.IsNullOrWhiteSpace(@string))
				return 0;
			if (@string.Split(',') is string[] ranges && ranges.Length > 1)
				@string = ranges.Random();
			return @string.Split('-') is string[] values
				&& values.Length == 2
				&& uint.TryParse(values[0], out uint min)
				&& uint.TryParse(values[1], out uint max) ?
				(uint)Main.RNG.Next((int)min, (int)max)
				: uint.TryParse(@string, out uint value) ?
				value
				: 0;
		}
		#endregion Math
		#region Game assets
		public static void Load() => Load(Main.Folder);
		public static void Load(string folder, string file = "game.xml") => Load(folder, LoadXML(folder, file));
		/// <summary>
		/// Warning: Does not clear types that aren't nullable!
		/// </summary>
		public static void Clear()
		{
			XML = null;
			Palettes = null;
			VSwapTextures = null;
			VSwapMaterials = null;
			VgaGraphTextures = null;
			DigiSounds = null;
			//StatusBarBlank = null;
			//StatusBarDigits = null;
			BitmapFonts = null;
			Maps = null;
			SelectSound = null;
			ScrollSound = null;
			Walls = null;
			Doors = null;
			Elevators = null;
			PushWalls = null;
			States?.Clear();
			Turns?.Clear();
			EndStrings = null;
			FloorCodeFirst = 0;
			FloorCodes = 0;
		}
		public static void Load(string folder, XElement xml, bool limitedLoad = false)
		{
			Clear();
			XML = xml;
			EndStrings = XML?.Element("VgaGraph")?.Element("Menus")?.Elements("EndString")?.Select(a => a.Value)?.ToArray() ?? new string[] { "Sure you want to quit? Y/N" };
			if (XML.Element("VgaGraph") != null)
				VgaGraph = VgaGraph.Load(folder, XML);
			if (limitedLoad)
			{
				SetPalettes(VSwap.LoadPalettes(xml).ToArray());
				PackAtlas(VgaGraph, null, xml);
			}
			else
			{
				if (XML.Element("VSwap") != null)
					VSwap = VSwap.Load(folder, XML);
				else
					SetPalettes(VSwap.LoadPalettes(xml).ToArray());
				PackAtlas(VgaGraph, VSwap, xml);
				if (XML.Element("Maps") != null)
					Maps = GameMap.Load(folder, XML);
				Walls = XML.Element("VSwap")?.Element("Walls")?.Elements("Wall").Select(e => ushort.Parse(e.Attribute("Number").Value)).ToArray();
				Doors = XML.Element("VSwap")?.Element("Walls")?.Elements("Door")?.Select(e => ushort.Parse(e.Attribute("Number").Value))?.ToArray();
				Elevators = XML.Element("VSwap")?.Element("Walls")?.Elements("Elevator")?.Select(e => ushort.Parse(e.Attribute("Number").Value))?.ToArray();
				PushWalls = PushWall?.Select(e => ushort.Parse(e.Attribute("Number").Value))?.ToArray();
				States.Clear();
				foreach (XElement xState in XML?.Element("VSwap")?.Element("Objects")?.Elements("State") ?? Enumerable.Empty<XElement>())
					States.Add(xState.Attribute("Name").Value, new State(xState));
				foreach (State state in States.Values)
					if (state.XML.Attribute("Next")?.Value is string next)
						state.Next = States[next];
				Turns.Clear();
				foreach (XElement xTurn in XML?.Element("VSwap")?.Element("Objects")?.Elements("Turn") ?? Enumerable.Empty<XElement>())
					Turns.Add((ushort)(int)xTurn.Attribute("Number"), Direction8.From(xTurn.Attribute("Direction")));
				if (ushort.TryParse(XML?.Element("VSwap")?.Element("Walls")?.Attribute("FloorCodeFirst")?.Value, out ushort floorCodeFirst))
					FloorCodeFirst = floorCodeFirst;
				if (ushort.TryParse(XML?.Element("VSwap")?.Element("Walls")?.Attribute("FloorCodeLast")?.Value, out ushort floorCodeLast))
					FloorCodes = (ushort)(1 + floorCodeLast - FloorCodeFirst);
			}
			if (XML.Element("Audio") is XElement audio)
			{
				AudioT = AudioT.Load(folder, XML);
				// Load "extra" IMF/WLF files not included in AudioT
				Godot.File file = new Godot.File();
				foreach (XElement songXML in audio.Elements("Imf")?.Where(e => e.Attribute("File") is XAttribute))
					if (file.Open(songXML.Attribute("File").Value, Godot.File.ModeFlags.Read) == Godot.Error.Ok && file.IsOpen())
					{
						byte[] bytes = file.GetBuffer((int)file.GetLen());
						file.Close();
						AudioT.Songs.Add(songXML?.Attribute("Name")?.Value, new Song()
						{
							Name = songXML?.Attribute("Name")?.Value,
							Bytes = bytes,
							Imf = Imf.ReadImf(new MemoryStream(bytes)),
						});
					}
			}
		}
		public static ushort[] Walls { get; set; }
		public static ushort[] Doors { get; set; }
		public static ushort[] Elevators { get; set; }
		public static ushort[] PushWalls { get; set; }
		public static ushort FloorCodeFirst = 107;
		public static ushort FloorCodes = 36;
		public static readonly Dictionary<ushort, Direction8> Turns = new Dictionary<ushort, Direction8>();
		public static XElement LoadXML(string folder, string file = "game.xml")
		{
			string path = System.IO.Path.Combine(folder, file);
			if (!System.IO.Directory.Exists(folder) || !System.IO.File.Exists(path))
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
				if (XML?.Element("VgaGraph")?.Element("Menus") is XElement menus && menus != null)
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
		public static void SetPalettes(int[][] palettes)
		{
			Palettes = new Godot.Color[palettes.Length][];
			for (uint x = 0; x < Palettes.Length; x++)
			{
				Palettes[x] = new Godot.Color[palettes[x].Length];
				for (uint y = 0; y < Palettes[x].Length; y++)
					Palettes[x][y] = Godot.Color.Color8(
							palettes[x][y].R(),
							palettes[x][y].G(),
							palettes[x][y].B(),
							palettes[x][y].A()
						);
			}
		}
		public static Godot.Color[][] Palettes;
		public static VSwap VSwap
		{
			get => vswap;
			set
			{
				vswap = value;
				SetPalettes(VSwap.Palettes);
				VSwapTextures = new ImageTexture[VSwap.SoundPage];
				VSwapMaterials = new SpatialMaterial[VSwapTextures.Length];
				int scale = ushort.TryParse(XML?.Element("VSwap")?.Attribute("Scale")?.Value, out ushort shortScale) ? shortScale : 1;
				int side = (ushort.TryParse(XML?.Element("VSwap")?.Attribute("Sqrt")?.Value, out ushort tileSqrt) ? tileSqrt : 64) * scale;
				uint textureFlags = (uint)(
						Texture.FlagsEnum.ConvertToLinear |
						Texture.FlagsEnum.AnisotropicFilter |
						Texture.FlagsEnum.Repeat
					);
				if (!XML?.Element("VSwap")?.IsFalse("MipMaps") ?? false)
					textureFlags |= (uint)Texture.FlagsEnum.Mipmaps;
				for (uint i = 0; i < VSwapTextures.Length; i++)
					if (VSwap.Pages[i] != null)
					{
						Godot.Image image = new Godot.Image();
						image.CreateFromData(side, side, false, Godot.Image.Format.Rgba8, VSwap.Pages[i].Upscale(scale, scale));
						VSwapTextures[i] = new ImageTexture();
						VSwapTextures[i].CreateFromImage(image, textureFlags);
						VSwapMaterials[i] = new SpatialMaterial()
						{
							AlbedoTexture = VSwapTextures[i],
							FlagsUnshaded = true,
							FlagsDoNotReceiveShadows = true,
							FlagsDisableAmbientLight = true,
							FlagsTransparent = i >= VSwap.SpritePage,
							ParamsUseAlphaScissor = true,
							ParamsAlphaScissorThreshold = 0.5f,
							ParamsCullMode = i >= VSwap.SpritePage ? SpatialMaterial.CullMode.Back : SpatialMaterial.CullMode.Disabled,
							ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
							AnisotropyEnabled = true,
							RenderPriority = 1,
						};
					}
				DigiSounds = new AudioStreamSample[VSwap.DigiSounds.Length];
				for (uint i = 0; i < DigiSounds.Length; i++)
					if (VSwap.DigiSounds[i] != null)
						DigiSounds[i] = new AudioStreamSample()
						{
							ResourceName = XML?.Element("VSwap")?.Elements("DigiSound")
								?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort result) && result == i)
								?.FirstOrDefault()?.Attribute("Name")?.Value,
							Data = VSwap.DigiSounds[i],
							Format = AudioStreamSample.FormatEnum.Format8Bits,
							MixRate = 7042, // Adam Biser said 7042 Hz is the correct frequency
						};
			}
		}
		private static VSwap vswap;
		public static VgaGraph VgaGraph
		{
			get => vgaGraph;
			set
			{
				vgaGraph = value;
				//VgaGraphTextures = new ImageTexture[VgaGraph.Pics.Length];
				//for (uint i = 0; i < VgaGraphTextures.Length; i++)
				//	if (VgaGraph.Pics[i] != null)
				//	{
				//		Godot.Image image = new Image();
				//		image.CreateFromData(VgaGraph.Sizes[i][0], VgaGraph.Sizes[i][1], false, Image.Format.Rgba8, VgaGraph.Pics[i]);
				//		VgaGraphTextures[i] = new ImageTexture();
				//		VgaGraphTextures[i].CreateFromImage(image, 0); //(int)Texture.FlagsEnum.ConvertToLinear);
				//	}
				//if (XML?.Element("VgaGraph")?.Element("StatusBar") is XElement statusBar && statusBar != null)
				//{
				//	StatusBarDigits = new AtlasTexture[10];
				//	for (int x = 0; x < StatusBarDigits.Length; x++)
				//		StatusBarDigits[x] = PicTextureSafe(
				//			statusBar.Attribute("NumberPrefix")?.Value +
				//			x.ToString() +
				//			statusBar.Attribute("NumberSuffix")?.Value
				//			);
				//	StatusBarBlank = statusBar.Attribute("NumberBlank")?.Value is string numberBlank && !string.IsNullOrWhiteSpace(numberBlank) ?
				//		PicTextureSafe(numberBlank) ?? StatusBarDigits[0]
				//		: StatusBarDigits[0];
				//}
				//if (ushort.TryParse(XML?.Element("VgaGraph")?.Element("Sizes")?.Attribute("BitmapFonts")?.Value, out ushort bitmaps))
				//{
				//	BitmapFonts = new BitmapFont[bitmaps];
				//	for (ushort i = 0; i < bitmaps; i++)
				//	{
				//		BitmapFonts[i] = new BitmapFont();
				//		ushort letters = 0;
				//		foreach (XElement letter in XML?.Element("VgaGraph")?.Elements("Pic").Where(e => ushort.TryParse(e.Attribute("BitmapFont")?.Value, out ushort number) && number == i) ?? Enumerable.Empty<XElement>())
				//		{
				//			AtlasTexture texture = VgaGraphTextures[(uint)letter.Attribute("Number")];
				//			BitmapFonts[i].AddTexture(texture);
				//			BitmapFonts[i].AddChar(
				//				letter.Attribute("Character").Value[0],
				//				letters++,
				//				new Rect2()
				//				{
				//					Size = texture.GetSize(),
				//				}
				//			);
				//		}
				//		if (XML?.Element("VgaGraph")?.Elements("Space").Where(e => ushort.TryParse(e.Attribute("BitmapFont")?.Value, out ushort spaceFont) && spaceFont == i).FirstOrDefault() is XElement space)
				//		{
				//			uint width = (uint)space.Attribute("Width"),
				//				height = (uint)space.Attribute("Height");
				//			byte[] bytes = new byte[width * height * 4];
				//			if (ushort.TryParse(space.Attribute("Color")?.Value, out ushort index) && index < VgaGraph.Palettes[0].Length)
				//			{
				//				byte[] color = new byte[] {
				//					(byte)(VgaGraph.Palettes[0][index] >> 24),
				//					(byte)(VgaGraph.Palettes[0][index] >> 16),
				//					(byte)(VgaGraph.Palettes[0][index] >> 8),
				//					(byte)VgaGraph.Palettes[0][index]
				//				};
				//				for (uint x = 0; x < bytes.Length; x += 4)
				//					System.Array.Copy(color, 0, bytes, x, 4);
				//			}
				//			Godot.Image spaceImage = new Image();
				//			spaceImage.CreateFromData((int)width, (int)height, false, Image.Format.Rgba8, bytes);
				//			ImageTexture spaceTexture = new ImageTexture();
				//			spaceTexture.CreateFromImage(spaceImage, 0);
				//			BitmapFonts[i].AddTexture(spaceTexture);
				//			BitmapFonts[i].AddChar(
				//				' ',
				//				letters++,
				//				new Rect2()
				//				{
				//					Size = spaceTexture.GetSize(),
				//				}
				//			);
				//		}
				//	}
				//}
			}
		}
		private static VgaGraph vgaGraph;
		public static void PackAtlas(VgaGraph? vgaGraph, VSwap? vSwap, XElement xml = null)
		{
			PackingRectangle[] rectangles = PackingRectangles(vgaGraph, vSwap, xml).ToArray();
			RectanglePacker.Pack(rectangles, out PackingRectangle bounds, PackingHints.TryByBiggerSide);
			int atlasSize = (int)TextureMethods.NextPowerOf2(bounds.BiggerSide);
			byte[] bin = new byte[atlasSize * 4 * atlasSize];
			foreach (PackingRectangle rectangle in rectangles)
				if (TryTextureFromId(rectangle.Id, out byte[] texture, out int width, out int height, vgaGraph, vSwap))
					bin.DrawInsert((int)rectangle.X + 1, (int)rectangle.Y + 1, texture, width, atlasSize)
						.DrawPadding((int)rectangle.X + 1, (int)rectangle.Y + 1, width, height, atlasSize);
			int total = (vSwap is VSwap vs2 ? vs2.SoundPage : 0)
				+ (vgaGraph is VgaGraph vg2 ? vg2.Pics.Length + vg2.Fonts.Select(f => f.Character.Length).Sum() : 0),
				spaceNumber = 0;
			for (; spaceNumber < rectangles.Length && rectangles[spaceNumber].Id != total; spaceNumber++) { }
			if (spaceNumber < rectangles.Length)
				foreach (XElement fontXml in xml?.Element("VgaGraph")?.Elements("Font")?.Where(e => !string.IsNullOrWhiteSpace(e.Attribute("SpaceColor")?.Value)))
					if (rectangles[spaceNumber++] is PackingRectangle rectangle)
						bin.DrawRectangle(0, (int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, atlasSize); //TODO: fix color
			AtlasImage = new Godot.Image();
			AtlasImage.CreateFromData(atlasSize, atlasSize, false, Godot.Image.Format.Rgba8, bin);
			AtlasImageTexture = new ImageTexture();
			uint textureFlags = (uint)(
				Texture.FlagsEnum.ConvertToLinear |
				Texture.FlagsEnum.AnisotropicFilter |
				Texture.FlagsEnum.Repeat
				);
			//if (!XML?.Element("VSwap")?.IsFalse("MipMaps") ?? false)
			//	textureFlags |= (uint)Texture.FlagsEnum.Mipmaps;
			AtlasImageTexture.CreateFromImage(AtlasImage, textureFlags);
			int rectIndex = 0;
			if (vSwap is VSwap vs)
			{
				VSwapAtlasTextures = new AtlasTexture[vs.SoundPage];
				for (int i = 0; i < VSwapAtlasTextures.Length; i++)
					if (VSwap.Pages[i] != null)
						VSwapAtlasTextures[i] = new AtlasTexture()
						{
							Atlas = AtlasImageTexture,
							Region = new Rect2(rectangles[i].X + 1, rectangles[i].Y + 1, rectangles[i].Width - 2, rectangles[i].Height - 2),
						};
				rectIndex += vs.SoundPage;
			}
			if (vgaGraph is VgaGraph vg)
			{
				VgaGraphTextures = new AtlasTexture[vg.Pics.Length];
				for (int i = 0; i < vg.Pics.Length; i++)
					if (vg.Pics[i] != null && rectangles.Where(r => r.Id == rectIndex + i).FirstOrDefault() is PackingRectangle rectangle)
						VgaGraphTextures[i] = new AtlasTexture()
						{
							Atlas = AtlasImageTexture,
							Region = new Rect2(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, rectangle.Height - 2),
						};
				rectIndex += vg.Pics.Length;
				BitmapFonts = new BitmapFont[XML?.Element("VgaGraph")?.Elements("Font")?.Count() ?? vg.Fonts.Length];
				int fontNumber = 0;
				for (; fontNumber < vg.Fonts.Length; fontNumber++)
				{
					VgaGraph.Font font = vg.Fonts[fontNumber];
					BitmapFonts[fontNumber] = new BitmapFont()
					{
						Height = font.Height,
						Fallback = null,
					};
					BitmapFonts[fontNumber].AddTexture(AtlasImageTexture);
					for (int c = 0; c < font.Character.Length; c++)
						if (font.Character[c] != null && rectangles.Where(r => r.Id == rectIndex + c).FirstOrDefault() is PackingRectangle rectangle)
							BitmapFonts[fontNumber].AddChar(
								character: (char)c,
								texture: 0,
								rect: new Rect2(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, rectangle.Height - 2)
								);
					rectIndex += font.Character.Length;
				}
				for (; fontNumber < BitmapFonts.Length; fontNumber++)
					if (XML?.Element("VgaGraph")?.Elements("Font")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort f) && f == fontNumber)?.FirstOrDefault()?.Attribute("Prefix")?.Value is string prefix)
					{
						BitmapFonts[fontNumber] = new BitmapFont();
						BitmapFonts[fontNumber].AddTexture(AtlasImageTexture);
						foreach (XElement pic in XML?.Element("VgaGraph")?.Elements("Pic")?.Where(e => e?.Attribute("Name")?.Value?.StartsWith(prefix) ?? false))
							if (pic.Attribute("Character")?.Value is string characterString && characterString.Length > 0 && characterString[0] is char c
								&& ushort.TryParse(pic.Attribute("Number")?.Value, out ushort number) && VgaGraphTextures[number].Region is Rect2 region)
								BitmapFonts[fontNumber].AddChar(
									character: c,
									texture: 0,
									rect: region
									);
						foreach (XElement font in xml?.Element("VgaGraph")?.Elements("Font")?.Where(e => e?.Attribute("SpaceColor")?.Value?.StartsWith(prefix) ?? false))
							if (rectangles.Where(r => r.Id == rectIndex).FirstOrDefault() is PackingRectangle rectangle)
							{
								BitmapFonts[int.Parse(font.Attribute("Number").Value)].AddChar(
									character: ' ',
									texture: 0,
									rect: new Rect2(rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, rectangle.Height - 2)
									);
								rectIndex++;
							}
					}
			}
		}
		public static IEnumerable<PackingRectangle> PackingRectangles(VgaGraph? vgaGraph = null, VSwap? vSwap = null, XElement xml = null)
		{
			int total = (vSwap is VSwap vs ? vs.SoundPage : 0)
				+ (vgaGraph is VgaGraph vg ? vg.Pics.Length + vg.Fonts.Select(f => f.Character.Length).Sum() : 0),
				i = 0;
			for (; i < total; i++)
				if (TryTextureFromId(i, out byte[] _, out int width, out int height, vgaGraph, vSwap))
					yield return new PackingRectangle(0, 0, (uint)width + 2u, (uint)height + 2u, i);
			foreach (XElement font in xml?.Element("VgaGraph")?.Elements("Font")?.Where(e => !string.IsNullOrWhiteSpace(e.Attribute("SpaceColor")?.Value)))
				yield return new PackingRectangle(0, 0, uint.Parse(font.Attribute("SpaceWidth").Value) + 2u, uint.Parse(font.Attribute("SpaceHeight").Value) + 2u, ++i);
		}
		public static bool TryTextureFromId(int id, out byte[] texture, out int width, out int height, VgaGraph? vgaGraph = null, VSwap? vSwap = null)
		{
			texture = null;
			width = height = 0;
			if (vSwap is VSwap vs)
			{
				if (id < vs.SoundPage)
				{
					if (vs.Pages[id] == null)
						return false;
					texture = vs.Pages[id];
					width = height = vs.TileSqrt;
					return true;
				}
				id -= vs.SoundPage;
			}
			if (!(vgaGraph is VgaGraph vg))
				return false;
			if (id < vg.Pics.Length)
			{
				if (vg.Pics[id] == null)
					return false;
				texture = vg.Pics[id];
				width = vg.Sizes[id][0];
				height = vg.Sizes[id][1];
				return true;
			}
			id -= vg.Pics.Length;
			foreach (VgaGraph.Font font in vg.Fonts)
				if (id < font.Character.Length)
				{
					if (font.Character[id] == null)
						return false;
					texture = font.Character[id];
					width = font.Width[id];
					height = font.Height;
					return true;
				}
				else
					id -= font.Character.Length;
			return false;
		}
		public static Godot.Image AtlasImage;
		public static ImageTexture AtlasImageTexture;
		public static AtlasTexture[] VSwapAtlasTextures;
		public static AtlasTexture[] VgaGraphTextures;
		public static ImageTexture[] VSwapTextures;
		public static SpatialMaterial[] VSwapMaterials;
		public static AudioStreamSample[] DigiSounds;
		public static BitmapFont[] BitmapFonts;
		public static short? Shape(string @string) =>
			short.TryParse(@string, out short shape) ? shape :
			short.TryParse(XML?.Element("VSwap")?.Element("Sprites")?.Elements("Sprite")
				?.Where(e => e.Attribute("Name")?.Value?.Equals(@string, StringComparison.InvariantCultureIgnoreCase) ?? false)
				?.FirstOrDefault()?.Attribute("Page")?.Value, out shape) ?
				shape
				: (short?)null;
		public static AudioStreamSample DigiSound(string name) =>
			DigiSoundSafe(name) ?? throw new InvalidDataException("DigiSound not found: \"" + name + "\"");
		public static AudioStreamSample DigiSoundSafe(string name) => DigiOneSoundSafe(name is string && name.Contains(',') ? name.Split(',').Random() : name);
		public static AudioStreamSample DigiOneSoundSafe(string name) =>
			uint.TryParse(name, out uint index) && index < DigiSounds.Length ?
			DigiSounds[index]
			: uint.TryParse((
			from e in XML?.Element("VSwap")?.Element("DigiSounds")?.Elements("DigiSound") ?? Enumerable.Empty<XElement>()
			where e.Attribute("Name")?.Value?.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false
			select e.Attribute("Number")?.Value).FirstOrDefault(),
			out uint result) && result < DigiSounds.Length ?
			DigiSounds[result]
			: null;
		public static AtlasTexture PicTexture(string name) =>
			PicTextureSafe(name) ?? throw new InvalidDataException("Pic not found: \"" + name + "\"");
		public static AtlasTexture PicTextureSafe(string name) =>
			uint.TryParse(name, out uint index) && index < VgaGraphTextures.Length ?
			VgaGraphTextures[index]
			: uint.TryParse(
				XML?.Element("VgaGraph")?.Elements("Pic")
				?.Where(e => e.Attribute("Name")?.Value?.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false)
				?.FirstOrDefault()
				?.Attribute("Number")?.Value,
				out uint result) && result < VgaGraphTextures.Length ?
			VgaGraphTextures[result]
			: null;
		public static AtlasTexture LoadingPic => PicTexture(XML?.Element("VgaGraph")?.Attribute("LoadingPic")?.Value?.Trim());
		public static Adl Sound(string name) => SoundSafe(name) ?? throw new InvalidDataException("Sound not found: \"" + name + "\"");
		public static Adl SoundSafe(string name) =>
			uint.TryParse(name, out uint index) && index < AudioT.Sounds.Length ?
			AudioT.Sounds[index]
			: uint.TryParse((
			from e in XML?.Element("Audio")?.Elements("Sound") ?? Enumerable.Empty<XElement>()
			where e.Attribute("Name")?.Value?.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false
			select e.Attribute("Number")?.Value).FirstOrDefault(),
			out uint result) && result < AudioT.Sounds.Length ?
			AudioT.Sounds[result]
			: null;
		public static BitmapFont Font(uint font) => BitmapFonts[Direction8.Modulus((int)font, BitmapFonts.Length)];
		public static ImageTexture Text(string @string, uint font = 0, ushort padding = 0) => Text(vgaGraph.Fonts[font], @string, padding);
		public static ImageTexture Text(VgaGraph.Font font, string @string = "", ushort padding = 0)
		{
			Godot.Image image = new Godot.Image();
			image.CreateFromData(font.CalcWidth(@string), font.CalcHeight(@string, padding), false, Godot.Image.Format.Rgba8, font.Text(@string, padding));
			ImageTexture imageTexture = new ImageTexture();
			imageTexture.CreateFromImage(image, 0);
			return imageTexture;
		}
		public static BitmapFont ModalFont =>
	Font(uint.TryParse(XML?.Element("VgaGraph")?.Element("Menus")?.Attribute("Font")?.Value, out uint font) ? font : 0);
		public static string[] EndStrings;
		public static MenuScreen Menu(string name) =>
			XML?.Element("VgaGraph")?.Element("Menus")?.Elements("Menu")
			?.Where(e => e.Attribute("Name")?.Value?.Equals(name, System.StringComparison.InvariantCultureIgnoreCase) ?? false)
			?.FirstOrDefault() is XElement screen && screen != null ?
				new MenuScreen(screen)
				: null;
		public static string WallName(ushort wall) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall")
			?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort w) && w == wall)
			?.FirstOrDefault()?.Attribute("Name")?.Value;
		public static IEnumerable<XElement> Treasures =>
			XML?.Element("VSwap")?.Element("Objects")?.Elements("Pickup")?.Where(e => e.IsTrue("Treasure"));
		public static uint Treasure(GameMap map) => Treasure(map.ObjectData);
		public static uint Treasure(ushort[] ObjectData)
		{
			uint found = 0;
			foreach (XElement treasure in Treasures ?? Enumerable.Empty<XElement>())
				if (ushort.TryParse(treasure.Attribute("Number")?.Value, out ushort number))
					foreach (ushort square in ObjectData ?? Enumerable.Empty<ushort>())
						if (number == square)
							found++;
			return found;
		}
		public static IEnumerable<XElement> Spawn =>
			XML?.Element("VSwap")?.Element("Objects")?.Elements("Spawn");
		public static uint Spawns(GameMap map) => Spawns(map.ObjectData);
		public static uint Spawns(ushort[] ObjectData)
		{
			uint found = 0;
			foreach (XElement spawn in Spawn ?? Enumerable.Empty<XElement>())
				if (ushort.TryParse(spawn.Attribute("Number")?.Value, out ushort number))
					foreach (ushort square in ObjectData ?? Enumerable.Empty<ushort>())
						if (number == square)
							found++;
			return found;
		}
		public static IEnumerable<XElement> PushWall =>
	XML?.Element("VSwap")?.Element("Objects")?.Elements("Pushwall");
		public static uint CountPushWalls(GameMap map) => CountPushWalls(map.ObjectData);
		public static uint CountPushWalls(ushort[] ObjectData)
		{
			uint found = 0;
			foreach (XElement pushwall in PushWall ?? Enumerable.Empty<XElement>())
				if (ushort.TryParse(pushwall.Attribute("Number")?.Value, out ushort number))
					foreach (ushort square in ObjectData ?? Enumerable.Empty<ushort>())
						if (number == square)
							found++;
			return found;
		}
		public static XElement Wall(ushort number) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Wall")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort wall) && wall == number)?.FirstOrDefault();
		public static XElement Elevator(ushort number) => XML?.Element("VSwap")?.Element("Walls")?.Elements("Elevator")?.Where(e => ushort.TryParse(e.Attribute("Number")?.Value, out ushort elevator) && elevator == number)?.FirstOrDefault();
		public readonly static Dictionary<string, State> States = new Dictionary<string, State>();
		public static bool IsNavigable(ushort mapData, ushort objectData) =>
			IsTransparent(mapData, objectData) && (
				!(XML?.Element("VSwap")?.Element("Objects").Elements("Billboard")
					.Where(e => uint.TryParse(e.Attribute("Number")?.Value, out uint number) && number == objectData).FirstOrDefault() is XElement mapObject)
				|| mapObject.IsTrue("Walk")
			);
		public static bool IsTransparent(ushort mapData, ushort objectData) =>
			(!Walls.Contains(mapData) || PushWalls.Contains(objectData))
			&& !Elevators.Contains(mapData);
		public static GameMap? NextMap(GameMap previous) => GetMap(previous.Episode, previous.ElevatorTo);
		public static GameMap? GetMap(byte episode, byte floor) => Maps.Where(e => e.Episode == episode && e.Floor == floor).FirstOrDefault();
		public static bool Start(GameMap map, out ushort index, out Direction8 direction)
		{
			foreach (XElement start in XML?.Element("VSwap")?.Elements("Objects")?.Elements("Start") ?? Enumerable.Empty<XElement>())
				if (ushort.TryParse(start.Attribute("Number")?.Value, out ushort find)
					&& Array.FindIndex(map.ObjectData, o => o == find) is int found
					&& found > -1)
				{
					index = (ushort)found;
					direction = Direction8.From(start.Attribute("Direction"));
					return true;
				}
			index = 0;
			direction = null;
			return false;
		}
		public static Transform StartTransform(GameMap map) =>
			Start(map, out ushort index, out Direction8 direction) ?
			new Transform(direction.Basis, new Vector3(CenterSquare(map.X(index)), 0f, CenterSquare(map.Z(index))))
			: throw new InvalidDataException("Could not find start of \"" + map.Name + "\"!");
		#endregion Game assets
	}
}
