using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3DTest
{
	public class GameMapTest
	{

		public readonly static XElement XML = VSwapTest.LoadXML(VSwapTest.Folder);
		[Test]
		public void Test()
		{
			foreach (MapAnalyzer.MapAnalysis map in new MapAnalyzer(XML).Analyze(GameMap.Load(VSwapTest.Folder, XML)))
			{
				File.WriteAllText(map.Map.Name + "-IsMappable.csv", string.Join(Environment.NewLine, Enumerable.Range(0, map.Map.Depth).Select(z => string.Join(",", Enumerable.Range(0, map.Map.Width).Select(x => map.IsMappable(x, z) ? "1" : "0")))));
				File.WriteAllText(map.Map.Name + "-IsTransparent.csv", string.Join(Environment.NewLine, Enumerable.Range(0, map.Map.Depth).Select(z => string.Join(",", Enumerable.Range(0, map.Map.Width).Select(x => map.IsTransparent(x, z) ? "1" : "0")))));
			}
		}
	}
}
