using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{

    public abstract class Warrior2 : Positioned2
    {
        public abstract int Actions { get; }
        public abstract int MaxActions { get; }
        public abstract int Hitpoints { get; }
        public abstract int GetDamage(TrooperStance stance);
        public abstract int GetDamage();
        public abstract int GetGrenadeDamage(int distance);
        public abstract TrooperType Type { get; }
        public abstract TrooperStance Position { get; }

        public abstract PossibleMove Location { get; }
        public abstract int AttackRange { get; }
        public abstract int GrenadeRange { get; }
        public abstract int VisionRange { get; }

        public abstract int Cost(ActionType type);

        public abstract bool HasFieldRation { get; }
        public abstract bool HasGrenade { get; }
        public abstract bool HasMedkit { get; }
        public abstract bool IsMedic { get; }
        public abstract int MedkitHealth { get; }
        public abstract int FieldRationExtraPoints { get; }
        public abstract bool IsSick { get; }
        public abstract int MedicHealth { get; }

        public int Cost(ActionDraft draft)
        {
            var action = ActionType.EndTurn;
            var count = 1;
            switch (draft)
            {
                case ActionDraft.EatFieldRation: action = ActionType.EatFieldRation; break;
                case ActionDraft.HealAlly: action = ActionType.Heal; break;
                case ActionDraft.HealSelf: action = ActionType.UseMedikit; break;
                case ActionDraft.LieDown: 
                    action = ActionType.LowerStance;
                    count = Math.Abs(Position - TrooperStance.Prone);
                    break;
                case ActionDraft.StandUp: 
                    action = ActionType.LowerStance; 
                    count = Math.Abs(Position - TrooperStance.Standing);
                    break;
                case ActionDraft.OnKneel: 
                    action = ActionType.LowerStance;
                    count = Math.Abs(Position - TrooperStance.Kneeling);

                    break;
                case ActionDraft.Shoot: action = ActionType.Shoot; break;
                case ActionDraft.StepFromEnemy: action = ActionType.Move; break;
                case ActionDraft.StepToEnemy: action = ActionType.Move; break;
                case ActionDraft.StepToSickAlly: action = ActionType.Move; break;
                case ActionDraft.ThrowGrenade: action = ActionType.ThrowGrenade; break;
            }
            return Cost(action, count);
        }

        public int Cost(ActionType type, int count)
        {
            return Cost(type) * count;
        }
        public bool Can(ActionType type, int count, int points)
        {
            return Cost(type, count) <= points;
        }
        public bool DoIfCan(ActionType type, ref int points)
        {
            return DoIfCan(type, 1, ref points);
        }

        public bool DoIfCan(ActionType type, int count, ref int points)
        {
            var cost = Cost(type, count);
            if (cost <= points)
            {
                points -= cost;
                return true;
            }
            return false;
        }
    }

    public interface IWarriorMaze<T> : IMaze where T : Positioned2
    {
        bool CanAttack(T attacker, int xFrom, int yFrom, T attackWho, int xTo, int yTo);
    }

    public class BattleCase2<T> where T : Positioned2
    {
        public BattleCase2(T self, T enemy, IWarriorMaze<T> maze, IEnumerable<T> allies = null, IEnumerable<T> enemies = null)
        {
            Self = self;
            Enemy = enemy;
            Allies = allies == null ? new List<T>() : allies.ToList();
            OtherEnemies = enemies == null ? new List<T>() : enemies.ToList();
            var currentDistance = Math.Abs(self.Location.X - enemy.Location.X) + Math.Abs(self.Location.Y - enemy.Location.Y);
            var myAttackRadius = Tool.GetRadius(self.AttackRange);
            var enemyAttackRadius = Tool.GetRadius(enemy.AttackRange);
            Action<PossibleMove> processCell = (a =>
            {
                a.CanAttackFromHere = maze.CanAttack(self, a.X, a.Y, enemy, enemy.Location.X, enemy.Location.Y) && myAttackRadius.Contains(Math.Abs(enemy.Location.X - a.X), Math.Abs(enemy.Location.Y - a.Y));
                a.CanBeAttacked = maze.CanAttack(enemy, enemy.Location.X, enemy.Location.Y, self, a.X, a.Y) && enemyAttackRadius.Contains(Math.Abs(enemy.Location.X - a.X), Math.Abs(enemy.Location.Y - a.Y));
                if (!a.CanBeAttacked && OtherEnemies.Count > 0)
                {
                    a.CanBeAttacked = OtherEnemies.Any(e => maze.CanAttack(e, e.Location.X, e.Location.Y, self, a.X, a.Y) && Tool.GetRadius(e.AttackRange).Contains(Math.Abs(e.Location.X - a.X), Math.Abs(e.Location.Y - a.Y)));
                }
                a.DistanceToEnemy = enemy.Location.DistanceTo(a);
                a.CloserToEnemy = a.DistanceToEnemy < currentDistance;
                a.DistanceToTeam = Allies.Count() > 0 ? Allies.Select(al => al.Location.DistanceTo(a)).Sum() : 999;
            });
            self.Location.ForEach(processCell);
            Allies.Select(a => a.Location).ToList().ForEach(processCell);
            /*
            WaysToEnemy = new List<IEnumerable<PossibleMove>>(5);
            self.Location.WhereLeafs(a => a.CloserToEnemy).ToList().ForEach(a =>
            {
                WaysToEnemy.Add(a.PathToThis());
            });*/
            WaysToEnemy = new List<IEnumerable<PossibleMove>> { WalkableMap.Instance().FindWay(self.Location, Enemy.Location) };
            WaysFromEnemy = new List<IEnumerable<PossibleMove>>(5);
            self.Location.Where(a => !a.CanBeAttacked && a.FurtherMoves.Count == 0).ToList().ForEach(a =>
            {
                WaysFromEnemy.Add(a.PathToThis());
            });
            StepsToAttack = self.Location.CloserLevelWhere(a => a.CanAttackFromHere);
            StepsToBeAttacked = self.Location.CloserLevelWhere(a => a.CanBeAttacked);
            SickAlly = Allies.Where(a => a.IsSick).FirstOrDefault();
            if (SickAlly == null)
                WayToSickAlly = new List<PossibleMove>();
            else
                WayToSickAlly = WalkableMap.Instance().FindWay(self.Location, SickAlly.Location).ToList();
            //if (WayToSickAlly.Count > 1) WayToSickAlly.RemoveAt(WayToSickAlly.Count - 1);
        }

        public IList<T> AllEnemies { get { return OtherEnemies.Concat(new List<T> { Enemy }).ToList(); } }
        public IList<T> AllOurTroops { get { return Allies.Concat(new List<T> { Self }).ToList(); } }
        public IList<T> All { get { return AllEnemies.Concat(AllOurTroops).ToList();  } }

        public T Self { get; private set; }
        public T Enemy { get; private set; }
        public IList<T> Allies { get; private set; }
        public IList<T> OtherEnemies { get; private set; }

        public int StepsToAttack { get; private set; }
        public int StepsToBeAttacked { get; private set; }

        public IList<IEnumerable<PossibleMove>> WaysToEnemy { get; private set; }
        public IList<IEnumerable<PossibleMove>> WaysFromEnemy { get; private set; }
        public IList<PossibleMove> WayToSickAlly { get; private set; }

        public T SickAlly { get; private set; }
    }
}
