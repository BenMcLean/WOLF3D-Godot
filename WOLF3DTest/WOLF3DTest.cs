using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D;

namespace WOLF3DTest
{
    [TestClass]
    public class WOLF3DTest
    {
        public static readonly string Folder = @"..\..\..\WOLF3D\";
        public static readonly XElement XML = Assets.LoadXML(Folder);

        [TestMethod]
        public void GameMapsTest()
        {
            DownloadShareware.Main(new string[] { Folder });
            GameMaps maps = XML.Element("Maps") == null ? null : GameMaps.Load(Folder, XML);

            GameMaps.Map map = maps.Maps[0];
            Console.WriteLine();
            for (uint i = 0; i < map.MapData.Length; i++)
            {
                Console.Write(map.MapData[i].ToString("D3"));
                if (i % map.Width == map.Width - 1)
                    Console.WriteLine();
                else
                    Console.Write(" ");
            }
        }

        [TestMethod]
        public void SongTest()
        {
            DownloadShareware.Main(new string[] { Folder });
            AudioT audioT = XML.Element("Audio") == null ? null : AudioT.Load(Folder, XML);
            byte[] song = audioT.AudioTFile[audioT.StartMusic + 14],
                bytes = new byte[1];
            Array.Copy(song, song.Length - bytes.Length, bytes, 0, bytes.Length);

            using (Stream stream = new MemoryStream(bytes))
            using (StreamReader streamReader = new StreamReader(stream))
                Console.WriteLine(streamReader.ReadToEnd());
        }
    }
}
