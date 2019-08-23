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
            VSwap vswap = new VSwap()
                .LoadPalette(@"..\..\..\Palettes\Wolf3D.pal")
                .Read(@"..\..\..\WOLF3D\VSWAP.WL1");
        }

        [TestMethod]
        public void GameMapsTest()
        {
            WOLF3D.DownloadShareware.Main(new string[] { @"..\..\..\" });
            GameMaps gameMaps = new GameMaps().Read(@"..\..\..\WOLF3D\MAPHEAD.WL1", @"..\..\..\WOLF3D\GAMEMAPS.WL1");
            GameMaps.Map map = gameMaps.Maps[8];
            Console.WriteLine();
            string result = string.Empty;
            for (uint i=0; i<map.MapData.Length; i++)
            {
                //if (map.OtherData[i] == 43981)
                //    map.OtherData[i] = 0;
                result += map.MapData[i].ToString("D3") + " ";
                if (i % map.Width == map.Width-1)
                    result += "\n";
            }
            Console.WriteLine(result);

            //foreach (GameMaps.Map map in gameMaps.Maps)
            //    Console.WriteLine(map.Name);
        }
    }
}
