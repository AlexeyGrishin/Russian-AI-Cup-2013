using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;

namespace Tests.AI
{
    [TestClass]
    public class RightHandObserverTest
    {


        private IMazeObserver observer;
        private RHWayFinder finder;

        [TestInitialize]
        public void SetUp()
        {
            observer = new RightHandObserver();
            finder = new RHWayFinder(2);
        }
        

        [TestMethod]
        public void TestRectangle()
        {
            var maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "xxxxx",
                "x   x",
                "x ! x",
                "x   x",
                "xxxxx"
            });
            var steps = observer.NextSteps(maze, maze.Point, 10);
            CollectionAssert.AreEqual(new List<Direction> { 
                Direction.East, 
                Direction.North, 
                Direction.West, Direction.West,
                Direction.South, Direction.South,
                Direction.East, Direction.East,
                Direction.North, Direction.North, 
            }, steps.ToList(), String.Join(",", steps.ToArray()));
        }

        [TestMethod]
        public void Maze()
        {
            var maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "      xx",
                "x  xx x!",
                "x xx  x ",
                "x    xx ",
                "xxxx    "
            });
            WalkableMap.Create(maze).BuildMapFrom(maze.Point, 10);
            var steps = observer.NextSteps(maze, maze.Point, 11);
            CollectionAssert.AreEqual(new List<Direction> { 
                Direction.South, Direction.South, Direction.South,
                Direction.West, Direction.West, Direction.West,
                Direction.North, Direction.North,
                Direction.East, Direction.North, Direction.North
            }, steps.ToList(), String.Join(",", steps.ToArray()));
        }

        [TestMethod]
        public void RHWay_find_start_not_on_way()
        {
            var maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "     ",
                "     ",
                " ! x ",
                "     ",
                "     "
            });
            var way = finder.DefineWay(0, maze, maze.Point.X, maze.Point.Y);
            WalkableMap.Create(maze).BuildMapFrom(maze.Point, 10);
            Assert.AreEqual(8, way.All.Count());
            var cell = way.GetCell(0);
            Assert.AreEqual(Point.Get(2, 2), cell.Point);

            var howto = finder.HowToGoToWay(0, maze, maze.Point.X, maze.Point.Y);
            CollectionAssert.AreEqual(new Direction[] { Direction.East }, howto.ToArray(), String.Join(",", howto.Select(h => h.ToString())));

        }

        [TestMethod]
        public void RHWay_find()
        {
            var maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "   ",
                "   ",
                "x!x",
                "   ",
                "   "

            });
            WalkableMap.Create(maze).BuildMapFrom(maze.Point, 10);
            var way = finder.DefineWay(0, maze, maze.Point.X, maze.Point.Y);
            var index = way.GetIndex(maze.Point);
            var cell = way.GetCell(index);
            var all = way.All;
            Console.WriteLine(all);
            Assert.AreEqual(maze.Point, cell.Point);
            Assert.AreEqual(Direction.South, cell.Direction);
            Assert.AreEqual(1, cell.NextIndex);

            CollectionAssert.AreEqual(new Direction[] {
                Direction.South,
                Direction.West, Direction.South, Direction.East, Direction.East,
                Direction.North, Direction.West, Direction.North,
                Direction.North, Direction.East,
                Direction.North, Direction.West, Direction.West, Direction.South, Direction.East,
                Direction.South
            }, all.Select(d => d.Direction).ToArray());

            var howto = finder.HowToGoToWay(0, maze, maze.Point.X, maze.Point.Y);
            CollectionAssert.AreEqual(new Direction[0], howto.ToArray());

        }

        [TestMethod]
        public void RHWay_deadends()
        {
            var maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "xxxxxxxxx ! ",
                "xx        x ",
                "xxxxxxxxx   ",
            });

            var way = finder.DefineWay(0, maze, maze.Point.X, maze.Point.Y);
            var all = way.All;
            Assert.AreEqual(8, all.Count());

        }

        [TestMethod]
        public void RHWay_HowToGoToWay()
        {
            var maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "xxxxxxxxxxxxxxxxxxxx",
                "x                  x",
                "x  !               x",
                "x                  x",
                "x                  x",
                "x                  x",
                "xxxxxxxxxxxxxxxxxxxx",
            });
            var way = finder.DefineWay(0, maze, maze.Point.X, maze.Point.Y);
            var MA = WalkableMap.Create(maze);
            var p = MA.BuildMapFrom(maze.Point, 10);
            var way1 = finder.HowToGoToWay(0, maze, maze.Point.X, maze.Point.Y);
            CollectionAssert.AreEqual(new List<Direction>
            {
                Direction.North
            }, way1.ToList());
        }
    }
}
