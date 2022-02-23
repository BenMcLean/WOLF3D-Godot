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
			VSwap vSwap = VSwap.Load(Folder, LoadXML(Folder));
			PackingRectangle[] rectangles = PackingRectangles(vSwap).ToArray();
			RectanglePacker.Pack(rectangles, out PackingRectangle bounds, PackingHints.MostlySquared);
			int atlasSize = (int)TextureMethods.NextPowerOf2(bounds.BiggerSide);
			byte[] bin = new byte[atlasSize * 4 * atlasSize];
			foreach (PackingRectangle rectangle in rectangles)
				bin.DrawInsert((int)rectangle.X + 1, (int)rectangle.Y + 1, vSwap.Pages[rectangle.Id], vSwap.TileSqrt, atlasSize)
					.DrawPadding((int)rectangle.X + 1, (int)rectangle.Y + 1, vSwap.TileSqrt, vSwap.TileSqrt, atlasSize);
			Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(bin, atlasSize, atlasSize)
						.SaveAsPng("output.png");
		}
		public IEnumerable<PackingRectangle> PackingRectangles(VSwap vSwap)
		{
			for (int i = 0; i < vSwap.SoundPage; i++)
				if (vSwap.Pages[i] != null)
					yield return new PackingRectangle(0, 0, vSwap.TileSqrt + 2u, vSwap.TileSqrt + 2u, i);
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
