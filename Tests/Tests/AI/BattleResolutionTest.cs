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




        class DumbMaze : IMaze, IWarriorMaze<Warrior2Mock>
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


            public bool CanAttack(int xFrom, int yFrom, TrooperStance stance, int xTo, int yTo)
            {
                return true;
            }


            public int DangerIndex(int x, int y, TrooperStance stance = TrooperStance.Standing)
            {
                return 1;
            }
        }

        [TestMethod]
        public void Test_Shoot2()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(4, 0);
            var self = new Warrior2Mock { AActions = 13, ADamage = 20, AHitpoints = 100, AAttackRange = 5, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 20, AHitpoints = 190, AAttackRange = 5, ALocation = enemyLoc };

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
            var allyLoc = MA.Get(5, 0);
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
            var self = new Warrior2Mock { AActions = 4, ADamage = 25, AHitpoints = 100, AAttackRange = 6, ALocation = selfLoc, AHasRation = true, ShootCost = 2 };
            var enemy = new Warrior2Mock { AActions = 8, ADamage = 15, AHitpoints = 100, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.EatFieldRation, ActionType.LowerStance, ActionType.Shoot, ActionType.Shoot}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
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
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.LowerStance, ActionType.LowerStance, ActionType.EatFieldRation, ActionType.Shoot, ActionType.Shoot, ActionType.Shoot}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }

        [TestMethod]
        public void Test_Prone()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var self = new Warrior2Mock { AActions = 7, ADamage = 10, AHitpoints = 70, AAttackRange = 6, ALocation = selfLoc};
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 10, AHitpoints = 30, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.LowerStance, ActionType.LowerStance, ActionType.Shoot}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_Move_After_Prone()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 12);
            var enemyLoc = MA.Get(7, 0);
            var self = new Warrior2Mock { AActions = 9, ADamage = 10, AHitpoints = 70, AAttackRange = 6, ALocation = selfLoc, APosition = TrooperStance.Prone };
            var enemy = new Warrior2Mock { AActions = 6, ADamage = 4, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.RaiseStance, ActionType.RaiseStance, ActionType.Move, ActionType.Shoot }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());

        }

        [TestMethod]
        public void Test_Damage()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(2, 0), 10);
            var enemyLoc = MA.Get(8, 0);
            var self = new Warrior2Mock { AActions = 4, ADamage = 2, AHitpoints = 55, AAttackRange = 6, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 6, ADamage = 10, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.Move }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }
        [TestMethod]
        public void Test_Damage_more()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(2, 0), 10);
            var enemyLoc = MA.Get(8, 0);
            var self = new Warrior2Mock { AActions = 4, ADamage = 8, AHitpoints = 55, AAttackRange = 6, ALocation = selfLoc };
            var enemy = new Warrior2Mock { AActions = 6, ADamage = 10, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.Move, ActionType.Move }, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }
        [TestMethod]
        public void Test_Medkit()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 0), 10);
            var enemyLoc = MA.Get(6, 0);
            var self = new Warrior2Mock { AActions = 5, ADamage = 9, AHitpoints = 30, AAttackRange = 6, ALocation = selfLoc, AHasMedkit = true };
            var enemy = new Warrior2Mock { AActions = 8, ADamage = 9, AHitpoints = 40, AAttackRange = 6, ALocation = enemyLoc };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { });
            var resolution = Battle.All(battle);
            CollectionAssert.AreEqual(new List<ActionType> { ActionType.UseMedikit, ActionType.Shoot}, resolution.First(m => m.Possible).Moves.Select(m => m.Action).ToList());
        }

        [TestMethod]
        public void Case1()
        {
            var maze = new DumbMaze();
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(Point.Get(0, 5), 30);
            var medicLoc = MA.Get(1, 0);
            var solLoc = MA.Get(0, 1);
            var comLoc = MA.Get(2, 2);
            var self = new Warrior2Mock { AActions = 10, ADamage = 25, AHitpoints = 70, AAttackRange = 8, ALocation = selfLoc, AHasMedkit = true, AHasGrenade = true };
            var med = new Warrior2Mock { AActions = 12, ADamage = 9, AHitpoints = 100, AAttackRange = 5, ALocation = medicLoc };
            var com = new Warrior2Mock { AActions = 10, ADamage = 15, AHitpoints = 100, AAttackRange = 8, ALocation = comLoc};
            var sol = new Warrior2Mock { AActions = 12, ADamage = 25, AHitpoints = 120, AAttackRange = 9, ALocation = solLoc, AHasGrenade = true };
            var battle = new BattleCase2<Warrior2Mock>(self, med, maze, new List<Warrior2Mock> { }, new List<Warrior2Mock> {com, sol});
            var resolution = Battle.All(battle);
        }


        [TestMethod]
        public void Case2()
        {
            var maze = new MockMaze<Warrior2Mock>(new string[] {
                "                     ",
                "                     ",
                "          !          ",
                "                     ",
                "       xxxx          ",
                "       xxxx          ",
                "       xxxx          ",
                "       xxxx          ",
                "          cm         ",
                "          s          ",

            });
            var MA = WalkableMap.Create(maze);
            var selfLoc = MA.BuildMapFrom(maze.Trooper(TrooperType.FieldMedic), 30);
            var enemyLoc = MA.Get(maze.FindChar('!'));
            var solLoc = MA.Get(maze.Trooper(TrooperType.Soldier));
            var comLoc = MA.Get(maze.Trooper(TrooperType.Commander));
            var self = new Warrior2Mock { AActions = 12, ADamage = 9, AHitpoints = 100, AAttackRange = 5, ALocation = selfLoc };
            var com = new Warrior2Mock { AActions = 10, ADamage = 15, AHitpoints = 100, AAttackRange = 8, ALocation = comLoc };
            var sol = new Warrior2Mock { AActions = 12, ADamage = 25, AHitpoints = 120, AAttackRange = 9, ALocation = solLoc, AHasGrenade = true };
            var enemy = new Warrior2Mock { AActions = 10, ADamage = 15, AHitpoints = 100, AAttackRange = 7, ALocation = enemyLoc, AHasGrenade = true };
            var battle = new BattleCase2<Warrior2Mock>(self, enemy, maze, new List<Warrior2Mock> { com, sol });
            var resolution = Battle.All(battle);
        }
    
    
    }
}
