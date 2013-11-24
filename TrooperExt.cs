using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model
{
    public class TrooperExt : Warrior2, Positioned2//, Pointed
    {
        internal Trooper orig;
        public Game Game { get; set; }

        public int X { get { return orig.X; } }
        public int Y { get { return orig.Y; } }

        public override bool IsSick { get { return Hitpoints <= MyStrategy.NeedHeeling; } }
        public bool IsABitSick { get { return Hitpoints <= MyStrategy.NeedHeelingOutsideBattle; } }
        public override int MedicHealth
        {
            get { return Game.FieldMedicHealBonusHitpoints; }
        }

        public IEnumerable<Direction> WayToWay { get; set; }

        public IList<Move> NextMoves { get; set; }

        public override TrooperStance Position
        {
            get { return orig.Stance; }
        }

        public TrooperExt(Trooper orig)
        {
            this.orig = orig;
            NextMoves = new List<Move>();
        }

        public void AddMove(Move move)
        {
            NextMoves.Add(move);
        }

        public bool HasNextMove()
        {
            return NextMoves.Count > 0;
        }

        public void ExecuteMove(Move gameMove)
        {
            var move = NextMoves[0];
            NextMoves.RemoveAt(0);
            move.Apply(gameMove);
        }


        public void CheckAttackList(IEnumerable<Point> enumerable)
        {
            if (!HasNextMove()) return;
            var attackPoint = NextMoves.Where(m => m.Action == ActionType.Shoot || m.Action == ActionType.ThrowGrenade).Select(m => Point.Get(m.X, m.Y)).FirstOrDefault();
            if (attackPoint == null) return;
            var found = enumerable.Any(e => e.X == attackPoint.X && e.Y == attackPoint.Y);
            if (found) return;
            if (NextMoves.Any(m => m.Action == ActionType.ThrowGrenade))
            {
                found = enumerable.Any(e => Tool.GetDistance(e, attackPoint) == 1);
            }
            if (!found)
            {
                Console.WriteLine("Our enemy disappeared, so cancel orders");
                NextMoves.Clear();
            }
        }


        public override bool HasGrenade
        {
            get { return orig.IsHoldingGrenade; }
        }

        public override int GrenadeRange
        {
            get { return (int) Game.GrenadeThrowRange; }
        }

        public override int GetGrenadeDamage(int delta)
        {
            if (delta > 1) return 0;
            return delta == 0 ? Game.GrenadeDirectDamage : Game.GrenadeCollateralDamage;
        }

        public bool Noticed { get; set; }

        public override int FieldRationExtraPoints
        {
            get { return Game.FieldRationBonusActionPoints - Game.FieldRationBonusActionPoints; }
        }

        public override int Hitpoints
        {
            get { return orig.Hitpoints; }
        }

        public override int Actions
        {
            get { return orig.IsTeammate ? orig.ActionPoints : orig.InitialActionPoints; }
        }

        public override int MaxActions
        {
            get { return orig.InitialActionPoints; }
        }

        public override int AttackRange
        {
            get { return (int)orig.ShootingRange; }
        }



        public int MoveCost { get {
            switch (orig.Stance)
            {
                case TrooperStance.Standing: return Game.StandingMoveCost;
                case TrooperStance.Prone: return Game.ProneMoveCost;
                case TrooperStance.Kneeling: return Game.KneelingMoveCost;
            }
            return 0;
        } }

        public override PossibleMove Location
        {
            //TODO
            get { return WalkableMap.Instance().Get(X, Y); }
        }

        public override int GetDamage(TrooperStance stance)
        {
            switch (stance)
            {
                case TrooperStance.Standing: return orig.StandingDamage;
                case TrooperStance.Kneeling: return orig.KneelingDamage;
                case TrooperStance.Prone: return orig.ProneDamage;
            }
            throw new Exception("Unexpected position");
        }

        public override int GetDamage()
        {
            return orig.Damage;
        }

        public override int Cost(ActionType type)
        {
            switch (type)
            {
                case ActionType.Heal: return Game.FieldMedicHealCost;
                case ActionType.UseMedikit: return Game.MedikitUseCost;
                case ActionType.RaiseStance: return Game.StanceChangeCost;
                case ActionType.LowerStance: return Game.StanceChangeCost;
                case ActionType.Shoot: return orig.ShootCost;
                case ActionType.Move: return Game.StandingMoveCost;
                case ActionType.EndTurn: return 0;
                case ActionType.EatFieldRation: return Game.FieldRationEatCost;
                case ActionType.ThrowGrenade: return Game.GrenadeThrowCost;
            }
            throw new Exception("Unknown action");
        }

        public override bool HasFieldRation
        {
            get { return orig.IsHoldingFieldRation; }
        }

        public override bool HasMedkit
        {
            get { return orig.IsHoldingMedikit; }
        }

        public override bool IsMedic
        {
            get { return orig.Type == TrooperType.FieldMedic; }
        }

        public override int MedkitHealth
        {
            get { return Game.MedikitHealSelfBonusHitpoints; }
        }

        internal void AddMoves(List<CSharpCgdk.Model.Move> list)
        {
            NextMoves = list;
        }

        public override string ToString()
        {
            return (Noticed ? "*" : " ") + orig.Type + "[" + orig.Hitpoints + "]/" + orig.ActionPoints + " at (" + orig.X + "," + orig.Y + ")";
        }

        private int scoreBeforeShoot = -1;
        public void WasShoot(int score)
        {
            scoreBeforeShoot = score;
        }

        public bool WasntReallyShoot(int score)
        {
            return scoreBeforeShoot != -1 && score - scoreBeforeShoot < Game.TrooperDamageScoreFactor;
        }

        public void WasNotShoot()
        {
            scoreBeforeShoot = -1;
        }



        public bool Can(ActionType actionType)
        {
            return Can(actionType, 1, orig.ActionPoints);
        }

        public override TrooperType Type { get { return orig.Type; } }

        public override int VisionRange { get { return (int)orig.VisionRange; } }

    }
}
