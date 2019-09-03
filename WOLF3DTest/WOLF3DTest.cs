using System;
using System.IO;
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

            VSwap vswap;
            using (FileStream palette = new FileStream(@"..\..\..\Wolf3DSim\Palettes\Wolf3D.pal", FileMode.Open))
            using (FileStream file = new FileStream(@"..\..\..\WOLF3D\VSWAP.WL1", FileMode.Open))
                vswap = new VSwap(palette, file);
        }

        [TestMethod]
        public void GameMapsTest()
        {
            GameMaps maps;
            DownloadShareware.Main(new string[] { @"..\..\..\" });
            using (FileStream mapHead = new FileStream(@"..\..\..\WOLF3D\MAPHEAD.WL1", FileMode.Open))
            using (FileStream gameMaps = new FileStream(@"..\..\..\WOLF3D\GAMEMAPS.WL1", FileMode.Open))
                maps = new GameMaps(mapHead, gameMaps);
            GameMaps.Map map = maps.Maps[0];
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
