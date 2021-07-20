using NUnit.Framework;

namespace WOLF3DTest
{
	public class Tests
	{
		const string Folder = "../../../WOLF3D/WL1/";
		[Test]
		public void Test1()
		{
			TestContext.WriteLine(Folder);
			Assert.Pass();
		}
	}
}
