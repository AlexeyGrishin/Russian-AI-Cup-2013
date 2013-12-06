using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{

    //собственно нужен только для юнит-тестов, вся реализация в TrooperExt
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
        public bool CanHeal { get { return IsMedic || (!IsSick && HasMedkit); } }
        public abstract int MedkitHealth { get; }
        public virtual int AllyMedkitHealth { get { return MedkitHealth; } }
        public int GetMedkitHealth(Warrior2 another)
        {
            return another == this ? MedkitHealth : AllyMedkitHealth;
        }
        public abstract int FieldRationExtraPoints { get; }
        public abstract bool IsSick { get; }
        public abstract int MedicHealth { get; }
        public abstract bool IsTeammate { get; }

        public int MaxSteps
        {
            get { return (Actions + (HasFieldRation ? FieldRationExtraPoints : 0)) / Cost(ActionType.Move); }
        }

        public int AbsoluteMaxSteps
        {
            get { return (MaxActions + (HasFieldRation ? FieldRationExtraPoints : 0)) / Cost(ActionType.Move); }
        }

        public bool CanTheoreticallyAttack(Warrior2 anotherWarrior, PossibleMove from = null)
        {
            from = from ?? Location;
            return Tool.GetRadius(AttackRange).Contains(from.X - anotherWarrior.Location.X, from.Y - anotherWarrior.Location.Y);
        }

        public bool CanTheoreticallyAttack(PossibleMove anotherWarrior, PossibleMove from = null)
        {
            from = from ?? Location;
            return Tool.GetRadius(AttackRange).Contains(from.X - anotherWarrior.X, from.Y - anotherWarrior.Y);
        }

        public bool CanTheoreticallySee(PossibleMove anotherWarrior, PossibleMove from = null)
        {
            from = from ?? Location;
            return Tool.GetRadius(VisionRange).Contains(from.X - anotherWarrior.X, from.Y - anotherWarrior.Y);
        }        

        /// DANGERRR!!!
        public bool CanTheoreticallySee(Point anotherWarrior)
        {
            return Tool.GetRadius(VisionRange).Contains(Location.X - anotherWarrior.X, Location.Y - anotherWarrior.Y);
        }

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
                case ActionDraft.StepBack: action = ActionType.Move; break;
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

    //TODO: reduce interfaces
    public interface IVisibilityMaze: IMaze
    {
        bool CanAttack(int xFrom, int yFrom, TrooperStance stance, int xTo, int yTo);

    }

    public interface IWarriorMaze<T> : IMaze, IVisibilityMaze where T : Positioned2
    {
        bool CanAttack(T attacker, int xFrom, int yFrom, T attackWho, int xTo, int yTo);
    }


    public class CellVisibility
    {
        public int VisibleFromHereOnly = 0;
        public int VisibleOutsideOnly = 0;
    }

    //глобальная карта опасностей. создается единожды. кстати я затупил - надо было считать только для 1/4, т.к. карта симметрична
    public class DangerMap
    {
        private int[, ,] dangerMap;

        private CellVisibility[] CountVisibility(IVisibilityMaze maze, int x0, int y0, int radius)
        {
            var res = new CellVisibility[] { new CellVisibility(), new CellVisibility(), new CellVisibility() };
            var r = Tool.GetRadius(radius);
            for (var x = 0; x < maze.Width; x++)
            {
                for (var y = 0; y < maze.Height; y++)
                {
                    if (maze.IsFree(x, y) && r.Contains(Math.Abs(x - x0), Math.Abs(y - y0)))
                    {
                        for (var stance = TrooperStance.Prone; stance <= TrooperStance.Standing; stance++) {
                            var visibleFromHere = maze.CanAttack(x0, y0, stance, x, y);
                            var visibleFromThere = maze.CanAttack(x, y, stance, x0, y0);
                            if (visibleFromHere && !visibleFromThere)
                            {
                                res[(int)stance].VisibleFromHereOnly++;
                            }
                            if (!visibleFromHere && visibleFromThere)
                            {
                                res[(int)stance].VisibleOutsideOnly++;
                            }
                        }
                    }
                }
            }
            return res;
        }

        public DangerMap(IVisibilityMaze maze)
        {
            dangerMap = new int[3, maze.Width, maze.Height];
            for (var x = 0; x < maze.Width; x++)
            {
                for (var y = 0; y < maze.Height; y++)
                {
                    if (maze.IsFree(x, y))
                    {
                        var res = CountVisibility(maze, x, y, 10);
                        for (var s = 0; s < res.Length; s++)
                        {
                            var dangerIndex = res[s].VisibleOutsideOnly > 0 ? res[s].VisibleOutsideOnly : -res[s].VisibleFromHereOnly;
                            dangerMap[s, x, y] = dangerIndex;
                        }
                    }
                }
            }
        }

        public int DangerIndex(int x, int y, TrooperStance stance = TrooperStance.Standing)
        {
            return dangerMap[(int)stance, x, y];
        }

    }

}
