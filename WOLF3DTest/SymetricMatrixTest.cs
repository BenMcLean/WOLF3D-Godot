using Microsoft.VisualStudio.TestTools.UnitTesting;
using WOLF3D.WOLF3DGame.Action;

namespace WOLF3DTest
{
    [TestClass]
    public class SymetricMatrixTest
    {
        [TestMethod]
        public void Test()
        {
            SymetricMatrix test = new SymetricMatrix(3);
            test[1, 0] = 1;
            test[2, 0] = 2;
            test[2, 1] = 3;
            test[3, 0] = 4;
            test[3, 1] = 5;
            test[3, 2] = 6;
            Assert.IsTrue(test.ToString().Equals("1,2,3,4,5,6"));
            Assert.IsTrue(new SymetricMatrix(test.ToString()).ToString().Equals(test.ToString()));
        }
    }
}
