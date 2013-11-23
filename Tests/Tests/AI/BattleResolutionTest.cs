using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using System.Collections.Generic;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk;

namespace Tests.AI
{
    [TestClass]
    public class BattleResolutionTest
    {



        public class Warrior2Mock : Warrior2
        {

            public int AHitpoints { get; set; }
            public override int Hitpoints
            {
                get { return AHitpoints; }
            }

            public override bool IsSick
            {
                get { return AHitpoints <= MyStrategy.NeedHeeling; }
            }

            

            public int AActions { get; set; }
            public override int Actions
            {
                get { return AActions; }
            }


            public override int GetDamage(Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model.TrooperStance stance)
            {
                return stance == TrooperStance.Standing ? ADamage : ADamage * 2;
            }

            public override int GetDamage()
            {
                return ADamage;
            }
            public int ADamage { get; set; }


            public bool AHasRation { get; set; }
            public override bool HasFieldRation
            {
                get { return AHasRation; }
            }

            public bool AHasMedkit { get; set; }
            public override bool HasMedkit
            {
                get { return AHasMedkit; }
            }

            public override int MaxActions
            {
                get { return 10; }
            }

            public bool AIsMedic { get; set; }
            public override bool IsMedic
            {
                get { return AIsMedic; }
            }

            public override int MedkitHealth
            {
                get { return 20; }
            }

            public PossibleMove ALocation { get; set; }
            public override PossibleMove Location
            {
                get { return ALocation;  }
            }

            public int AAttackRange { get; set; }
            public override int AttackRange
            {
                get { return AAttackRange; }
            }

            public bool AHasGrenade { get; set; }
            public override bool HasGrenade
            {
                get { return AHasGrenade; }
            }

            public override int GrenadeRange
            {
                get { return 5; }
            }

            public override int GetGrenadeDamage(int delta)
            {
                return delta == 0 ? 80 : 60;
            }

            public override int Cost(ActionType type)
            {
                return type == ActionType.ThrowGrenade ? 8 : 2;
            }

            public override int FieldRationExtraPoints
            {
                get { return 3; }
            }

            public override int MedicHealth
            {
                get { return 30; }
            }
        }

        class DumbMaze : IWalkingMaze, IWarriorMaze<Warrior2Mock>
        {

            public int Width
            {
                get { return 100; }
            }

            public int Height
            {
                get { return 100; }
            }

            public bool IsFree(int x, int y)
            {
                return true;
            }

            public bool HasNotWallOrUnit(int x, int y)
            {
                return true;
            }

            public bool CanAttack(Warrior2Mock attacker, int xFrom, int yFrom, Warrior2Mock attackWho, int xTo, int yTo)
            {
                return true;
            }
        }

        [TestMethod]
        public void Test_Shoot2()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(4, 0);
            var self = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = enemyLoc };

            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze);
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.LowerStance, ActionType.Shoot, ActionType.Shoot, ActionType.Shoot, ActionType.RaiseStance }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }

        [TestMethod]
        public void Test_JustGo()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(40, 0);
            var self = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = enemyLoc };
            var allyLoc = MA.Get(32, 0);
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };

            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> {ally});
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.Move, ActionType.Move, ActionType.Move, ActionType.Move }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_MoveClose()
        {
            var maze = new MockMaze<Warrior2Mock>(new string[] {
                "xxxxx",
                "x!* x",
                "xxxxx"
            });
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(maze.Point, 10);
            var enemyLoc = MA.Get(maze.FindChar('*'));
            var self = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze);
            var resolution = Battle.All(battle);
            
        }
        
        [TestMethod]
        public void Test_Grenade2()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var self = new Warrior2Mock { AActions = 11, ADamage = 15, AHitpoints = 50, AAttackRange = 5, AHasGrenade = true, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 80, AAttackRange = 6, AHasGrenade = true, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze);
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.ThrowGrenade }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }

        [TestMethod]
        public void Test_GrenadeAllies()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var allyLoc = MA.Get(7, 0);
            var self = new Warrior2Mock { AActions = 12, ADamage = 15, AHitpoints = 70, AAttackRange = 5, AHasGrenade = true, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 12, ADamage = 5, AHitpoints = 80, AAttackRange = 6, ALocation = enemyLoc };
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> {ally});
            var resolution = Battle.All(battle);
            Assert.IsFalse(resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList().Contains(ActionType.ThrowGrenade));

        }

        [TestMethod]
        public void Test_Heal()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var allyLoc = MA.Get(2, 1);
            var self = new Warrior2Mock { AIsMedic = true, AActions = 12, ADamage = 3, AHitpoints = 70, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 12, ADamage = 5, AHitpoints = 80, AAttackRange = 6, ALocation = enemyLoc };
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { ally });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.Move, ActionType.Heal, ActionType.Heal, ActionType.Heal, ActionType.Heal }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_Heal_near()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var allyLoc = MA.Get(1, 0);
            var self = new Warrior2Mock { AIsMedic = true, AActions = 12, ADamage = 3, AHitpoints = 70, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 12, ADamage = 5, AHitpoints = 80, AAttackRange = 6, ALocation = enemyLoc };
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { ally });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Heal, ActionType.Heal, ActionType.Heal, ActionType.Heal, ActionType.Heal, ActionType.Heal }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }


        [TestMethod]
        public void Test_Go_To_Sick_Event_Far()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 30);
            var enemyLoc = MA.Get(20, 0);
            var allyLoc = MA.Get(18, 1);
            var self = new Warrior2Mock { AIsMedic = true, AActions = 6, ADamage = 3, AHitpoints = 70, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 12, ADamage = 5, AHitpoints = 80, AAttackRange = 6, ALocation = enemyLoc };
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { ally });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.Move, ActionType.Move }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_Attack_Even_Sick_If_Can_Kill()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var allyLoc = MA.Get(2, 1);
            var self = new Warrior2Mock { AIsMedic = true, AActions = 12, ADamage = 3, AHitpoints = 70, AAttackRange = 10, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 12, ADamage = 5, AHitpoints = 9, AAttackRange = 6, ALocation = enemyLoc };
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { ally });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.LowerStance, ActionType.Shoot, ActionType.Shoot, ActionType.RaiseStance }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_Heal_Event_Can_Kill_If_Our_Sick_Could_Be_Dead()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var allyLoc = MA.Get(2, 1);
            var self = new Warrior2Mock { AIsMedic = true, AActions = 6, ADamage = 3, AHitpoints = 70, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 10, AHitpoints = 80, AAttackRange = 6, ALocation = enemyLoc };
            var ally = new Warrior2Mock { AActions = 12, ADamage = 40, AHitpoints = 50, AAttackRange = 6, ALocation = allyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { ally });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.Move, ActionType.Heal }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_EatRation()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var self = new Warrior2Mock { AActions = 4, ADamage = 10, AHitpoints = 70, AAttackRange = 6, ALocation = selfLoc, AHasRation = true };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 10, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.EatFieldRation, ActionType.Shoot, ActionType.Shoot, ActionType.Shoot}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }

        [TestMethod]
        public void Test_EatRation_Max()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var self = new Warrior2Mock { AActions = 10, ADamage = 5, AHitpoints = 70, AAttackRange = 6, ALocation = selfLoc, AHasRation = true };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 10, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.LowerStance, ActionType.EatFieldRation, ActionType.Shoot, ActionType.Shoot, ActionType.Shoot, ActionType.Shoot, ActionType.RaiseStance}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }

        [TestMethod]
        public void Test_Medkit()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var self = new Warrior2Mock { AActions = 4, ADamage = 10, AHitpoints = 30, AAttackRange = 6, ALocation = selfLoc, AHasMedkit = true };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 9, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.UseMedikit, ActionType.Shoot}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }
    
    
    }
}
