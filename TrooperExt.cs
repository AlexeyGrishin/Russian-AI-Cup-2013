using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model
{
    public class TrooperExt : Warrior2, Positioned2, Moveable//, Pointed
    {
        internal Trooper orig;
        public Game Game { get; set; }

        public int WayIndex { get; set; }
        public int TurnOrder { get; set; }
        public Move Move { get; set; }
        public bool OnWay { get; set; }

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

        public TrooperExt(Trooper orig)
        {
            this.orig = orig;
            WayIndex = -1;
            WayToWay = null;
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


        public void DoMove(Direction direction, int wayIdx)
        {
            Move.Move(direction);
            WayIndex = wayIdx;
        }

        public void Wait(string reason)
        {
            Console.WriteLine(String.Format("{0} waits: {1}", orig.Type, reason));
            Move.Wait();
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
            return orig.Type + "[" + orig.Hitpoints + "]/" + orig.ActionPoints + " at (" + orig.X + "," + orig.Y + ")";
        }

        public bool Can(ActionType actionType)
        {
            return Can(actionType, 1, orig.ActionPoints);
        }
    }
}
