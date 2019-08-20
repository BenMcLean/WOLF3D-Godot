using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D;

namespace WOLF3DTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void VSwapTest()
        {
            WOLF3D.DownloadShareware.Main(new string[] { @"..\..\..\" });
            VSwap vswap = new VSwap().LoadPalette(@"..\..\..\Palettes\Wolf3D.pal");
            vswap.Read(@"..\..\..\WOLF3D\VSWAP.WL1");
        }

        [TestMethod]
        public void MapsTest()
        {
            WOLF3D.DownloadShareware.Main(new string[] { @"..\..\..\" });
            Maps maps = new Maps().Read(@"..\..\..\WOLF3D\MAPHEAD.WL1", "");
        }
    }
}
