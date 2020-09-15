using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using WOLF3D.WOLF3DGame.Action;

namespace WOLF3DTest
{
    [TestClass]
    public class SymmetricMatrixTest
    {
        [TestMethod]
        public void Test()
        {
            SymmetricMatrix test = new SymmetricMatrix(3);
            test[1, 0] = 1;
            test[2, 0] = 2;
            test[2, 1] = 3;
            test[3, 0] = 4;
            test[3, 1] = 5;
            test[3, 2] = 6;
            Assert.AreEqual(test[0, 1], 1);
            Assert.AreEqual(test[0, 2], 2);
            Assert.AreEqual(test[1, 2], 3);
            Assert.AreEqual(test[0, 3], 4);
            Assert.AreEqual(test[1, 3], 5);
            Assert.AreEqual(test[2, 3], 6);
            Assert.IsTrue(test.ToString().Equals("1,2,3,4,5,6"));
            Assert.IsTrue(new SymmetricMatrix(test.ToString()).ToString().Equals(test.ToString()));
            Assert.IsTrue(new SymmetricMatrix(test).ToString().Equals(test.ToString()));
        }

        [TestMethod]
        public void FloorCodesTest()
        {
            SymmetricMatrix test = new SymmetricMatrix(10);
            test[1, 3] = 1;
            test[3, 5] = 1;
            test[5, 7] = 1;
            test[7, 9] = 1;
            void InnerTest(params uint[] input)
            {
                List<uint> floorCodes = test.FloorCodes(input);
                Assert.AreEqual(floorCodes.Count, 5);
                foreach (uint floorCode in floorCodes)
                    Assert.AreEqual(floorCode % 2u, 1u);
            }
            InnerTest(1);
            InnerTest(3);
            InnerTest(5);
            InnerTest(7);
            InnerTest(9);
            InnerTest(1, 3, 5, 7, 9);
            Assert.AreEqual(test.FloorCodes(2).Count, 1);
            Assert.AreEqual(test.FloorCodes(4).Count, 1);
            Assert.AreEqual(test.FloorCodes(6).Count, 1);
            Assert.AreEqual(test.FloorCodes(8).Count, 1);
            Assert.AreEqual(test.FloorCodes(10).Count, 1);
            Assert.AreEqual(test.FloorCodes(2, 4, 6, 8, 10).Count, 5);
        }
    }
}
