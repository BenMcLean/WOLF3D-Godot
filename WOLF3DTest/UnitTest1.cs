using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D.Graphics;
using static WOLF3D.Graphics.VswapFileReader;

namespace WOLF3DTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            VswapFileData data;
            using (FileStream file = new FileStream(@"..\..\..\WOLF3D\VSWAP.WL1", FileMode.Open))
                data = VswapFileReader.Read(file, 64);
        }
    }
}
