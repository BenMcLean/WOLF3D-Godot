using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WOLF3DModel
{
	public struct VSwap
	{
		public static VSwap Load(string folder, XElement xml)
		{
			using (FileStream vSwap = new FileStream(System.IO.Path.Combine(folder, xml.Element("VSwap").Attribute("Name").Value), FileMode.Open))
				return new VSwap(xml, LoadPalettes(xml).ToArray(), vSwap,
					ushort.TryParse(xml?.Element("VSwap")?.Attribute("Sqrt")?.Value, out ushort tileSqrt) ? tileSqrt : (ushort)64
					);
		}

		public int[][] Palettes { get; set; }
		public byte[][] Pages { get; set; }
		public byte[][] DigiSounds { get; set; }
		public ushort SpritePage { get; set; }
		public ushort NumPages { get; set; }
		public int SoundPage => Pages.Length;
		public ushort TileSqrt { get; set; }

		public byte[] Sprite(ushort number) => Pages[SpritePage + number];

		public static uint GetOffset(ushort x, ushort y, ushort tileSqrt = 64) => (uint)((tileSqrt * y + x) * 4);
		public uint GetOffset(ushort x, ushort y) => GetOffset(x, y, TileSqrt);
		public byte GetR(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y)];
		public byte GetG(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y) + 1];
		public byte GetB(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y) + 2];
		public byte GetA(ushort page, ushort x, ushort y) => Pages[page][GetOffset(x, y) + 3];
		public bool IsTransparent(ushort page, ushort x, ushort y) =>
			page >= Pages.Length
			|| Pages[page] == null
			|| (page >= SpritePage // We know walls aren't transparent
			&& GetOffset(x, y) + 3 is uint offset
			&& offset < Pages[page].Length
			&& Pages[page][offset] > 128);

		public VSwap(XElement xml, int[][] palettes, Stream stream, ushort tileSqrt = 64)
		{
			Palettes = palettes;
			TileSqrt = tileSqrt;
			if (Palettes == null || Palettes.Length < 1)
				throw new InvalidDataException("Must load a palette before loading a VSWAP!");
			using (BinaryReader binaryReader = new BinaryReader(stream))
			{
				// parse header info
				NumPages = binaryReader.ReadUInt16();
				SpritePage = binaryReader.ReadUInt16();
				Pages = new byte[binaryReader.ReadUInt16()][]; // SoundPage

				uint[] pageOffsets = new uint[NumPages];
				uint dataStart = 0;
				for (ushort i = 0; i < pageOffsets.Length; i++)
				{
					pageOffsets[i] = binaryReader.ReadUInt32();
					if (i == 0)
						dataStart = pageOffsets[0];
					if ((pageOffsets[i] != 0 && pageOffsets[i] < dataStart) || pageOffsets[i] > stream.Length)
						throw new InvalidDataException("VSWAP contains invalid page offsets.");
				}
				ushort[] pageLengths = new ushort[NumPages];
				for (ushort i = 0; i < pageLengths.Length; i++)
					pageLengths[i] = binaryReader.ReadUInt16();

				ushort page;
				// read in walls
				for (page = 0; page < SpritePage; page++)
					if (pageOffsets[page] > 0)
					{
						stream.Seek(pageOffsets[page], 0);
						byte[] wall = new byte[TileSqrt * TileSqrt];
						for (ushort col = 0; col < TileSqrt; col++)
							for (ushort row = 0; row < TileSqrt; row++)
								wall[TileSqrt * row + col] = (byte)stream.ReadByte();
						Pages[page] = wall.Index2ByteArray(palettes[PaletteNumber(page, xml)]);
					}

				// read in sprites
				for (; page < Pages.Length; page++)
					if (pageOffsets[page] > 0)
					{
						stream.Seek(pageOffsets[page], 0);
						ushort leftExtent = binaryReader.ReadUInt16(),
							rightExtent = binaryReader.ReadUInt16(),
							startY, endY;
						byte[] sprite = new byte[TileSqrt * TileSqrt];
						for (ushort i = 0; i < sprite.Length; i++)
							sprite[i] = 255; // set transparent
						long[] columnDataOffsets = new long[rightExtent - leftExtent + 1];
						for (ushort i = 0; i < columnDataOffsets.Length; i++)
							columnDataOffsets[i] = pageOffsets[page] + binaryReader.ReadUInt16();
						long trexels = stream.Position;
						for (ushort column = 0; column <= rightExtent - leftExtent; column++)
						{
							long commands = columnDataOffsets[column];
							stream.Seek(commands, 0);
							while ((endY = binaryReader.ReadUInt16()) != 0)
							{
								endY >>= 1;
								binaryReader.ReadUInt16(); // Not using this value for anything. Don't know why it's here!
								startY = binaryReader.ReadUInt16();
								startY >>= 1;
								commands = stream.Position;
								stream.Seek(trexels, 0);
								for (ushort row = startY; row < endY; row++)
									sprite[(row * TileSqrt - 1) + column + leftExtent - 1] = binaryReader.ReadByte();
								trexels = stream.Position;
								stream.Seek(commands, 0);
							}
						}
						Pages[page] = TransparentBorder(sprite.Index2IntArray(palettes[PaletteNumber(page, xml)])).Int2ByteArray();
					}

				// read in digisounds
				byte[] soundData = new byte[stream.Length - pageOffsets[Pages.Length]];
				stream.Seek(pageOffsets[Pages.Length], 0);
				stream.Read(soundData, 0, soundData.Length);

				uint start = pageOffsets[NumPages - 1] - pageOffsets[Pages.Length];
				ushort[][] soundTable;
				using (MemoryStream memoryStream = new MemoryStream(soundData, (int)start, soundData.Length - (int)start))
					soundTable = VgaGraph.Load16BitPairs(memoryStream);

				uint numDigiSounds = 0;
				while (numDigiSounds < soundTable.Length && soundTable[numDigiSounds][1] > 0)
					numDigiSounds++;

				DigiSounds = new byte[numDigiSounds][];
				for (uint sound = 0; sound < DigiSounds.Length; sound++)
					if (soundTable[sound][1] > 0 && pageOffsets[Pages.Length + soundTable[sound][0]] > 0)
					{
						DigiSounds[sound] = new byte[soundTable[sound][1]];
						start = pageOffsets[Pages.Length + soundTable[sound][0]] - pageOffsets[Pages.Length];
						for (uint bite = 0; bite < DigiSounds[sound].Length; bite++)
							DigiSounds[sound][bite] = (byte)(soundData[start + bite] - 128); // Godot makes some kind of oddball conversion from the unsigned byte to a signed byte
					}
			}
		}

		public static uint PaletteNumber(uint pageNumber, XElement xml) =>
			xml?.Element("VSwap")?.Descendants()?.Where(
				e => uint.TryParse(e.Attribute("Page")?.Value, out uint page) && page == pageNumber
				)?.Select(e => uint.TryParse(e.Attribute("Palette")?.Value, out uint palette) ? palette : 0)
			?.FirstOrDefault() ?? 0;

		public static IEnumerable<int[]> LoadPalettes(XElement xml)
		{
			foreach (XElement xPalette in xml.Elements("Palette"))
				using (MemoryStream palette = new MemoryStream(Encoding.ASCII.GetBytes(xPalette.Value)))
					yield return TextureMethods.LoadPalette(palette);
		}

		public static int[] TransparentBorder(int[] texture, int width = 0)
		{
			if (width == 0)
				width = (int)Math.Sqrt(texture.Length);
			int[] result = new int[texture.Length];
			Array.Copy(texture, result, result.Length);
			int height = texture.Length / width;
			int Index(int x, int y) => x * width + y;
			List<int> neighbors = new List<int>(9);
			void Add(int x, int y)
			{
				if (x >= 0 && y >= 0 && x < width && y < height
					&& texture[Index(x, y)] is int pixel
					&& pixel.A() > 128)
					neighbors.Add(pixel);
			}
			int Average()
			{
				int count = neighbors.Count();
				if (count == 1)
					return (int)(neighbors.First() & 0xFFFFFF00u);
				int r = 0, g = 0, b = 0;
				foreach (int color in neighbors)
				{
					r += color.R();
					g += color.G();
					b += color.B();
				}
				return TextureMethods.Color((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
			}
			for (int x = 0; x < width; x++)
				for (int y = 0; y < height; y++)
					if (texture[Index(x, y)].A() < 128)
					{
						neighbors.Clear();
						Add(x - 1, y);
						Add(x + 1, y);
						Add(x, y - 1);
						Add(x, y + 1);
						if (neighbors.Count > 0)
							result[Index(x, y)] = Average();
						else
						{
							Add(x - 1, y - 1);
							Add(x + 1, y - 1);
							Add(x - 1, y + 1);
							Add(x + 1, y + 1);
							if (neighbors.Count > 0)
								result[Index(x, y)] = Average();
							else // Make non-border transparent pixels transparent black
								result[Index(x, y)] = 0;
						}
					}
			return result;
		}
	}
}
