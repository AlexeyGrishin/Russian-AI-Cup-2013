using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System.Collections.Generic;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;

namespace Tests.AI
{
    [TestClass]
    public class TrooperExtTest
    {
        private TrooperExt ext;

        [TestInitialize]
        public void SetUp()
        {
            ext = new TrooperExt(new Trooper(1, 1, 1, 1, 1, true, TrooperType.Commander, TrooperStance.Standing, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, false, false, false));

        }

        [TestMethod]
        public void NextMoves_moving()
        {
            ext.AddMove(new Move { Action = ActionType.Move, X = 3, Y = 4 });
            Assert.IsTrue(ext.HasNextMove());
            ext.CheckAttackList(new List<Point>());
            Assert.IsTrue(ext.HasNextMove());
        }

        [TestMethod]
        public void NextMoves_attackRealEnemy()
        {
            ext.AddMove(new Move { Action = ActionType.Move, X = 2, Y = 4 });
            ext.AddMove(new Move { Action = ActionType.Shoot, X = 3, Y = 4 });
            Assert.IsTrue(ext.HasNextMove());
            ext.CheckAttackList(new List<Point> {Point.Get(3, 4)});
            Assert.IsTrue(ext.HasNextMove());
        }

        [TestMethod]
        public void NextMoves_attackEnemyDisappeared()
        {
            ext.AddMove(new Move { Action = ActionType.Move, X = 2, Y = 4 });
            ext.AddMove(new Move { Action = ActionType.Shoot, X = 3, Y = 4 });
            Assert.IsTrue(ext.HasNextMove());
            ext.CheckAttackList(new List<Point> { });
            Assert.IsFalse(ext.HasNextMove());
        }

        [TestMethod]
        public void NextMoves_attackGrenadeExact()
        {
            ext.AddMove(new Move { Action = ActionType.Move, X = 2, Y = 4 });
            ext.AddMove(new Move { Action = ActionType.ThrowGrenade, X = 3, Y = 4 });
            Assert.IsTrue(ext.HasNextMove());
            ext.CheckAttackList(new List<Point> { Point.Get(3, 4) });
            Assert.IsTrue(ext.HasNextMove());
        }

        [TestMethod]
        public void NextMoves_attackGrenadeNear()
        {
            ext.AddMove(new Move { Action = ActionType.Move, X = 2, Y = 4 });
            ext.AddMove(new Move { Action = ActionType.ThrowGrenade, X = 3, Y = 4 });
            Assert.IsTrue(ext.HasNextMove());
            ext.CheckAttackList(new List<Point> { Point.Get(3, 3) });
            Assert.IsTrue(ext.HasNextMove());
        }

        [TestMethod]
        public void NextMoves_attackGrenadeNone()
        {
            ext.AddMove(new Move { Action = ActionType.Move, X = 2, Y = 4 });
            ext.AddMove(new Move { Action = ActionType.ThrowGrenade, X = 3, Y = 4 });
            Assert.IsTrue(ext.HasNextMove());
            ext.CheckAttackList(new List<Point> { Point.Get(4, 3) });
            Assert.IsFalse(ext.HasNextMove());
        }
    }
}
