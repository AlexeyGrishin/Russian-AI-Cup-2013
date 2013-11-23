using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;

namespace Tests.AI
{
    [TestClass]
    public class GroupObserver2Test
    {
        class MoveableMock: Moveable
        {
            static int order = 0;

            public MoveableMock(MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock> maze, TrooperType type)
            {
                var t = maze.Trooper(type);
                this.type = type;
                X = t.X;
                Y = t.Y;
                TurnOrder = order++;
            }
            public ActionType? action = null;
            public Direction? direction = null;
            public string reason = null;
            private TrooperType type;

            public void DoMove(Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model.Direction direction, int wayIdx)
            {
                action = ActionType.Move;
                this.direction = direction;
                var newPos = Point.Get(X, Y).To(this.direction);
                X = newPos.X;
                Y = newPos.Y;
                WayIndex = wayIdx;
            }

            public void Wait(string reason)
            {
                action = ActionType.EndTurn;
                this.reason = reason;
            }

            public int WayIndex { get; set; }
            public bool OnWay { get; set; }
            public int TurnOrder { get; set; }

            public int X { get; set; }
            public int Y { get; set; }

            public override string ToString()
            {
                return type.ToString();
            }
        }

        private MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock> maze;
        private Group2 group;
        private GroupObserver2 groupObserver;

        [TestMethod]
        public void LastOne()
        {
            maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "     c",
                "     m",
                "      ",
                "      ",
                "     s",
                "      "
            });
            var commander = new MoveableMock(maze, TrooperType.Commander);
            var medic = new MoveableMock(maze, TrooperType.FieldMedic);
            var soldier = new MoveableMock(maze, TrooperType.Soldier);
            WalkableMap.Create(maze).BuildMapFrom(Point.Get(commander.X, commander.Y), 10, p => !maze.HasNotWallOrUnit(p.X, p.Y));
            group = new Group2(new List<Moveable> { commander, soldier , medic });
            groupObserver = new GroupObserver2(min: 2, max: 3);
            groupObserver.OnTurn(medic, maze);
            SuggestMove(soldier);
            AssertMove(soldier, Direction.North);
        }


        [TestMethod]
        public void GoBack()
        {
            maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "s    c",
                "     m",
                "      ",
                "      ",
                "      ",
                "      ",
                "      ",
                "      "
            });
            var commander = new MoveableMock(maze, TrooperType.Commander);
            var medic = new MoveableMock(maze, TrooperType.FieldMedic);
            var soldier = new MoveableMock(maze, TrooperType.Soldier);
            WalkableMap.Create(maze).BuildMapFrom(Point.Get(commander.X, commander.Y), 10, p => !maze.HasNotWallOrUnit(p.X, p.Y));
            group = new Group2(new List<Moveable> { commander, soldier, medic });
            groupObserver = new GroupObserver2(min: 2, max: 3, back: 4);
            groupObserver.OnTurn(medic, maze);
            SuggestMove(soldier);
            AssertMove(soldier, Direction.East);

        }

        [TestMethod]
        public void MainSuccess()
        {
            maze = new MockMaze<Tests.AI.BattleResolutionTest.Warrior2Mock>(new string[] {
                "      ",
                "      ",
                "   c  ",
                "    s ",
                "     m",
                "      "
            });
            var commander = new MoveableMock(maze, TrooperType.Commander);
            var medic = new MoveableMock(maze, TrooperType.FieldMedic);
            var soldier = new MoveableMock(maze, TrooperType.Soldier);
            WalkableMap.Create(maze).BuildMapFrom(Point.Get(commander.X, commander.Y), 10, p => !maze.HasNotWallOrUnit(p.X, p.Y));
            group = new Group2(new List<Moveable> { commander, soldier, medic });
            groupObserver = new GroupObserver2(min: 2, max: 3, back: 10);

            groupObserver.OnTurn(commander, maze);
            //commander goes to right and stops waiting for other participants
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
            SuggestMove(commander);
            AssertNoMove(commander);
            AssertOnWay(commander);

            SuggestMove(medic);
            AssertNoMove(medic);
            AssertOnWay(medic);

            SuggestMove(soldier);
            AssertMove(soldier, Direction.East);
            SuggestMove(soldier);
            AssertNoMove(soldier);
            AssertOnWay(soldier);

            SuggestMove(commander);
            AssertMove(commander, Direction.North);
            SuggestMove(commander);
            AssertMove(commander, Direction.North);
            SuggestMove(commander);
            AssertMove(commander, Direction.West);
            SuggestMove(commander);
            //here we have distance from soldier == 4 - stop
            AssertNoMove(commander);

            SuggestMove(medic);
            AssertNoMove(medic);    //too close TODO: step back!

            SuggestMove(soldier);
            AssertMove(soldier, Direction.North);
            SuggestMove(soldier);
            AssertMove(soldier, Direction.North);

            //try medic again - shall get only 1 step
            SuggestMove(medic);
            AssertMove(medic, Direction.North);
            SuggestMove(medic);
            AssertNoMove(medic);    //too close

        }

        private void SuggestMove(MoveableMock trooper)
        {
            WalkableMap.Instance().BuildMapFrom(Point.Get(trooper.X, trooper.Y), 10);
            groupObserver.SuggestMove(trooper, group, maze);
        }

        private void AssertOnWay(MoveableMock trooper)
        {
            Assert.IsTrue(trooper.OnWay);
            Assert.AreNotEqual(-1, trooper.WayIndex);
        }

        private void AssertMove(MoveableMock trooper, Direction dir)
        {
            Assert.AreEqual(ActionType.Move, trooper.action, trooper.action == ActionType.EndTurn ? trooper.reason : "Unexpected state");
            Assert.AreEqual(dir, trooper.direction);
            trooper.action = null;
        }

        private void AssertNoMove(MoveableMock trooper)
        {
            Assert.AreEqual(ActionType.EndTurn, trooper.action, trooper.action == ActionType.Move ? trooper.direction.ToString() : "");
            trooper.action = null;
        }
    }



}
