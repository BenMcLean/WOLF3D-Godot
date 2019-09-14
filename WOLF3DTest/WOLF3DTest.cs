using System;
using System.IO;
using System.Linq;
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
            GameMaps maps = GameMaps.Load(Folder, XML);

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
            AudioT audioT = AudioT.Load(Folder, XML);
            byte[] song = audioT.AudioTFile[audioT.StartMusic + 14],
                bytes = new byte[1];
            Array.Copy(song, song.Length - bytes.Length, bytes, 0, bytes.Length);

            //using (Stream stream = new MemoryStream(bytes))
            //using (StreamReader streamReader = new StreamReader(stream))
            //    Console.WriteLine(streamReader.ReadToEnd());
        }

        [TestMethod]
        public void VgaGraphTest()
        {
            DownloadShareware.Main(new string[] { Folder });
            VgaGraph vgaGraph = VgaGraph.Load(Folder, XML);

            Console.Write("Lengths: ");
            foreach (byte[] length in vgaGraph.File)
                Console.Write(length.Length.ToString() + ", ");
            Console.WriteLine();

            if (vgaGraph.Sizes != null)
            {
                Console.Write("Image sizes: ");
                foreach (ushort[] size in vgaGraph.Sizes)
                    if (size != null)
                        Console.Write("(" + size[0].ToString() + ", " + size[1].ToString() + ") ");
                Console.WriteLine();
            }

            uint chunk = 1;
            Console.Write("Chunk " + chunk.ToString() + " contents: ");
            foreach (byte bite in vgaGraph.File[chunk])
                Console.Write(bite.ToString() + ", ");
            Console.WriteLine();

        }
    }
}
