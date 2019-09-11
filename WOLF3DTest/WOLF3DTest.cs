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
        public void AssetsTest()
        {
            DownloadShareware.Main(new string[] { Folder });

            XElement XML;
            using (FileStream xml = new FileStream(System.IO.Path.Combine(Folder, "game.xml"), FileMode.Open))
                XML = XElement.Load(xml);

            VSwap VSwap;
            if (XML.Element("Palette") != null && XML.Element("VSwap") != null)
                using (MemoryStream palette = new MemoryStream(Encoding.ASCII.GetBytes(XML.Element("Palette").Value)))
                using (FileStream vSwap = new FileStream(System.IO.Path.Combine(Folder, XML.Element("VSwap").Attribute("Name").Value), FileMode.Open))
                    VSwap = new VSwap(palette, vSwap);

            GameMaps GameMaps;
            if (XML.Element("Maps") != null)
                using (FileStream mapHead = new FileStream(System.IO.Path.Combine(Folder, XML.Element("Maps").Attribute("MapHead").Value), FileMode.Open))
                using (FileStream gameMaps = new FileStream(System.IO.Path.Combine(Folder, XML.Element("Maps").Attribute("GameMaps").Value), FileMode.Open))
                    GameMaps = new GameMaps(mapHead, gameMaps);

            AudioT AudioT;
            if (XML.Element("Audio") != null)
                using (FileStream audioHead = new FileStream(System.IO.Path.Combine(Folder, XML.Element("Audio").Attribute("AudioHead").Value), FileMode.Open))
                using (FileStream audioT = new FileStream(System.IO.Path.Combine(Folder, XML.Element("Audio").Attribute("AudioT").Value), FileMode.Open))
                    AudioT = new AudioT(audioHead, audioT, XML.Element("Audio"));
        }
    }
}
