using NUnit.Framework;
using System;
using System.IO;

namespace WOLF3DTest.ShadowCastTest
{
	public class ShadowCastTest
	{
		public const string Folder = @"..\..\..\ShadowCastTest\";
		[Test]
		public void LitBoolsTest()
		{
			string map1 = File.ReadAllText(Path.Combine(Folder, "Wolf1 Map1-IsTransparent.csv"));
			Console.Out.WriteLine(map1);
			Assert.IsTrue(map1.Equals(new ShadowCast.LitBools(map1).ToString()));
		}
	}
}
