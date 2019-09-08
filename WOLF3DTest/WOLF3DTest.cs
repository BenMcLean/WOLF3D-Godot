using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D;
using WOLF3DSim;

namespace WOLF3DTest
{
    [TestClass]
    public class WOLF3DTest
    {
        public static readonly string Folder = @"..\..\..\WOLF3D\";

        [TestMethod]
        public void VSwapTest()
        {
            DownloadShareware.Main(new string[] { Folder });

            XElement xml;
            using (FileStream game = new FileStream(System.IO.Path.Combine(Folder, "game.xml"), FileMode.Open))
                xml = XElement.Load(game);

            VSwap vswap;
            //using (FileStream palette = new FileStream(@"..\..\..\Wolf3DSim\Palettes\Wolf3D.pal", FileMode.Open))
            using (MemoryStream palette = new MemoryStream(Encoding.ASCII.GetBytes(xml.Element("Palette").Value)))
            using (FileStream file = new FileStream(System.IO.Path.Combine(Folder, "VSWAP.WL1"), FileMode.Open))
            {
                palette.Seek(0, 0);
                vswap = new VSwap(palette, file);
            }
        }

        [TestMethod]
        public void GameMapsTest()
        {
            DownloadShareware.Main(new string[] { Folder });

            GameMaps maps;
            using (FileStream mapHead = new FileStream(System.IO.Path.Combine(Folder, "MAPHEAD.WL1"), FileMode.Open))
            using (FileStream gameMaps = new FileStream(System.IO.Path.Combine(Folder, "GAMEMAPS.WL1"), FileMode.Open))
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

        [TestMethod]
        public void AudioTest()
        {
            DownloadShareware.Main(new string[] { Folder });

            AudioT audioT;
            using (FileStream audioHed = new FileStream(System.IO.Path.Combine(Folder, "AUDIOHED.WL1"), FileMode.Open))
            using (FileStream audioTStream = new FileStream(System.IO.Path.Combine(Folder, "AUDIOT.WL1"), FileMode.Open))
                audioT = new AudioT(audioHed, audioTStream);

            foreach (uint i in audioT.AudioHed)
                Console.Write(i.ToString() + ", ");
        }
    }
}
