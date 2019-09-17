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

            //uint chunk = 0;
            //Console.Write("Chunk " + chunk.ToString() + " contents: ");
            //foreach (byte bite in vgaGraph.File[chunk])
            //    Console.Write(bite.ToString() + ", ");
            //Console.WriteLine();
        }

        [TestMethod]
        public void LengthsTest()
        {
            DownloadShareware.Main(new string[] { Folder });
            uint[] head;
            using (FileStream vgaHead = new FileStream(System.IO.Path.Combine(Folder, XML.Element("VgaGraph").Attribute("VgaHead").Value), FileMode.Open))
                head = VgaGraph.ParseHead(vgaHead);
            uint[] lengths = new uint[head.Length - 1];
            using (FileStream vgaGraphStream = new FileStream(System.IO.Path.Combine(Folder, XML.Element("VgaGraph").Attribute("VgaGraph").Value), FileMode.Open))
            using (BinaryReader binaryReader = new BinaryReader(vgaGraphStream))
                for (uint i = 0; i < lengths.Length; i++)
                {
                    vgaGraphStream.Seek(head[i], 0);
                    lengths[i] = binaryReader.ReadUInt32();
                }

            Console.WriteLine("Lengths from start of each chunk: ");
            foreach (uint length in lengths)
                Console.Write(length.ToString() + ", ");
            Console.WriteLine();

            Console.WriteLine("Sizes from chunk 0: ");
            VgaGraph vgaGraph = VgaGraph.Load(Folder, XML);
            foreach (ushort[] size in vgaGraph.Sizes)
                Console.Write((size[0] * size[1]).ToString() + ", ");
            Console.WriteLine();

            Console.WriteLine("Huffman decompressed sizes: ");
            foreach (byte[] chunk in vgaGraph.File)
                Console.Write(chunk.Length.ToString() + ", ");
            Console.WriteLine();

            Console.WriteLine("Pic sizes / 4: ");
            foreach (byte[] pic in vgaGraph.Pic)
                if (pic != null)
                    Console.Write((pic.Length / 4).ToString() + ", ");
            Console.WriteLine();
        }

        [TestMethod]
        public void FontTest()
        {
            DownloadShareware.Main(new string[] { Folder });
            VgaGraph vgaGraph = VgaGraph.Load(Folder, XML);
            uint font = 0;
            char letter = 'A';
            Console.Write("Writing letter \"" + letter + "\":");
            for (uint i = 0; i < vgaGraph.Fonts[font].Character[letter].Length; i++)
            {
                if (i % (vgaGraph.Fonts[font].Width[letter] * 4) == 0)
                    Console.WriteLine();
                Console.Write(
                    vgaGraph.Fonts[font].Character[letter][i] == 0 ?
                    "0"
                    : "1"
                    );
            }
        }
    }
}