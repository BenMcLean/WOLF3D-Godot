using NUnit.Framework;
using WOLF3DModel;

namespace WOLF3DTest
{
	public class GameMapTest
	{
		[Test]
		public void Test()
		{
			GameMap[] gameMaps = GameMap.Load(VSwapTest.Folder, VSwapTest.LoadXML(VSwapTest.Folder));
		}
	}
}
