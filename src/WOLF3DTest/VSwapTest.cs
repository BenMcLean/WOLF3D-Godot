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
		public void PackAtlas()
		{
			XElement xml = LoadXML(Folder);
			VSwap vSwap = VSwap.Load(Folder, xml);
			VgaGraph vgaGraph = VgaGraph.Load(Folder, xml);
			PackingRectangle[] rectangles = PackingRectangles(vgaGraph, vSwap).ToArray();
			RectanglePacker.Pack(rectangles, out PackingRectangle bounds, PackingHints.TryByBiggerSide);
			int atlasSize = (int)TextureMethods.NextPowerOf2(bounds.BiggerSide);
			byte[] bin = new byte[atlasSize * 4 * atlasSize];
			foreach (PackingRectangle rectangle in rectangles)
				if (TryTextureFromId(rectangle.Id, out byte[] texture, out int width, out int height, vgaGraph, vSwap))
					bin.DrawInsert((int)rectangle.X + 1, (int)rectangle.Y + 1, texture, width, atlasSize)
						.DrawPadding((int)rectangle.X + 1, (int)rectangle.Y + 1, width, height, atlasSize);
			SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bin, atlasSize, atlasSize)
				.SaveAsPng("output.png");
		}
		public IEnumerable<PackingRectangle> PackingRectangles(VgaGraph? vgaGraph = null, VSwap? vSwap = null)
		{
			int total = (vSwap is VSwap vs ? vs.SoundPage : 0)
				+ (vgaGraph is VgaGraph vg ? vg.Pics.Length + vg.Fonts.Select(f => f.Character.Length).Sum() : 0);
			for (int i = 0; i < total; i++)
				if (TryTextureFromId(i, out byte[] _, out int width, out int height, vgaGraph, vSwap))
					yield return new PackingRectangle(0, 0, (uint)width + 2u, (uint)height + 2u, i);
		}
		public bool TryTextureFromId(int id, out byte[] texture, out int width, out int height, VgaGraph? vgaGraph = null, VSwap? vSwap = null)
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
