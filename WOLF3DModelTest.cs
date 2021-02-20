using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Godot;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D.WOLF3DGame;
using WOLF3DModel;

namespace WOLF3DTest
{
    [TestClass]
    public class WOLF3DModelTest
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
            VSwap vSwap = VSwap.Load(Folder, XML);
            Console.WriteLine("Number of graphics pages: " + vSwap.Pages.Length.ToString());
            Console.WriteLine("Number of DigiSounds: " + vSwap.DigiSounds.Length.ToString());
            Console.WriteLine();
            for (int color = 0; color < vSwap.Palettes.Length; color++)
                Console.WriteLine("Color " + color + ": " +
                    VSwap.R(vSwap.Palettes[0][color]) + ", " +
                    VSwap.G(vSwap.Palettes[0][color]) + ", " +
                    VSwap.B(vSwap.Palettes[0][color]) + ", " +
                    VSwap.A(vSwap.Palettes[0][color])
                    );

            uint[] newPalette = VSwap.Byte2IntArray(VSwap.Int2ByteArray(vSwap.Palettes[0]));
            for (int color = 0; color < vSwap.Palettes.Length; color++)
                Assert.IsTrue(vSwap.Palettes[0][color] == newPalette[color]);
        }

        [TestMethod]
        public void GameMapsTest()
        {
            GameMap[] maps = GameMap.Load(Folder, XML);

            Console.WriteLine("Number of maps: " + maps.Length);

            for (int i = 0; i < maps.Length; i++)
                Console.WriteLine(
                    "\"" + maps[i].Name + "\" " +
                    "Floor: " + maps[i].Ground +
                    ", Ceiling: " + maps[i].Ceiling +
                    ", Border: " + maps[i].Border
                    );

            GameMap map = maps[0];
            Console.WriteLine("\"" + map.Name + "\", Width: " + map.Width + ", Depth: " + map.Depth);
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
                Console.WriteLine("From zero to (" + direction8.Vector2.x + ", " + direction8.Vector2.y + ") would be " + angle + " radians which is " + Direction8.FromAngle(angle));
                Assert.AreEqual(direction8, Direction8.FromAngle(angle));
                Assert.AreEqual(Direction8.FromAngle(angle), Direction8.AngleToPoint(direction8.Vector2.x, direction8.Vector2.y));
            }
        }

        [TestMethod]
        public void EndStringTest()
        {
            ICollection<string> it = XML?.Element("VgaGraph")?.Element("Menus")?.Elements("EndString")?.Select(a => a.Value)?.ToList();

            foreach (string s in it)
                Console.WriteLine("\"" + s + "\"");

            RNG rng = new RNG();
            Console.WriteLine("Random element: \"" + rng.RandomElement(it) + "\"");
        }

        [TestMethod]
        public void Direction8RandomOrderTest()
        {
            foreach (Direction8 direction in Direction8.RandomOrder(new RNG(), Direction8.NORTH))
                Console.WriteLine(direction.Name);
        }
    }
}
