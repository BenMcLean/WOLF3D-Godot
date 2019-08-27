using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3DSim;

namespace WOLF3DTest
{
    [TestClass]
    public class WOLF3DTest
    {
        [TestMethod]
        public void VSwapTest()
        {
            DownloadShareware.Main(new string[] { @"..\..\..\" });
            VSwap vswap = new VSwap()
                .SetPalette(@"..\..\..\Wolf3DSim\Palettes\Wolf3D.pal")
                .Read(@"..\..\..\WOLF3D\VSWAP.WL1");
        }

        [TestMethod]
        public void GameMapsTest()
        {
            DownloadShareware.Main(new string[] { @"..\..\..\" });
            GameMaps gameMaps = new GameMaps().Read(@"..\..\..\WOLF3D\MAPHEAD.WL1", @"..\..\..\WOLF3D\GAMEMAPS.WL1");
            GameMaps.Map map = gameMaps.Maps[0];
            Console.WriteLine();
            string result = string.Empty;
            for (uint i = 0; i < map.MapData.Length; i++)
            {
                result += map.MapData[i].ToString("D3") + " ";
                if (i % map.Width == map.Width - 1)
                    result += "\n";
            }
            Console.WriteLine(result);

            //foreach (GameMaps.Map map in gameMaps.Maps)
            //    Console.WriteLine(map.Name);
        }
    }
}
