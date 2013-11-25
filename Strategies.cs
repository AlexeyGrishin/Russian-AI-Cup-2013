using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{


    public enum ActionDraft
    {
        StepToEnemy,
        StepFromEnemy,
        StepToSickAlly,
        Shoot,
        HealSelf,   //medkit
        HealAlly,
        ThrowGrenade,
        StandUp,
        OnKneel,
        LieDown,
        Skip,
        EatFieldRation
    }

    public interface Positioned2
    {
        PossibleMove Location { get; }
        int AttackRange { get;  }
        bool IsSick { get; }
        
    }

    public abstract class Warrior
    {
        public abstract int Hitpoints { get; }
        public abstract int Actions { get; }
        public abstract TrooperType Type { get; }
        public abstract int Damage { get; }
        public abstract int GetDamage(ActionDraft position);
        public abstract int VisibilityRange { get; }
        public abstract int AttackRange { get; }
        public abstract bool HasGrenade { get; }
        public abstract int GrenadeRange { get; }

        public abstract int GetGrenadeDamage(int delta);


        public abstract int Cost(ActionDraft draft);
        //public abstract int Cost(ActionDraft draft, ActionDraft position);

        public bool Can(ActionDraft draft, int points)
        {
            return points >= Cost(draft);
        }

        public bool DoIfCan(ActionDraft draft, ref int points)
        {
            var cost = Cost(draft);
            if (points >= cost)
            {
                points -= cost;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Can(ActionDraft actionDraft)
        {
            return Can(actionDraft, Actions);
        }

        public abstract PossibleMove Location { get; }

    }

    public class StrategyChange: IComparable
    {
        public int EnemyDamage = 0;
        public int OurDamage = 0;
        public int EnemyLost = 0;
        public int OurLost = 0;
        public int OurHeal = 0;

        public int PotentiallyThrowGrenade = 0;
        public int PotentiallyHelpToAttack = 0;
        public int PotentiallyIncreasePower = 0;
        public int PotentiallyHeal = 0;

        public int PotentiallyStuck = 0;

        public int LostMobility = 0;

        public String Name;

        public StrategyChange(String name)
        {
            this.Name = name;
        }

        private static int Better(int res) { return res; }
        private static int Worse(int res) { return -res; }

        public int EnemyDamageNHeal
        {
            get
            {
                return EnemyDamage + (int)(OurHeal * MyStrategy.HealToDamageCoeff);
            }
        }

        public int UnitedDamage
        {
            get
            {
                return EnemyDamageNHeal - (int)(OurDamage * MyStrategy.OurEnemyDamageCoeff);
            }
        }

        public int CompareTo(StrategyChange another)
        {
            if (another.OurLost != OurLost) return Worse(OurLost - another.OurLost);
            if (another.EnemyLost != EnemyLost) return Better(EnemyLost - another.EnemyLost);
            if (another.UnitedDamage != UnitedDamage) return Better(UnitedDamage - another.UnitedDamage);

            if (another.LostMobility != LostMobility) return Worse(LostMobility - another.LostMobility);
            if (another.PotentiallyHeal != PotentiallyHeal) return Better(PotentiallyHeal - another.PotentiallyHeal);
            if (another.PotentiallyIncreasePower != PotentiallyIncreasePower) return Better(PotentiallyIncreasePower - another.PotentiallyIncreasePower);
            if (another.PotentiallyHelpToAttack != PotentiallyHelpToAttack) return Better(PotentiallyHelpToAttack - another.PotentiallyHelpToAttack);
            if (another.PotentiallyStuck != PotentiallyStuck) return Worse(PotentiallyStuck - another.PotentiallyStuck);
            if (another.PotentiallyThrowGrenade != PotentiallyThrowGrenade) return Better(PotentiallyThrowGrenade - another.PotentiallyThrowGrenade);
            
            return 0;
        }
    
        public int CompareTo(object obj)
        {
 	        return CompareTo(obj as StrategyChange);
        }

        private static string F(int flag, string c)
        {
            if (flag > 0) return c + "+";
            if (flag < 0) return c + "-";
            return c + " ";
        }

        public override string ToString()
        {
            return String.Format("(o/e) lost = {0}/{1} damage = {2}/{3}({9}) = {11}, heal = {8}, flags = {4} {5} {6} {7} {10} {12}",
                OurLost, EnemyLost, OurDamage, 
                EnemyDamage, F(PotentiallyIncreasePower, "pow"), F(PotentiallyHelpToAttack, "atc"), 
                F(PotentiallyThrowGrenade, "gre"), F(PotentiallyHeal, "hea"),  OurHeal,
                EnemyDamageNHeal, F(PotentiallyStuck, "stk"), UnitedDamage,
                F(-LostMobility, "mob")
                );
        }

    }

    public class Battle
    {

        public static IList<StrategyResult> All<T>(BattleCase2<T> battleCase) where T : Warrior2
        {
            var allCases = StrategiesFor<T>(battleCase).Select(s => Emulator2.Emulate(battleCase, s)).ToList();
            allCases.Sort();
            allCases.Reverse();
            Console.WriteLine(String.Join("\n\n", allCases.Select(a => a.ToString())));
            return allCases;
        }

        private static List<IStrategy> StrategiesFor<T>(BattleCase2<T> battleCase) where T : Warrior2
        {
            var list = new List<IStrategy>();
            if (battleCase.StepsToAttack == 0)
            {
                Add(list, "just shoot",                 ActionDraft.Shoot);
                Add(list, "shoot and go away",          ActionDraft.Shoot, ActionDraft.StepFromEnemy);
                Add(list, "shoot and go away: 2 steps", ActionDraft.Shoot, ActionDraft.StepFromEnemy, ActionDraft.StepFromEnemy);
                Add(list, "shoot and go away: 3 steps", ActionDraft.Shoot, ActionDraft.StepFromEnemy, ActionDraft.StepFromEnemy, ActionDraft.StepFromEnemy);
                Add(list, "kneel and shoot",                ActionDraft.OnKneel, ActionDraft.Shoot);
                Add(list, "kneel and shoot and stand back", ActionDraft.OnKneel, ActionDraft.Shoot, ActionDraft.StandUp);
                Add(list, "line down and shoot", ActionDraft.LieDown, ActionDraft.Shoot);
            }
            Add(list, "come and shoot",             ActionDraft.StepToEnemy, 1, Math.Min(Math.Max(1, battleCase.StepsToAttack), 5), ActionDraft.Shoot);
            if (battleCase.Self.Type == TrooperType.Sniper)
            {
                Add(list, "come and kneel", ActionDraft.StepToEnemy, 1, Math.Min(Math.Max(1, battleCase.StepsToAttack), 5), ActionDraft.OnKneel);   //for sniper
                Add(list, "come and lie down", ActionDraft.StepToEnemy, 1, Math.Min(Math.Max(1, battleCase.StepsToAttack), 5), ActionDraft.LieDown);   //for sniper
            }
            Add(list, "step away and shoot", ActionDraft.StepFromEnemy, 1, 1, ActionDraft.Shoot);
            if (battleCase.Self.HasGrenade)
            {
                Add(list, "come and throw grenade", ActionDraft.StepToEnemy, 0, 3, ActionDraft.ThrowGrenade);
            }
            Add(list, "go to enemy", ActionDraft.StepToEnemy, 1, 5);
            Add(list, "go away from enemy", ActionDraft.StepFromEnemy, 1, 5);
            Add(list, "stand still", ActionDraft.Skip);
            if (battleCase.Self.IsMedic && battleCase.SickAlly != null)
            {
                Add(list, "heal sick", ActionDraft.HealAlly);
                Add(list, "heal sick", ActionDraft.StepToSickAlly, 0, 5, ActionDraft.HealAlly);
                Add(list, "go to sick", ActionDraft.StepToSickAlly, 0, 5);
            }
            return list;
        }

        private static void Add(List<IStrategy> list, string name, params ActionDraft[] actions)
        {
            list.Add(Strategy.Create(name, actions));
        }

        private static void Add(List<IStrategy> list, string name, ActionDraft move, int from, int to, params ActionDraft[] actions)
        {
            for (var steps = from; steps <= to; steps++)
            {
                list.Add(Strategy.Create(name, move, steps, actions));
            }
        }

    }

    public class StrategyResult : IComparable
    {
        public StrategyChange Change { get; private set; }
        public List<Move> Moves {get; private set;}
        public string Name { get { return Change.Name; } }
        public string ReasonOfImpossibility { get; set; }
        public StrategyResult(string name)
        {
            Change = new StrategyChange(name);
            Moves = new List<Move>();
        }
        public override string ToString()
        {
            return String.Format("{0}: {1} --> {2}", Name, ReasonOfImpossibility ?? String.Join(",", Moves.Select(m => "[" + m.AsString() + "]")), ReasonOfImpossibility == null ? Change.ToString() : "skip");
        }

        public StrategyResult Impossible(string p)
        {
            ReasonOfImpossibility = p;
            return this;
        }

        public bool SetImpossible(string p)
        {
            ReasonOfImpossibility = p;
            return false;
        }


        public bool Possible { get { return ReasonOfImpossibility == null; } }

        public int CompareTo(object obj)
        {
            var sr = obj as StrategyResult;
            if (Possible != sr.Possible) return Possible.CompareTo(sr.Possible);
            var res = Change.CompareTo(sr.Change);
            if (res == 0)
            {
                if (sr.OnlyMoves && OnlyMoves)
                    return Moves.Count - sr.Moves.Count;
                else if (!sr.OnlyMoves && !OnlyMoves)
                    return sr.Moves.Count - Moves.Count;
                else return OnlyMoves ? 1 : -1;
            }
            return res;
        }

        private bool OnlyMoves
        {
            get
            {
                return Moves.All(m => m.Action == ActionType.Move);
            }
        }

    }

    public class Emulator2
    {
        class Comparer : IEqualityComparer<IEnumerable<PossibleMove>>
        {

            public bool Equals(IEnumerable<PossibleMove> x, IEnumerable<PossibleMove> y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(IEnumerable<PossibleMove> obj)
            {
                return obj.Select(p => p.X + p.Y).Sum();
            }
        }

        public static StrategyResult Emulate<T>(BattleCase2<T> battleCase, IStrategy strategy) where T: Warrior2
        {
            var movingActions = strategy.Actions.Where(a => a == ActionDraft.StepFromEnemy || a == ActionDraft.StepToEnemy || a == ActionDraft.StepToSickAlly);
            var movingActionsCount = movingActions.Count();
            List<IEnumerable<PossibleMove>> ways = new List<IEnumerable<PossibleMove>>();
            if (movingActionsCount > 0)
            {
                var direction = movingActions.First();
                switch (direction)
                {
                    case ActionDraft.StepToEnemy:
                        ways = battleCase.WaysToEnemy.Where(w => w.Count() > movingActionsCount).OrderBy(w => w.Last().DistanceToTeam)
                            .Select(w => w.Take(movingActionsCount + 1)).Distinct(new Comparer()).ToList();
                        break;
                    case ActionDraft.StepFromEnemy:
                        ways = battleCase.WaysFromEnemy.Where(w => w.Count() > movingActionsCount).Where(w => !w.ElementAt(movingActionsCount).CanBeAttacked)
                            .Select(w => w.Take(movingActionsCount + 1)).Distinct(new Comparer()).OrderBy(w => w.Last().DistanceToTeam).ToList();
                        break;
                    case ActionDraft.StepToSickAlly:
                        if (battleCase.SickAlly == null)
                        {
                            return new StrategyResult(strategy.Name).Impossible("There is no sick units");
                        }
                        if (battleCase.WayToSickAlly.Count() > movingActionsCount)
                            ways.Add(battleCase.WayToSickAlly);
                        break;
                }
                if (ways.Count == 0)
                {
                    return new StrategyResult(strategy.Name).Impossible(String.Format("Action {1} requires {0} steps, but there is no acceptable way", movingActionsCount, direction));
                }
            }
            if (ways.Count == 0)
            {
                return Emulate<T>(null, battleCase, strategy);
            }
            else
            {
                //Console.WriteLine(String.Join(" \n ", ways.Select(w => String.Join(" -> ", w.Select(wp => wp.ToString())))));
                var results = ways.Select(w => Emulate(w.ToList(), battleCase, strategy)).ToList();
                results.Sort();
                results.Reverse();
                var best = results.FirstOrDefault(r => r.Possible);
                return best ?? results.First();
            }
        }
        
        private static StrategyResult Emulate<T>(List<PossibleMove> way, BattleCase2<T> battleCase, IStrategy strategy) where T: Warrior2
        {
            var sr = new StrategyResult(strategy.Name);
            var distance = 0;
            int points = battleCase.Self.Actions;
            var currentPosition = battleCase.Self.Position;
            int overallDistance = 0;
            var actions = strategy.Actions;
            var currentLocation = battleCase.Self.Location;
            var hasRation = battleCase.Self.HasFieldRation;
            if (battleCase.Self.IsSick && battleCase.Self.HasMedkit && battleCase.Self.DoIfCan(ActionType.UseMedikit, ref points))
            {
                sr.Moves.Add(new Move { Action = ActionType.UseMedikit, X = battleCase.Self.Location.X, Y = battleCase.Self.Location.Y });
                sr.Change.OurDamage -= battleCase.Self.MedkitHealth;
            }
            sr.Change.PotentiallyStuck = currentLocation.FreeSpace ? 0 : 2;
            foreach (var action in actions)
            {
                ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                if (action == ActionDraft.StepToEnemy || action == ActionDraft.StepFromEnemy || action == ActionDraft.StepToSickAlly)
                {
                    distance += 1;
                    continue;
                }
                else if (distance != 0)
                {
                    if (!Move(sr, way, distance, battleCase, ref points, ref currentLocation, ref currentPosition, ref hasRation)) return sr;
                    overallDistance += distance;
                    distance = 0;
                }
                if (action == ActionDraft.Shoot)
                {
                    var pointsAfterRepeatedAction = (actions.IndexOf(action) == actions.Count - 1) ? 0 : actions.GetRange(actions.IndexOf(action) + 1, actions.Count - actions.IndexOf(action) - 1).Select(n => battleCase.Self.Cost(n)).Sum();
                    points -= pointsAfterRepeatedAction;
                    if (points <= 0) return sr.Impossible(String.Format("Not enogh points to shoot at least once"));
                    while (Shoot(sr, battleCase.Self, battleCase.Enemy, ref points, currentPosition, currentLocation)) {
                        ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                    };
                    points += pointsAfterRepeatedAction;                    
                }
                else if (action == ActionDraft.StandUp || action == ActionDraft.OnKneel || action == ActionDraft.LieDown)
                {
                    var targetPosition = (TrooperStance)((int)TrooperStance.Standing - (action - ActionDraft.StandUp));
                    var down = targetPosition < currentPosition; var count = Math.Abs(targetPosition - currentPosition);
                    if (targetPosition == currentPosition) continue;
                    var actionType = down ? ActionType.LowerStance : ActionType.RaiseStance;
                    if (!battleCase.Self.DoIfCan(actionType, count, ref points)) return sr.Impossible(String.Format("Cannot {0} {1} times - not enough points ({2})", actionType, count, points));
                    for (var i = 0; i < count; i++) sr.Moves.Add(new Move { Action = actionType });
                    currentPosition = targetPosition;
                }
                else if (action == ActionDraft.Skip)
                {
                    sr.Moves.Add(new Move().Wait());
                }
                else if (action == ActionDraft.HealAlly)
                {
                    if (!battleCase.Self.IsMedic) return sr.Impossible("Unit is not a medic!");
                    if (!Heal(sr, battleCase, ref points, currentLocation, ref hasRation)) return sr;
                }
                else if (action == ActionDraft.HealSelf)
                {
                    if (!battleCase.Self.HasMedkit) return sr.Impossible("Unit does not have medkit!");
                    if (!battleCase.Self.DoIfCan(ActionType.UseMedikit, ref points)) return sr.Impossible(String.Format("Cannot use medkit - not enough points({0})", points));
                    sr.Moves.Add(new Move { Action = ActionType.UseMedikit});
                    sr.Change.OurDamage -= battleCase.Self.MedkitHealth;
                }
                else if (action == ActionDraft.ThrowGrenade)
                {
                    if (!ThrowGrenate(sr, battleCase, ref points, currentLocation)) return sr;
                }
                else if (action == ActionDraft.EatFieldRation)
                {
                    if (!battleCase.Self.HasFieldRation) return sr.Impossible("Unit does not have ration!");
                    if (!battleCase.Self.DoIfCan(ActionType.EatFieldRation, ref points)) return sr.Impossible(String.Format("Cannot eat field ration - not enough points({0})", points));
                    sr.Moves.Add(new Move { Action = ActionType.EatFieldRation });
                }
            }
            if (distance > 0)
            {
                if (!Move(sr, way, distance, battleCase, ref points, ref currentLocation, ref currentPosition, ref hasRation)) return sr;
            }
            if (sr.Moves.Any(m => m.Action == ActionType.EatFieldRation))
            {
                if (sr.Moves.Last().Action == ActionType.EatFieldRation)
                {
                    sr.Moves.RemoveAt(sr.Moves.Count - 1);
                }
                else if (points >= battleCase.Self.FieldRationExtraPoints)
                {
                    Console.WriteLine("Cancel eating ration - it was not used"); //[DEBUG]
                    sr.Moves.RemoveAll(s => s.Action == ActionType.EatFieldRation);
                }
            }
            if (battleCase.Self.Type != TrooperType.Sniper)
                sr.Change.LostMobility += (TrooperStance.Standing - currentPosition);
            else if (currentLocation.CanAttackFromHere)
                sr.Change.PotentiallyHelpToAttack += (TrooperStance.Standing - currentPosition);
            CalculateEnemyResponse(sr, battleCase, currentLocation, currentPosition);
            return sr;
        }

        private static void ExtraRation<T>(StrategyResult sr, BattleCase2<T> battleCase, ref int points, ref bool hasRation) where T: Warrior2
        {
            if (hasRation) 
                if (points < battleCase.Self.MaxActions && battleCase.Self.Can(ActionType.EatFieldRation, 1, points))
                {
                    sr.Moves.Add(new Move { Action = ActionType.EatFieldRation, X = battleCase.Self.Location.X, Y = battleCase.Self.Location.Y  });
                    points += battleCase.Self.FieldRationExtraPoints;
                    hasRation = false;
                }

        }

        class PositionedWarrior
        {
            public Warrior2 Warrior { get; set; }
            public PossibleMove Location { get; set; }
            public bool Alive { get { return Hitpoints > 0; } }
            public TrooperStance Position { get; set; }
            public int Hitpoints { get; set; }
            public int Temp { get; set; }
            public bool TempAlive { get { return Hitpoints - Temp > 0; } }
        }

        private static bool Heal<T>(StrategyResult sr, BattleCase2<T> battleCase, ref int points, PossibleMove currentLocation, ref bool hasRation) where T : Warrior2
        {
            if (currentLocation.DistanceTo(battleCase.SickAlly.Location) > 1) return sr.SetImpossible("Cannot heal - distance > 1");
            if (!battleCase.Self.Can(ActionType.Heal, 1, points)) return sr.SetImpossible("Not enough points to just heal once");
            while (battleCase.Self.DoIfCan(ActionType.Heal, ref points))
            {
                sr.Moves.Add(new Move { Action = ActionType.Heal, X = battleCase.SickAlly.Location.X, Y = battleCase.SickAlly.Location.Y });
                sr.Change.OurDamage -= battleCase.Self.MedicHealth;
                sr.Change.OurHeal += battleCase.Self.MedicHealth;
                ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
            }

            return true;
        }

        private static void CalculateEnemyResponse<T>(StrategyResult sr, BattleCase2<T> battleCase, PossibleMove currentLocation, TrooperStance currentPosition) where T : Warrior2
        {
            var allEnemies = battleCase.OtherEnemies.ToList();
            if (sr.Change.EnemyLost == 0) allEnemies = allEnemies.Concat(new List<T> {battleCase.Enemy}).ToList();
            var alliesAndSelf = battleCase.Allies.Select(s => new PositionedWarrior { Location = s.Location, Warrior = s, Hitpoints = s.Hitpoints, Position = s.Position })
                .Concat(new List<PositionedWarrior> { new PositionedWarrior { Warrior = battleCase.Self, Location = currentLocation, Hitpoints = battleCase.Self.Hitpoints - sr.Change.OurDamage, Position = currentPosition } }).ToList();
            foreach (var enemy in allEnemies)
            {
                if (!alliesAndSelf.Any(a => a.Alive)) break;
                alliesAndSelf.ForEach(a => a.Temp = 0);
                var enemyGrenadeRadius = Tool.GetRadius(enemy.GrenadeRange);
                var attackable = alliesAndSelf.Where(a => a.Alive).Where(a => enemyGrenadeRadius.Contains(Math.Abs(a.Location.X - enemy.Location.X), Math.Abs(a.Location.Y - enemy.Location.Y)));
                var grenadeDamage = 0;
                var origOurDamage = sr.Change.OurDamage;
                if (enemy.HasGrenade && attackable.Count() > 0 && sr.Change.OurLost == 0)
                {
                    foreach (var attacked in attackable)
                    {
                        var damage = enemy.GetGrenadeDamage(0);
                        attacked.Temp += damage;
                        grenadeDamage += damage;
                        sr.Change.OurDamage += damage;
                    }
                }
                if (alliesAndSelf.Any(a => !a.TempAlive))
                {
                    alliesAndSelf.ForEach(a =>
                    {
                        a.Hitpoints -= a.Temp;
                    });
                }
                else
                {
                    sr.Change.OurDamage -= grenadeDamage;

                    var toAttack = alliesAndSelf.Where(a => a.Alive).OrderBy(a => WalkableMap.Instance().FindDistance(a.Location, MyStrategy.MaxStepsEnemyWillDo, m => m.CanBeAttacked) > -1).OrderBy(a => a.Warrior.Hitpoints).FirstOrDefault();
                    if (toAttack != null)
                    {
                        var stepsToAttack = WalkableMap.Instance().FindDistance(toAttack.Location, MyStrategy.MaxStepsEnemyWillDo, m => m.CanBeAttacked);
                        if (toAttack.Warrior.VisionRange > enemy.VisionRange)
                            stepsToAttack++;

                        var enemyPoints = enemy.Actions - enemy.Cost(ActionType.Move) * stepsToAttack;
                        if (enemy.HasFieldRation) enemyPoints += enemy.FieldRationExtraPoints;
                        if (enemy.Type == TrooperType.Sniper && stepsToAttack > 0) enemyPoints = 0;
                        while (enemy.DoIfCan(ActionType.Shoot, ref enemyPoints))
                        {
                            var dmg = (int)(enemy.GetDamage() * (1 + (TrooperStance.Standing - toAttack.Position + 1) * MyStrategy.NotStandingDamageCoeff));
                            sr.Change.OurDamage += dmg;
                            toAttack.Hitpoints -= dmg;
                        }
                    }
                }
                if (alliesAndSelf.All(a => a.Alive) && sr.Change.OurDamage < origOurDamage + grenadeDamage)
                {
                    sr.Change.OurDamage = origOurDamage + grenadeDamage;
                }
            }
            sr.Change.OurLost += alliesAndSelf.Count(a => !a.Alive);
        }

        private static bool CheckStanceBeforeMoving<T>(StrategyResult sr, BattleCase2<T> battleCase, ref int points, PossibleMove currentLocation, ref TrooperStance currentPosition) where T: Warrior2
        {
            if (currentPosition == TrooperStance.Standing) return true;
            var count = (int)(TrooperStance.Standing - currentPosition);
            if (!battleCase.Self.DoIfCan(ActionType.RaiseStance, count, ref points)) return sr.SetImpossible("Trying to stand up to move, but cannot - not enough points");
            for (var i = 0; i < count; i++) sr.Moves.Add(new Move { Action = ActionType.RaiseStance });
            currentPosition = TrooperStance.Standing;
            return true;
        }

        private static bool ThrowGrenate<T>(StrategyResult sr, BattleCase2<T> battleCase, ref int points, PossibleMove currentLocation) where T : Warrior2
        {
            if (!battleCase.Self.HasGrenade) return sr.SetImpossible("Unit does not have grenade!");
            if (!battleCase.Self.Can(ActionType.ThrowGrenade, 1, points)) return sr.SetImpossible("Not enought points to throw grenade");
            Point target = battleCase.Enemy.Location.Point;

            //1. check distance. if not exact - check near points
            var grenadeRadis = Tool.GetRadius(battleCase.Self.GrenadeRange);
            var dxes = new int[] { 0, 1, 0, -1, 0 };
            var dyes = new int[] { 0, 0, 1, 0, -1 };
            int distance = -1;
            for (var i = 0; i < dxes.Length; i++)
            {
                target.X = battleCase.Enemy.Location.X + dxes[i];
                target.Y = battleCase.Enemy.Location.Y + dyes[i];
                int dx = Math.Abs(target.X - currentLocation.X), dy = Math.Abs(target.Y - currentLocation.Y);
                if (grenadeRadis.Contains(dx, dy))
                {
                    distance = i == 0 ? 0 : 1;
                    break;
                }
            }
            if (distance == -1) return sr.SetImpossible(String.Format("It is not possible to throw grenade from {0},{1} to {2},{3}", currentLocation.X, currentLocation.Y, battleCase.Enemy.Location.X, battleCase.Enemy.Location.Y));
            int damage = battleCase.Self.GetGrenadeDamage(distance);
            sr.Change.EnemyDamage += damage;
            if (sr.Change.EnemyDamage >= battleCase.Enemy.Hitpoints)
            {
                sr.Change.EnemyLost++;
            }
            if (Math.Abs(battleCase.Self.Location.X - target.X) + Math.Abs(battleCase.Self.Location.Y - target.Y) <= 1) return sr.SetImpossible("I do not want to throu grenade to myself...");
            battleCase.Allies.Where(a => Math.Abs(a.Location.X - target.X) + Math.Abs(a.Location.Y - target.Y) <= 1).ToList().ForEach(a =>
            {
                var adamage = battleCase.Self.GetGrenadeDamage(Math.Abs(a.Location.X - target.X) + Math.Abs(a.Location.Y - target.Y));
                sr.Change.OurDamage += adamage;
                if (adamage >= a.Hitpoints) sr.Change.OurLost++;
            });
            
            battleCase.Self.DoIfCan(ActionType.ThrowGrenade, ref points);
            sr.Moves.Add(new Move { Action = ActionType.ThrowGrenade, X = target.X, Y = target.Y });
            return true;
        }

        private static bool Shoot<T>(StrategyResult sr, T self, T enemy, ref int points, TrooperStance currentPosition, PossibleMove location) where T : Warrior2
        {
            if (!location.CanAttackFromHere) return sr.SetImpossible(String.Format("We cannot attack from {0},{1} enemy at {2},{3}", location.X, location.Y, enemy.Location.X, enemy.Location.Y));
            if (self.DoIfCan(ActionType.Shoot, ref points))
            {
                sr.Moves.Add(new Move().Shoot(enemy.Location.Point));
                var damage = self.GetDamage(currentPosition);
                sr.Change.EnemyDamage += damage;
                if (sr.Change.EnemyDamage >= enemy.Hitpoints)
                {
                    sr.Change.EnemyLost += 1;
                    return false;
                }
                return true;
            }
            return false;
        }

        private static bool Move<T>(StrategyResult sr, List<PossibleMove> way, int distance, BattleCase2<T> battleCase, ref int points, ref PossibleMove location, ref TrooperStance position, ref bool hasRation) where T : Warrior2
        {
            if (!CheckStanceBeforeMoving(sr, battleCase, ref points, location, ref position)) return false;
            var oldLoc = location;
            var point = oldLoc;
            for (var wayIdx = 1; wayIdx <= distance; wayIdx++)
            {
                ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                if (!battleCase.Self.DoIfCan(ActionType.Move, 1, ref points)) return sr.SetImpossible(String.Format("Not enought action points ({0}) to perform {1} steps", points, distance)); 
                sr.Moves.Add(new Move().Move(point.DirectionTo(way[wayIdx])));
                point = way[wayIdx];
            }
            if (point.CloserToEnemy)
            {
                if (battleCase.Self.HasGrenade) sr.Change.PotentiallyThrowGrenade++;
                sr.Change.PotentiallyHelpToAttack++;
            }
            if (point.DistanceToTeam != oldLoc.DistanceToTeam)
            {
                var change = Math.Max(0, Math.Abs(point.DistanceToTeam - oldLoc.DistanceToTeam) - MyStrategy.MinDistanceToTeamInBattle);
                if (change > 0)
                    sr.Change.PotentiallyIncreasePower += (point.DistanceToTeam > oldLoc.DistanceToTeam ? -change : change);
                if (sr.Change.PotentiallyIncreasePower < 0)
                {
                    //return sr.SetImpossible("Too far from the team");
                }
            }
            if (point.CanAttackFromHere && !oldLoc.CanAttackFromHere)
            {
                sr.Change.PotentiallyHelpToAttack++;
            }
            if (!point.CanAttackFromHere && oldLoc.CanAttackFromHere)
            {
                sr.Change.PotentiallyHelpToAttack--;
            }
            if (battleCase.Self.IsMedic && battleCase.SickAlly != null && battleCase.SickAlly.Location.DistanceTo(oldLoc) > battleCase.SickAlly.Location.DistanceTo(point))
            {
                sr.Change.PotentiallyHeal++;
            }
            sr.Change.PotentiallyStuck = point.FreeSpace ? -1 : 1;
            location = point;
            return true;

        }
    }

    public abstract class IStrategy
    {
        public abstract string Name { get; }
        public abstract List<ActionDraft> Actions { get; }
    }

    public class Strategy: IStrategy
    {
        private string name;
        private List<ActionDraft> actions;

        public Strategy(string name, params ActionDraft[] actions)
        {
            this.name = name;
            this.actions = actions.ToList();
        }

        public override string Name
        {
            get { return name; }
        }

        public override List<ActionDraft> Actions
        {
            get { return actions; }
        }

        public static Strategy Create(string name, params ActionDraft[] actions)
        {
            return new Strategy(name, actions);
        }

        public static Strategy Create(string name, ActionDraft moveAction, int count, params ActionDraft[] actions)
        {
            name = name + ": " + count + " steps";
            List<ActionDraft> drafts = new List<ActionDraft>();
            for (int i = 0; i < count; i++)
            {
                drafts.Add(moveAction);
            }
            drafts.AddRange(actions);
            return new Strategy(name, drafts.ToArray());
        }
    }
}
