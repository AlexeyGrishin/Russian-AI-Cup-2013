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

        public bool CanTheoreticallySee(Warrior2 anotherWarrior)
        {
            return CanTheoreticallySee(anotherWarrior.Location.Point);
        }

        public bool CanTheoreticallySee(PossibleMove anotherWarrior)
        {
            return Tool.GetRadius(VisionRange).Contains(Location.X - anotherWarrior.X, Location.Y - anotherWarrior.Y);
        }        

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


    public class BattleCase2<T> where T : Positioned2
    {
        public BattleCase2(T self, T enemy, IWarriorMaze<T> maze, IEnumerable<T> allies = null, IEnumerable<T> enemies = null)
        {
            Self = self;
            Enemy = enemy;
            this.maze = maze;
            Allies = allies == null ? new List<T>() : allies.ToList();
            OtherEnemies = enemies == null ? new List<T>() : enemies.ToList();
            var currentDistance = Math.Abs(self.Location.X - enemy.Location.X) + Math.Abs(self.Location.Y - enemy.Location.Y);
            var myAttackRadius = Tool.GetRadius(self.AttackRange);
            var enemyAttackRadius = Tool.GetRadius(enemy.AttackRange);
            Action<PossibleMove> processCell = (a =>
            {
                a.CanAttackFromHere = maze.CanAttack(self, a.X, a.Y, enemy, enemy.Location.X, enemy.Location.Y) && myAttackRadius.Contains(Math.Abs(enemy.Location.X - a.X), Math.Abs(enemy.Location.Y - a.Y));
                a.CanBeAttacked = maze.CanAttack(enemy.Location.X, enemy.Location.Y, TrooperStance.Standing, a.X, a.Y) && enemyAttackRadius.Contains(Math.Abs(enemy.Location.X - a.X), Math.Abs(enemy.Location.Y - a.Y));
                if (!a.CanBeAttacked && OtherEnemies.Count > 0)
                {
                    a.CanBeAttacked = OtherEnemies.Any(e => maze.CanAttack(e.Location.X, e.Location.Y, TrooperStance.Standing, a.X, a.Y) && Tool.GetRadius(e.AttackRange).Contains(Math.Abs(e.Location.X - a.X), Math.Abs(e.Location.Y - a.Y)));
                }
                a.CanBeAttackedOnKneel = a.CanBeAttacked && AllEnemies.Any(e => maze.CanAttack(e.Location.X, e.Location.Y, TrooperStance.Kneeling, a.X, a.Y));
                a.DistanceToEnemy = enemy.Location.DistanceTo(a);
                a.VisibleToEnemy = AllEnemies.Any(e => maze.CanAttack(e, e.Location.X, e.Location.Y, self, a.X, a.Y) && Tool.GetRadius(e.VisionRange).Contains(Math.Abs(e.Location.X - a.X), Math.Abs(e.Location.Y - a.Y)));
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
            WaysToEnemy = new List<IEnumerable<PossibleMove>> { WalkableMap.Instance().FindWay(self.Location, Enemy.Location, (m) => m.CanAttackFromHere) };
            WaysFromEnemy = new List<IEnumerable<PossibleMove>>(5);
            self.Location.Where(a => (!a.CanBeAttacked || !a.CanBeAttackedOnKneel) && a.FurtherMoves.Count == 0).ToList().ForEach(a =>
            {
                WaysFromEnemy.Add(a.PathToThis());
            });
            StepsToAttack = self.Location.CloserLevelWhere(a => a.CanAttackFromHere);
            StepsToBeAttacked = self.Location.CloserLevelWhere(a => a.CanBeAttacked);
            SickAlly = Allies.Where(a => a.IsSick).OrderBy(a => a.Location.DistanceTo(self.Location)).FirstOrDefault();
            if (SickAlly == null)
                WayToSickAlly = new List<PossibleMove>();
            else
                WayToSickAlly = WalkableMap.Instance().FindWay(self.Location, SickAlly.Location).ToList();
            //if (WayToSickAlly.Count > 1) WayToSickAlly.RemoveAt(WayToSickAlly.Count - 1);
        }

        private IWarriorMaze<T> maze;

        public bool CanAttack(Point from, Point to, TrooperStance position)
        {
            return maze.CanAttack(from.X, from.Y, position, to.X, to.Y);
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
