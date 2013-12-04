using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

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
            TestRadiusInclusion(5);

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

        private void TestRadiusInclusion(int r)
        {
            var radius = Tool.GetRadius(r);
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <=r; dy++)
                {
                    var insideRadius = radius.Contains(dx, dy);
                    var insideReal = (dx * dx) + (dy * dy) <= r * r;
                    Assert.AreEqual(insideReal, insideRadius, String.Format("{0},{1}", dx, dy));
                }
            }
        }

        [TestMethod]
        public void Radius_Inclusion_10()
        {
            TestRadiusInclusion(10);
        }

        [TestMethod]
        public void Radius_Inclusion_9()
        {
            TestRadiusInclusion(9);
        }

        [TestInitialize]
        public void SetUp()
        {
            pointNormal = new PossibleMove { DangerIndex = 0 };
            pointDanger = new PossibleMove { DangerIndex = 2 };
            pointVeryDanger = new PossibleMove { DangerIndex = 4 };
            pointGood = new PossibleMove { DangerIndex = -1 };
        }

        PossibleMove pointNormal, pointDanger, pointVeryDanger, pointGood;

        [TestMethod]
        public void BestWay_2ways_different_dangerous()
        {
            var way1 = new PossibleMove[] { pointNormal, pointNormal, pointDanger };
            var way2 = new PossibleMove[] { pointNormal, pointNormal, pointVeryDanger };
            var best = Tool.LessDangerousWay(new List<IEnumerable<PossibleMove>> { way1, way2 }, 2);
            Assert.AreSame(way1, best);
        }

        [TestMethod]
        public void BestWay_2ways_smaller_length()
        {
            var way1 = new PossibleMove[] { pointNormal, pointNormal, pointDanger };
            var way2 = new PossibleMove[] { pointNormal, pointVeryDanger };
            var best = Tool.LessDangerousWay(new List<IEnumerable<PossibleMove>> { way1, way2 }, 2);
            Assert.AreSame(way1, best);
            
        }

        PossibleMove P(int x, int y)
        {
            return new PossibleMove { X = x, Y = y };
        }

        [TestMethod]
        public void Distinct_ways()
        {
            var way1 = new PossibleMove[] { P(1, 1), P(1, 2), P(1, 3), P(1, 4) };
            var way2 = new PossibleMove[] { P(1, 1), P(1, 2), P(1, 3), P(1, 5) };
            var way3 = new PossibleMove[] { P(1, 1), P(1, 2), P(1, 3)};
            var way4 = new PossibleMove[] { P(1, 1), P(1, 2)};
            var way5 = new PossibleMove[] { P(1, 1), P(1, 2), P(2, 2) };
            var waysLength3 = Tool.DistinctWays(new []{ way1, way2, way3, way4, way5 }, 3);
            var waysLength2 = Tool.DistinctWays(new[] { way1, way2, way3, way4, way5 }, 2);

            Assert.AreEqual(2, waysLength3.Count());
            CollectionAssert.AreEqual(new[] { P(1, 1), P(1, 2), P(1, 3) }.ToList(), waysLength3.ElementAt(0).ToList());
            CollectionAssert.AreEqual(new[] { P(1, 1), P(1, 2), P(2, 2) }.ToList(), waysLength3.ElementAt(1).ToList());
            Assert.AreEqual(1, waysLength2.Count());
            CollectionAssert.AreEqual(new[] { P(1, 1), P(1, 2) }.ToList(), waysLength2.ElementAt(0).ToList());
        }
    }
}
