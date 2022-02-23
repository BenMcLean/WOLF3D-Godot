using NUnit.Framework;
using RectpackSharp;
using SixLabors.ImageSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3DTest
{
	public class VSwapTest
	{
		public const string Folder = "../../../../../godot/WOLF3D/WL1/";
		[Test]
		public void Test1()
		{
			XElement xml = LoadXML(Folder);
			VSwap vSwap = VSwap.Load(Folder, xml);
			VgaGraph vgaGraph = VgaGraph.Load(Folder, xml);
			PackingRectangle[] rectangles = PackingRectangles(vSwap, vgaGraph).ToArray();
			RectanglePacker.Pack(rectangles, out PackingRectangle bounds, PackingHints.TryByBiggerSide);
			int atlasSize = (int)TextureMethods.NextPowerOf2(bounds.BiggerSide);
			byte[] bin = new byte[atlasSize * 4 * atlasSize];
			foreach (PackingRectangle rectangle in rectangles)
			{
				if (TryTextureFromId(rectangle.Id, out byte[] texture, out int width, out int height, vSwap, vgaGraph))
					bin.DrawInsert((int)rectangle.X + 1, (int)rectangle.Y + 1, texture, width, atlasSize)
						.DrawPadding((int)rectangle.X + 1, (int)rectangle.Y + 1, width, height, atlasSize);
			}
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bin, atlasSize, atlasSize)
				.SaveAsPng("output.png");
		}
		public IEnumerable<PackingRectangle> PackingRectangles(VSwap vSwap, VgaGraph? vgaGraph = null)
		{
			int number = 0;
			for (; number < vSwap.SoundPage; number++)
				if (vSwap.Pages[number] != null)
					yield return new PackingRectangle(0, 0, vSwap.TileSqrt + 2u, vSwap.TileSqrt + 2u, number);
			if (!(vgaGraph is VgaGraph v))
				yield break;
			for (int i = 0; i < v.Sizes.Length; i++)
				yield return new PackingRectangle(0, 0, v.Sizes[i][0] + 2u, v.Sizes[i][1] + 2u, ++number);
			foreach (VgaGraph.Font font in v.Fonts)
				for (int i = 0; i < font.Width.Length; i++)
					yield return new PackingRectangle(0, 0, font.Width[i] + 2u, font.Height + 2u, ++number);
		}
		public bool TryTextureFromId(int id, out byte[] texture, out int width, out int height, VSwap vSwap, VgaGraph? vgaGraph = null)
		{
			texture = null;
			width = 0;
			height = 0;
			if (id < vSwap.SoundPage)
			{
				texture = vSwap.Pages[id];
				width = vSwap.TileSqrt;
				height = vSwap.TileSqrt;
				return true;
			}
			if (!(vgaGraph is VgaGraph v))
				return false;
			id -= vSwap.SoundPage;
			if (id < v.Pics.Length)
			{
				texture = v.Pics[id];
				width = v.Sizes[id][0];
				height = v.Sizes[id][1];
				return true;
			}
			id -= v.Pics.Length;
			foreach (VgaGraph.Font font in v.Fonts)
			{
				if (id < font.Character.Length)
				{
					texture = font.Character[id];
					width = font.Width[id];
					height = font.Height;
					return true;
				}
				id -= font.Character.Length;
			}
			return false;
		}
		public static XElement LoadXML(string folder, string file = "game.xml")
		{
			string path = System.IO.Path.Combine(folder, file);
			if (!System.IO.Directory.Exists(folder) || !System.IO.File.Exists(path))
				return null;
			else using (FileStream xmlStream = new FileStream(path, FileMode.Open))
					return XElement.Load(xmlStream);
		}
	}
}
