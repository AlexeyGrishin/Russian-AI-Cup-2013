using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk;
using System.Collections.Generic;

namespace Tests.AI
{
    [TestClass]
    public class FollowPointTest
    {

        Warrior2Mock soldier, commander, medic;
        List<Warrior2> all;
        FollowPoint walker;
        Move move;
        MockMaze<Warrior2> maze;
        
        [TestInitialize]
        public void SetUp()
        {
            soldier = null;
            commander = null;
            medic = null;
            move = new Move();
        }

        private void Init(string[] map, int backDistance = 5, Action<WalkableMap, Point, Point, Point> action = null, Action postMazeCreate = null)
        {
            maze = new MockMaze<Warrior2>(map);
            if (postMazeCreate != null) postMazeCreate();
            var soldierPoint = maze.Trooper(TrooperType.Soldier);
            var commanderPoint = maze.Trooper(TrooperType.Commander);
            var medicPoint = maze.Trooper(TrooperType.FieldMedic);
            var MA = WalkableMap.Create(maze);
            if (commanderPoint != null)
                commander = new Warrior2Mock { AActions = 10, AType = TrooperType.Commander, AVisionRange = 8, ALocation = MA.Get(commanderPoint) };
            if (soldierPoint != null)
                soldier = new Warrior2Mock { AActions = 10, AType = TrooperType.Soldier, AVisionRange = 7, ALocation = MA.Get(soldierPoint) };
            if (medicPoint != null)
                medic = new Warrior2Mock { AActions = 10, AType = TrooperType.FieldMedic, AVisionRange = 7, ALocation = MA.Get(medicPoint) };

            all = (new List<Warrior2> { commander, medic, soldier }).Where(w => w != null).ToList();
            if (action != null) action(MA, commanderPoint, soldierPoint, medicPoint);
            walker = new FollowPoint(all.First(), backDistance, MA);
        }
        
        [TestMethod]
        public void Walls()
        {
            Init(new string[] {
                "x    ",
                " c   ",
                "     ",
                "     ",
                "     "
            }, action: (map, c, s, m) => map.BuildMapFrom(c, 20));
            SuggestMove(commander);
            AssertMove(commander, Direction.North);

        }
        [TestMethod]
        public void Single_Observe()
        {
            Init(new string[] {
                "c  ",
                "   ",
                "   ",
                "   ",
                "   "
            }, action: (map, c, s, m) => map.BuildMapFrom(c, 20));
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
            SuggestMove(commander);
            AssertMove(commander, Direction.South);
            SuggestMove(commander);
            AssertMove(commander, Direction.South);
            SuggestMove(commander);
            AssertMove(commander, Direction.North);
            commander.AActions = 10;
            SuggestMove(commander);
            AssertMove(commander, Direction.North);
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
        }

        [TestMethod]
        public void Danger()
        {
            Init(new string[] {
                "cx                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                " x                                               ",
                "                                                 "
            }, 4, (map, c, s, m) => map.BuildMapFrom(c, 20), ()=> {
                maze.DangerIndexGetter = (x, y) => (x == 0 && y == 4) ? 10 : 0;
            });
            SuggestMove(commander);
            AssertMove(commander, Direction.South);
            SuggestMove(commander);
            AssertMove(commander, Direction.South);
            SuggestMove(commander);
            AssertMove(commander, Direction.South);
            SuggestMove(commander);
            AssertNoMove(commander);

        }

        [TestMethod]
        public void MainSuccessScenario()
        {
            Init(new string[] {
                "cms         ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            ",
                "            "
            }, 4, (map, c,s,m) => map.BuildMapFrom(c, 20));
            commander.AActions = 500;
            medic.AActions = 500;
            soldier.AActions = 500;

            SuggestMove(commander);
            //TODO: fix tests later
            AssertMove(commander, Direction.South);
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
            SuggestMove(commander);
            AssertMove(commander, Direction.East);
            SuggestMove(commander);
            AssertMove(commander, Direction.North);
            SuggestMove(commander);
            AssertMove(commander, Direction.West);    //becasue medic is far

            SuggestMove(medic);
            AssertMove(medic, Direction.South);
            SuggestMove(medic);
            AssertMove(medic, Direction.East);
            SuggestMove(medic);
            AssertNoMove(medic);    //reached commander

            SuggestMove(soldier);
            AssertMove(soldier, Direction.East);
            SuggestMove(soldier);
            AssertNoMove(soldier);    //reached commander

            SuggestMove(commander);
            AssertMove(commander, Direction.East);

        }

        [TestMethod]
        public void CommanderStuck()
        {
            Init(new string[] {
                "       ",
                "m      ",
                " x     ",
                "cx     ",
                "sx     ",
                "       "
            });
            SuggestMove(medic);
            AssertMove(medic, Direction.South);
            SuggestMove(medic);
            AssertNoMove(medic);   

            //now get stuck
            SuggestMove(commander);
            AssertNoMove(commander);
            SuggestMove(medic);
            AssertMove(medic, Direction.North);
            SuggestMove(medic);
            AssertMove(medic, Direction.North);
            SuggestMove(medic);
            AssertNoMove(medic);
            SuggestMove(commander);
            AssertMove(commander, Direction.North);
        }

        private void SuggestMove(Warrior2Mock trooper)
        {
            WalkableMap.Instance().BuildMapFrom(trooper.Location.Point, 10, (m) => all.Any(t => t.Location.X == m.X && t.Location.Y == m.Y));
            walker.SuggestMove(trooper, all, move);
            if (move.Action == ActionType.Move)
            {
                trooper.ALocation = WalkableMap.Instance().Get(trooper.ALocation.Point.To(move.Direction));
                trooper.AActions -= 2;
            }
        }


        private void AssertMove(Warrior2Mock trooper, Direction dir)
        {
            Assert.AreEqual(ActionType.Move, move.Action);
            Assert.AreEqual(dir, move.Direction);
            move = new Move();
        }

        private void AssertNoMove(Warrior2 trooper)
        {
            Assert.AreEqual(ActionType.EndTurn, move.Action, move.Direction.ToString());
            move = new Move();
        }
    }
}
