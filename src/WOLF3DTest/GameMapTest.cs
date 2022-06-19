using NUnit.Framework;
using System.Configuration;
using WOLF3DModel;

namespace WOLF3DTest
{
	public class GameMapTest
	{
		public readonly static string Folder = ConfigurationManager.AppSettings["Folder"];
		[Test]
		public void Test()
		{
			GameMap[] gameMaps = GameMap.Load(Folder, VSwapTest.LoadXML(Folder));
		}
	}
}
