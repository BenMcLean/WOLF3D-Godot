using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D;

namespace WOLF3DTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            VSwap vswap = new VSwap().LoadPalette(@"..\..\..\Palettes\Wolf3D.pal");
            using (FileStream file = new FileStream(@"..\..\..\WOLF3D\VSWAP.WL1", FileMode.Open))
                vswap.Read(file);
        }
    }
}
