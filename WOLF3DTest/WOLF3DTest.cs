using System;
using System.IO;
using System.Xml.Linq;
using Godot;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3DGame;
using WOLF3DGame.Model;

namespace WOLF3DTest
{
    [TestClass]
    public class WOLF3DTest
    {
        public static readonly string Folder = @"..\..\..\WOLF3D\WL1\";
        public static readonly XElement XML = LoadXML(Folder);

        public static XElement LoadXML(string folder, string file = "game.xml")
        {
            using (FileStream xmlStream = new FileStream(System.IO.Path.Combine(folder, file), FileMode.Open))
                return XElement.Load(xmlStream);
        }

        [TestMethod]
        public void VSwapTest()
        {
            //DownloadShareware.Main(new string[] { Folder });
            VSwap vSwap = VSwap.Load(Folder, XML);
            Console.WriteLine("Number of graphics pages: " + vSwap.Pages.Length.ToString());
            Console.WriteLine("Number of DigiSounds: " + vSwap.DigiSounds.Length.ToString());
            Console.WriteLine();
            for (int color = 0; color < vSwap.Palette.Length; color++)
                Console.WriteLine("Color " + color + ": " +
                    VSwap.R(vSwap.Palette[color]) + ", " +
                    VSwap.G(vSwap.Palette[color]) + ", " +
                    VSwap.B(vSwap.Palette[color]) + ", " +
                    VSwap.A(vSwap.Palette[color])
                    );
        }

        [TestMethod]
        public void GameMapsTest()
        {
            //DownloadShareware.Main(new string[] { Folder });
            GameMap[] maps = GameMap.Load(Folder, XML);

            Console.WriteLine("Number of maps: " + maps.Length);

            for (int i = 0; i < maps.Length; i++)
                Console.WriteLine(
                    "\"" + maps[i].Name + "\" " +
                    "Floor: " + maps[i].Floor +
                    ", Ceiling: " + maps[i].Ceiling +
                    ", Border: " + maps[i].Border
                    );

            GameMap map = maps[0];
            Console.WriteLine(map.Name + ": ");
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
        public void VgaGraphTest()
        {
            //DownloadShareware.Main(new string[] { Folder });
            VgaGraph vgaGraph = VgaGraph.Load(Folder, XML);

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
            //DownloadShareware.Main(new string[] { Folder });
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

            Console.WriteLine("Pic sizes / 4: ");
            foreach (byte[] pic in vgaGraph.Pics)
                if (pic != null)
                    Console.Write((pic.Length / 4).ToString() + ", ");
            Console.WriteLine();
        }

        [TestMethod]
        public void FontTest()
        {
            //DownloadShareware.Main(new string[] { Folder });
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
            Console.WriteLine();

            string str = "Ab";
            Console.Write("Writing string \"" + str + "\":");
            byte[] test = vgaGraph.Fonts[font].Line(str);
            int width = vgaGraph.Fonts[font].CalcWidth(str) * 4;
            for (uint i = 0; i < test.Length; i++)
            {
                if (i % width == 0)
                    Console.WriteLine();
                Console.Write(
                    test[i] == 0 ?
                    "0"
                    : "1"
                    );
            }
        }

        [TestMethod]
        public void Direction8Test()
        {
            foreach (Direction8 direction8 in Direction8.Values)
                Console.WriteLine(direction8 + " mirrorZ " + direction8.MirrorZ);
            Console.WriteLine();
            foreach (Direction8 direction8 in Direction8.Values)
            {
                float angle = Vector2.Zero.AngleToPoint(direction8.Vector2);
                Console.WriteLine("From zero to (" + direction8.Vector2.x + ", " + direction8.Vector2.y + ") would be " + angle + " radians which is " + Direction8.Angle(angle));
            }
        }
    }
}
