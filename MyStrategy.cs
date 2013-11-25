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
        public static long PlayerId = -1;

        public static int NeedHeeling = 60;
        public static int NeedHeelingOutsideBattle = 95;
        public static double HealToDamageCoeff = 0.5;
        public static double OurEnemyDamageCoeff = 1.2;
        public static int BackDistance = 6;
        public static int CloseDistance = 3;
        public static int TeamCloseDistance = 2;
        public static int MinDistanceToTeamInBattle = 3;
        public static int MaxStepsEnemyWillDo = 2;
        public static double NotStandingDamageCoeff = 0.5;



        public void Move(Trooper self, World world, Game game, Move move)
        {
            Turn = world.MoveIndex;
            PlayerId = self.PlayerId;
            Console.WriteLine("----- Turn: " + Turn + " [" + self.Type + "]"); //[DEBUG]
            self.Update(game);
            world.Troopers.ToList().ForEach(t => t.Update(game));
            var MA = WalkableMap.Instance(world.ToMaze());
            MA.BuildMapFrom(self.GetPosition(), (int)world.Width, p => !world.ToMaze().HasNotWallOrUnit(p.X, p.Y));
            Logger.Logger.Instance().LogMap(world);//[DEBUG]

            var BS = new BattleStrategy();
            bool inBattle = false;
            if (BS.IsInDanger(self, world))
            {
                BS.DoMove(self, world, move);
                inBattle = true;
            }
            else
            {
                Console.WriteLine("There is no danger...");
                BS.CancelSteps(self);
                
            }
            if (!move.IsMade() && self.Ext().Can(ActionType.RaiseStance) && self.Stance != TrooperStance.Standing)
            {
                move.Action = ActionType.RaiseStance;
            }

            if (!move.IsMade() && self.Ext().IsSick && self.IsHoldingMedikit && self.Ext().Can(ActionType.UseMedikit))
            {
                move.Action = ActionType.UseMedikit;
                move.Direction = Direction.CurrentPoint;
            }
            if (!inBattle)
            {
                if (!move.IsMade() && self.Type != TrooperType.FieldMedic && self.Ext().IsABitSick && world.Troopers.Where(t => t.IsTeammate && t.Type == TrooperType.FieldMedic).Any(t => t.GetDistanceTo(self) <= 1))
                {
                    Console.WriteLine("Allow medic to heal us");
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
                if (!move.IsMade() && self.ActionPoints >= game.StandingMoveCost)
                {
                    FollowToPoints.Instance.DoMove(self, world, move);
                    Console.WriteLine(String.Format("{0} goes {1}", self.Type, move.Direction));    //[DEBUG]
                }

            }

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