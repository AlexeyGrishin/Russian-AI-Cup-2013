using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace Tests
{
    [TestClass]
    public class ToolTest
    {
        private static Unit unit(int x, int y)
        {
            return new Bonus(-1, x, y, BonusType.Grenade);
        }

        [TestMethod]
        public void GetDirection()
        {
            Assert.AreEqual(Direction.North, Tool.GetDirection(unit(2,2), unit(2,1)));
            Assert.AreEqual(Direction.South, Tool.GetDirection(unit(2, 2), unit(2, 5)));
            Assert.AreEqual(Direction.East, Tool.GetDirection(unit(2, 2), unit(4, 2)));
            Assert.AreEqual(Direction.West, Tool.GetDirection(unit(20, 2), unit(4, 2)));
        }

        [TestMethod]
        public void GetDirection_Diagonal()
        {
            Assert.AreEqual(Direction.East, Tool.GetDirection(unit(5, 5), unit(10, 9)));
            Assert.AreEqual(Direction.South, Tool.GetDirection(unit(5, 5), unit(9, 10)));
        }

        [TestMethod]
        public void Radius_5()
        {
            var r = Tool.GetRadius(5);
            Assert.IsTrue(r.Contains(0, 5));
            Assert.IsFalse(r.Contains(1, 5));
            Assert.IsTrue(r.Contains(5, 0));
            Assert.IsFalse(r.Contains(4, 4));

            Assert.AreEqual(1, r.StepsToGoOutside(3, 4));
            Assert.AreEqual(2, r.StepsToGoOutside(2, 3));
            Assert.AreEqual(2, r.StepsToGoOutside(3, 3));

            Assert.AreEqual(1, r.StepsToGoInside(4, 4));
            
        }

        [TestMethod]
        public void Radius_5_out_of_range()
        {
            var r = Tool.GetRadius(5);

            Assert.AreEqual(5, r.StepsToGoInside(10, 0));
        }
    }
}
