using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{
    public class BattleStrategy
    {

        private static List<Trooper> oldEnemies = new List<Trooper>();
        private static int oldEnemiesTurn = -5;

        //внутри этой магии мы вспоминаем виденных ранее врагов и прикидываем на месте ли они еще
        public bool IsInDanger(Trooper self, World world)
        {
            var visibleEnemies = world.Troopers.Where(t => !t.IsTeammate);
            if (MyStrategy.Turn - oldEnemiesTurn > 5)
            {
                oldEnemies.Clear();
            }
            else {
                oldEnemies = oldEnemies.Where(oe => !visibleEnemies.Any(ve => ve.Id == oe.Id)).ToList();
                foreach (var oldEnemy in oldEnemies.ToList())
                {
                    var shallBeVisible = world.Troopers.Where(t => t.IsTeammate).Any(t => world.IsVisible(t.VisionRange, t.X, t.Y, t.Stance, oldEnemy.X, oldEnemy.Y, oldEnemy.Stance));
                    if (shallBeVisible)
                    {
                        Console.WriteLine("Seems like old enemy " + oldEnemy.Type + " gone away");  //[DEBUG]
                        oldEnemies.Remove(oldEnemy);
                    }
                    else if (oldEnemy.Ext().WasntReallyShoot(world.GetScore()))
                    {
                        Console.WriteLine("We've made a shoot at the enemy " + oldEnemy.Type + ", but score did not changed. So it gone away");  //[DEBUG]
                        oldEnemies.Remove(oldEnemy);
                    }
                    else
                    {
                        oldEnemy.Ext().Noticed = true;
                    }
                    
                }
            }
            return visibleEnemies.Count() + oldEnemies.Count > 0;
        }

        private static int howManySeenBefore = -1;

        //просчет боя
        public void DoMove(Trooper self, World world, Move move)
        {
            var enemies = world.Troopers.Where(t => !t.IsTeammate);
            self.Ext().CheckAttackList(enemies.Concat(oldEnemies).Select(e => e.GetPosition()));
            foreach (var enemy in enemies) enemy.Ext().Noticed = false;
            Console.WriteLine("Visible enemies are: " + String.Join(",", enemies.Select(e => e.Type + "(" + e.X + "," + e.Y + ")")));  //[DEBUG]
            Console.WriteLine("Old known enemies are: " + String.Join(",", oldEnemies.Select(e => e.Type + "(" + e.X + "," + e.Y + ")")));  //[DEBUG]
            //если кол-во виденных врагов поменялось - отменяем старые приказы
            if (enemies.Count() + oldEnemies.Count() != howManySeenBefore)
            {
                self.Ext().NextMoves.Clear();
                Console.WriteLine("Reset battle plan - we have new guys visible"); //[DEBUG]
            }
            enemies = enemies.Concat(oldEnemies).OrderBy(e => e.Hitpoints);
            howManySeenBefore = enemies.Count();
            if (!self.Ext().HasNextMove())
            {
                //составляем снимок битвы
                var allies = world.Troopers.Where(t => t.IsTeammate && t.Id != self.Id).ToList();
                var battleCase3 = new BattleCase3<TrooperExt>(self.Ext(), world.ToMaze(), allies.Select(a => a.Ext()).ToList(), enemies.Select(a => a.Ext()).ToList());

                bool done = false;
                //и гоняем по разным вариантам
                List<StrategyResult3> resolutions3 = Battle.All3(battleCase3).ToList();
                resolutions3.Sort();
                resolutions3.Reverse();
                //выбираем лучший
                var best3 = resolutions3.FirstOrDefault(r => r.Possible);

                Logger.Logger.Instance().LogBattle3(world, battleCase3, resolutions3, best3);       //[DEBUG]
                if (best3 != null)
                {
                    self.Ext().AddMoves(best3.Moves);
                    done = true;
                }

                if (!done)
                {
                    Console.WriteLine("Do not know what to do :(");  //[DEBUG]
                    self.Ext().AddMove(new Move().Wait());
                }
                if (!self.Ext().HasNextMove())
                {
                    self.Ext().AddMove(new Move().Wait());
                }
            }
            if (oldEnemies.Count == 0)
                oldEnemiesTurn = MyStrategy.Turn;
            oldEnemies = enemies.ToList();
            //если остались ходы от прошлого выбора - ходим
            self.Ext().ExecuteMove(move);
            PostProcessMove(self, move, world);

        }

        //если мы пуляем в невидимого врага, надо в следующий ход отследить по очкам получил ли он в щи или нет
        private void PostProcessMove(Trooper self, Move move, World world)
        {
            foreach (var enemy in oldEnemies)
            {
                enemy.Ext().WasNotShoot();
            }
            if (move.Action == ActionType.Shoot)
            {
                var oldEnemyThere = oldEnemies.Where(oe => oe.X == move.X && oe.Y == move.Y).FirstOrDefault();
                if (oldEnemyThere == null)
                {
                    Console.WriteLine("Very strange...");  //[DEBUG]
                }
                else
                {
                    var damage = self.Damage;
                    if (damage > oldEnemyThere.Hitpoints)
                    {
                        Console.WriteLine("Goodbye " + oldEnemyThere.Type);  //[DEBUG]
                        oldEnemies.Remove(oldEnemyThere);   //we do not need ghosts
                    }
                    else
                    {
                        oldEnemyThere.Ext().WasShoot(world.GetScore());
                    }
                }
            }
            if (move.Action == ActionType.ThrowGrenade)
            {
                foreach (var enemyAround in oldEnemies.Where(oe => Tool.GetDistance(oe, move) <= 1).ToList())
                {
                    var damage = self.Ext().GetGrenadeDamage(Tool.GetDistance(enemyAround, move));
                    if (damage > enemyAround.Hitpoints)
                    {
                        Console.WriteLine("Goodbye " + enemyAround.Type);  //[DEBUG]
                        oldEnemies.Remove(enemyAround);   //we do not need ghosts
                    }
                    else
                    {
                        enemyAround.Ext().WasShoot(world.GetScore());
                    }
                }
            }
        }

        public void CancelSteps(Trooper self)
        {
            self.Ext().NextMoves.Clear();
        }

        static int imagId = 1000;

        public bool ImagineEnemy(Trooper self, Move move, World world, Game game)
        {
            var maxAttackRadius = MyStrategy.AreThereSnipers.GetValueOrDefault(false) ? 12 : 9;
            var allies = world.Troopers.Where(a => a.IsTeammate);
            var possibleEnemies = new List<PossibleMove>();
            for (var dx = -maxAttackRadius; dx <= maxAttackRadius; dx++)
            {
                for (var dy = -maxAttackRadius; dy <= maxAttackRadius; dy++)
                {
                    var p = WalkableMap.Instance().Get(Point.Get(self.X + dx, self.Y + dy));
                    if (p != null && !allies.Any(a => world.IsVisible(a.VisionRange, a.X, a.Y, a.Stance, p.X, p.Y, a.Stance)) && world.IsVisible(maxAttackRadius, p.X, p.Y, self.Stance, self.X, self.Y, self.Stance))
                        possibleEnemies.Add(p);
                }
            }
            possibleEnemies = possibleEnemies.OrderBy(p => p.RealDistanceTo(self.Ext().Location)).Reverse().ToList();
            var enemyPos = possibleEnemies.FirstOrDefault();
            if (enemyPos != null)
            {
                var enemy = new Trooper (imagId++, enemyPos.X, enemyPos.Y, 10, 1, false, TrooperType.Sniper, self.Stance, 99, 99, 10, 10, maxAttackRadius, maxAttackRadius, self.ShootCost, self.StandingDamage, self.KneelingDamage, self.ProneDamage, 66, false, false, false);
                enemy.Ext().Game = game;
                Console.WriteLine("Imagine enemy at " + enemyPos);
                oldEnemies.Add(enemy);
                oldEnemiesTurn = MyStrategy.Turn;
                return true;
            }
            return false;
            
        }
    }
}
