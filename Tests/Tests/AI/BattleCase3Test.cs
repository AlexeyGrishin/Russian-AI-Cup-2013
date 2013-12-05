using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;

namespace Tests.AI
{
    [TestClass]
    public class BattleCase3Test
    {
        [TestMethod]
        public void TestCopy()
        {
            var state = new BattleWarrior3State();
            state.InitialHitpoints = 100;
            state.Damage = 20;
            state.Healed = 10;
            Assert.AreEqual(90, state.Hitpoints);

            var state2 = state.Copy();
            Assert.AreEqual(90, state2.Hitpoints);
            Assert.AreEqual(20, state2.Damage);
            Assert.AreEqual(10, state2.Healed);

            
        }

        [TestMethod]
        public void TestRealDamage()
        {
            var state = new BattleWarrior3State();
            state.InitialHitpoints = 100;
            state.Damage = 20;
            state.Healed = 10;
            Assert.AreEqual(20, state.RealDamage);
            state.Damage = 100;
            Assert.AreEqual(100, state.RealDamage);
            state.Damage = 120;
            Assert.AreEqual(110, state.RealDamage);

        }
    }
}
