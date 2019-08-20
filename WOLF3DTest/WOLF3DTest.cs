using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D;

namespace WOLF3DTest
{
    [TestClass]
    public class WOLF3DTest
    {
        [TestMethod]
        public void VSwapTest()
        {
            WOLF3D.DownloadShareware.Main(new string[] { @"..\..\..\" });
            VSwap vswap = new VSwap().LoadPalette(@"..\..\..\Palettes\Wolf3D.pal");
            vswap.Read(@"..\..\..\WOLF3D\VSWAP.WL1");
        }

        [TestMethod]
        public void GameMapsTest()
        {
            WOLF3D.DownloadShareware.Main(new string[] { @"..\..\..\" });
            GameMaps maps = new GameMaps().Read(@"..\..\..\WOLF3D\MAPHEAD.WL1", @"..\..\..\WOLF3D\GAMEMAPS.WL1");
            foreach (GameMaps.Map map in maps.Maps)
                Console.WriteLine(map.Name);
        }
    }
}
