using System;
using System.Linq;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Logger;//[DEBUG]
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        private readonly Random random = new Random();

        public static int Turn = 0;
        public static TrooperType Self = TrooperType.Commander;
        public static long PlayerId = -1;
        public static bool? AreThereSnipers = null;

        //Это собственно всякие настраиваемые параметры. Не все, правда, используются
        public static int NeedHeeling = 80;
        public static int NeedHeelingOutsideBattle = 95;
        public static double HealToDamageCoeff = 0.5;
        public static double OurEnemyDamageCoeff = 1.8;
        public static int BackDistance = 6;
        public static int CloseDistance = 3;
        public static int TeamCloseDistance = 2;
        public static int MinDistanceToTeamInBattle = 3;
        public static int MaxStepsEnemyWillDo = 2;
        public static double NotStandingDamageCoeff = 0.5;

        //Это для отладки
        public static void Break(int turn, TrooperType type)
        {
            if (Turn == turn && Self == type)
            {
                var a = 6;
            }
        }



        public void Move(Trooper self, World world, Game game, Move move)
        {
            Move(self, world, game, move, false);
        }

        private static bool wasInBattle = false;

        private void Move(Trooper self, World world, Game game, Move move, bool secondAttempt)
        {
            if (AreThereSnipers == null) AreThereSnipers = world.Troopers.Any(t => t.Type == TrooperType.Sniper);
            var start = DateTime.Now;   //[DEBUG]
            try
            {
                Turn = world.MoveIndex;
                PlayerId = self.PlayerId;
                Self = self.Type;
                Console.WriteLine("----- Turn: " + Turn + " [" + self.Type + "]"); //[DEBUG]
                self.Update(game);
                world.Troopers.ToList().ForEach(t => t.Update(game));
                //строим карту перемещений
                var MA = WalkableMap.Instance(world.ToMaze());
                MA.BuildMapFrom(self.GetPosition(), (int)world.Width, p => !world.ToMaze().HasNotWallOrUnit(p.X, p.Y), p => p.AllWayDangerIndex);
                Logger.Logger.Instance().LogMap(world);//[DEBUG]

                //вызываем боевой модуль. если есть опасность - он разберется что делать
                var BS = new BattleStrategy();
                bool inBattle = false;
                if (BS.IsInDanger(self, world))
                {
                    BS.DoMove(self, world, move);
                    inBattle = true;
                    wasInBattle = true;
                }
                else
                {
                    Console.WriteLine("There is no danger...");
                    BS.CancelSteps(self);
                    if (wasInBattle)
                    {
                        world.Troopers.Where(t => t.IsTeammate).ForEach(t => t.Ext().SaveHitpoints());
                        wasInBattle = false;
                    }

                }
                //полечиться, если больше нечем заняться
                if (!move.IsMade() && self.Ext().IsSick && self.IsHoldingMedikit && self.Ext().Can(ActionType.UseMedikit))
                {
                    move.Action = ActionType.UseMedikit;
                    move.Direction = Direction.CurrentPoint;
                }
                if (!inBattle)
                {
                    if (self.Ext().HasBeenDamaged())
                    {
                        Console.WriteLine("We are not in battle but we are attacked");
                        if (BS.ImagineEnemy(self, move, world, game) && !secondAttempt)
                        {
                            Move(self, world, game, move, true);
                        }
                        else
                        {
                            Console.WriteLine("Cannot find where we were attacked from... :(");
                        }
                    }
                    //не в бою лучше встать, если есть возможность
                    if (!move.IsMade() && self.Ext().Can(ActionType.RaiseStance) && self.Stance != TrooperStance.Standing)
                    {
                        move.Action = ActionType.RaiseStance;
                    }
                    //тут мы лечимся между боями
                    if (!move.IsMade() && self.Type != TrooperType.FieldMedic && self.Ext().IsABitSick && world.Troopers.Where(t => t.IsTeammate && t.Type == TrooperType.FieldMedic).Any(t => t.GetDistanceTo(self) <= 1))
                    {
                        Console.WriteLine("Allow medic to heal us");
                        self.Ext().SaveHitpoints(1);
                        return;
                    }
                    if (!move.IsMade() && self.Type == TrooperType.FieldMedic)
                    {
                        var sickNear = world.Troopers.Where(t => t.IsTeammate && t.Ext().IsABitSick).Where(t => t.GetDistanceTo(self) <= 1).FirstOrDefault();
                        if (sickNear != null)
                        {
                            Console.WriteLine("Medic gonna heal");
                            move.Action = ActionType.Heal;
                            move.X = sickNear.X;
                            move.Y = sickNear.Y;
                        }
                        //TODO: go to
                    }
                    //собираем бонусы между боями
                    if (!move.IsMade())
                    {
                        var bonusesAround = world.Bonuses.Where(b => b.GetDistanceTo(self) == 1).Where(b => self.CanTake(b.Type)).Where(b => !world.Troopers.Any(t => t.X == b.X && t.Y == b.Y));
                        if (bonusesAround.Count() > 0 && self.Ext().Can(ActionType.Move))
                        {
                            Console.WriteLine("There are bonuses around which are not on way: {0}", String.Join(",", bonusesAround.Select(b => b.Type.ToString())));
                            var bonus = bonusesAround.FirstOrDefault();
                            move.Action = ActionType.Move;
                            move.X = bonus.X;
                            move.Y = bonus.Y;
                        }
                    }
                    //ну и если ничего другого не остается, то перемещаемся
                    if (!move.IsMade() && self.ActionPoints >= game.StandingMoveCost)
                    {
                        FollowToPoints.Instance.DoMove(self, world, move);
                        Console.WriteLine(String.Format("{0} goes {1}", self.Type, move.Direction));    //[DEBUG]
                    }
                    self.Ext().SaveHitpoints();
                }
                //это чтобы не падало
                LastGuard(move, self.ActionPoints, self.Ext());

            }
            catch (Exception e)
            {
                Console.WriteLine("EXCEPTION: " + e);
            }
            finally
            {
                var end = DateTime.Now;//[DEBUG]
                var spentTime = end - start;//[DEBUG]
                Console.WriteLine("--- " + move.AsString() + " --- [" + spentTime.TotalMilliseconds + "ms]");//[DEBUG]
                if (spentTime.TotalMilliseconds > 1000)//[DEBUG]
                {//[DEBUG]
                    Console.WriteLine("WARNING");//[DEBUG]
                }//[DEBUG]
            }

        }

        private void LastGuard(Model.Move move, int pointsLeft, AI.Model.TrooperExt trooperExt)
        {
            if (trooperExt.Cost(move.Action) > pointsLeft)
            {
                Console.WriteLine("ERROR - Not enough points for " + move.AsString() + ". Cancel");
                move.Wait();
            }
        }


        static Game game;
        internal static int SniperRangeFor(TrooperStance s)
        {
            switch (s)
            {
                case TrooperStance.Standing: return 10;
                case TrooperStance.Kneeling: return 11;
                case TrooperStance.Prone: return 12;
            }
            return 10;
        }
    }

    public static class TrooperExtensions
    {
        public static bool CanTake(this Trooper self, BonusType bonus)
        {
            switch (bonus)
            {
                case BonusType.FieldRation: return !self.IsHoldingFieldRation;
                case BonusType.Grenade: return !self.IsHoldingGrenade;
                case BonusType.Medikit: return !self.IsHoldingMedikit;
            }
            return false;
        }

        public static bool IsVisible(this Trooper self, Trooper another, World world)
        {
            return world.IsVisible(self.VisionRange, self.X, self.Y, self.Stance, another.X, another.Y, another.Stance);
        }

        public static bool CanAttack(this Trooper self, Trooper another)
        {
            return self.GetDistanceTo(another) <= self.ShootingRange;
        }
    }
}