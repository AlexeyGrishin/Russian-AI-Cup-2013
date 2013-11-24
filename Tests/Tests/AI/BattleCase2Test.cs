using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;

namespace Tests.AI
{
    [TestClass]
    public class BattleCase2Test
    {
        [TestInitialize]
        public void SetUp()
        {

        }

        [TestMethod]
        public void TestMovingArea()
        {
            var map = new string[] {
                "        43",
                "       432",
                "     xxxx1",
                "   ! xxxxc",
                "     xxxx1",
                "        x2",
                "         x",
            };
            var maze = new MockMaze<Warrior2Mock>(map);
            var MA = WalkableMap.Create(maze);
            var commander = maze.Trooper(TrooperType.Commander);
            var location = MA.BuildMapFrom(commander, 4);
            Assert.AreEqual(commander.X, location.X);
            Assert.AreEqual(commander.Y, location.Y);
            Assert.AreEqual(0, location.Step);
            var pointsObserved = 0;
            location.ForEach(p =>
            {
                if (p == location) return;
                pointsObserved++;
                Assert.AreEqual(map[p.Y][p.X], p.Step.ToString()[0]);
            });
            Assert.AreEqual(8, pointsObserved);
            CollectionAssert.AreEqual(
                new List<PossibleMove> 
                {
                    MA.Get(9, 3), MA.Get(9, 2), MA.Get(9, 1), MA.Get(8, 1),  MA.Get(7, 1), 
                },
                MA.Get(7, 1).PathToThis().ToList()
                );
        }

        [TestMethod]
        public void TestFindWay()
        {
            var map = new string[] {
                "   c   ",
                "  xxx  ",
                " xxxxx ",
                " x * x ",
                " x     "
            };
            var maze = new MockMaze<Warrior2Mock>(map);
            var MA = WalkableMap.Create(maze);
            var self = MockTool.FindChar(map, 'c');
            var target = MockTool.FindChar(map, '*');
            var selfLoc = MA.BuildMapFrom(self, 11);
            var way = MA.FindWay(selfLoc, MA.Get(target.X, target.Y));
            Console.WriteLine(String.Join(" --> ", way.Select(c => c.ToString())));
            CollectionAssert.AreEqual(new List<PossibleMove> {
                MA.Get(3, 0), MA.Get(4, 0), MA.Get(5, 0), MA.Get(6, 0), MA.Get(6, 1), MA.Get(6, 2), MA.Get(6, 3), MA.Get(6, 4), MA.Get(5, 4), MA.Get(4, 4), MA.Get(3, 4), MA.Get(3, 3),
            }, way.ToList());
        }

        [TestMethod]
        public void TestFindWay_Exclude()
        {
            var map = new string[] {
                "   c   ",
                "  x x  ",
                " xx5xx ",
                " x   x ",
                " x     "
            };
            var maze = new MockMaze<Warrior2Mock>(map);
            var MA = WalkableMap.Create(maze);
            var self = MockTool.FindChar(map, 'c');
            var target = MockTool.FindChar(map, '5').To(Direction.South);
            var selfLoc = MA.BuildMapFrom(self, 11, p => !maze.HasNotWallOrUnit(p.X, p.Y));
            var way = MA.FindWay(selfLoc, MA.Get(target.X, target.Y));
            Console.WriteLine(String.Join(" --> ", way.Select(c => c.ToString())));
            CollectionAssert.AreEqual(new List<PossibleMove> {
                MA.Get(3, 0), MA.Get(4, 0), MA.Get(5, 0), MA.Get(6, 0), MA.Get(6, 1), MA.Get(6, 2), MA.Get(6, 3), MA.Get(6, 4), MA.Get(5, 4), MA.Get(4, 4), MA.Get(3, 4), MA.Get(3, 3),
            }, way.ToList());
        }

        [TestMethod]
        public void Test_Attackable()
        {
            var map = new string[] {
                "        ",
                "        ",
                "  c   ! ",
                "  m     ",
                "        ",
                "        ",
                "        "
            };
            var maze = new MockMaze<Positioned2Mock>(map);
            var MA = WalkableMap.Create(maze);
            var commander = maze.Trooper(TrooperType.Commander);
            var medic = maze.Trooper(TrooperType.FieldMedic);
            var enemy = MockTool.FindChar(map, '!');
            var location = MA.BuildMapFrom(commander, 4, (p) => !maze.HasNotWallOrUnit(p.X, p.Y));
            var medicLoc = MA.Get(medic);
            var enemyLocation = MA.Get(enemy.X, enemy.Y);

            var self2 = new Positioned2Mock { Location = location, AttackRange = 6 };
            var medic2 = new Positioned2Mock { Location = medicLoc, AttackRange = 2 };
            var enemy2 = new Positioned2Mock { Location = enemyLocation, AttackRange = 5 };
            var battle = new BattleCase2<Positioned2Mock>(self2, enemy2, maze, new List<Positioned2Mock> {medic2});
            Assert.IsTrue(location.CanBeAttacked);
            Assert.IsTrue(medicLoc.CanBeAttacked);

        }

        [TestMethod]
        public void TestBattleCase()
        {
            var map = new string[] {
                "       :' ",
                "       :' ",
                "   ! xxxx'",
                "     xxxxc",
                "     xxxx ",
                "      x:' ",
                "         x",
            };
            var maze = new MockMaze<Positioned2Mock>(map);
            var MA = WalkableMap.Create(maze);
            var commander = maze.Trooper(TrooperType.Commander);
            var enemy = MockTool.FindChar(map, '!');
            var location = MA.BuildMapFrom(commander, 4);
            var enemyLocation = MA.Get(enemy.X, enemy.Y);

            var self2 = new Positioned2Mock { Location = location, AttackRange = 6 };
            var enemy2 = new Positioned2Mock { Location = enemyLocation, AttackRange = 5 };
            var battle = new BattleCase2<Positioned2Mock>(self2, enemy2,  maze);

            location.ForEach(a =>
            {
                if (a == location) return;
                char c = ' ';
                if (a.CanAttackFromHere)
                {
                    c = a.CanBeAttacked ? ':' : '\'';
                }
                else if (a.CanBeAttacked)
                {
                    c = '.';
                }
                Assert.AreEqual(map[a.Y][a.X], c, String.Format("Point at {0},{1}", a.X, a.Y));
            });
            Console.WriteLine(String.Join("\n\n", battle.WaysToEnemy.Select(w => String.Join(" -> ", w.Select(c => c.ToString())))));
            Assert.AreEqual(1, battle.WaysToEnemy.Count);
            Assert.AreEqual(1, battle.StepsToAttack);
            Assert.AreEqual(4, battle.StepsToBeAttacked);
        }

    }
}
