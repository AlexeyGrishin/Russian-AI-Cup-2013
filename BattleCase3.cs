using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    
    //состояние бойца - используется при эмуляции
    public class BattleWarrior3State
    {
        public BattleWarrior3State() { }
        public BattleWarrior3State(Warrior2 warrior)
        {
            Position = warrior.Position;
            Location = warrior.Location;
            Damage = 0;
            Healed = 0;
            InitialHitpoints = warrior.Hitpoints;
        }

        private int damage = 0;
        public int Damage { 
            get { 
                return damage; 
            } 
            set {
                damage = value;
            } 
        }
        private int healed = 0;
        public int Healed
        {
            get { return healed; }
            set
            {
                healed = value;
            }
        }

        public int RealDamage
        {
            get { return Hitpoints < 0 ? Damage + Hitpoints : Damage; }
        }

        public int InitialHitpoints { get; set; }
        public int Hitpoints { get { return InitialHitpoints - Damage + Healed; } }
        public bool Alive { get { return Hitpoints > 0; } }

        public PossibleMove Location { get; set; }
        public TrooperStance Position { get; set; }

        public BattleWarrior3State Copy()
        {
            return new BattleWarrior3State { InitialHitpoints = InitialHitpoints, Damage = Damage, Healed = Healed, Location = Location, Position = Position };
        }
    }

    //эмулируемый боец. содержит стэк состояний. при эмуляции добавляем состояние в стек и меняем его. если ход нам не подходит - откатываем состояние
    public class BattleWarrior3<T> : IMapContext where T : Warrior2
    {
        private BattleWarrior3State state {get {return states.Peek();}}
        private Stack<BattleWarrior3State> states = new Stack<BattleWarrior3State>();
        private T warrior;
        private WalkableMap battleMap;
        private IWarriorMaze<T> maze;

        public bool IsTeammate { get { return warrior.IsTeammate; } }

        public BattleWarrior3(T warrior, IWarriorMaze<T> maze)
        {
            this.warrior = warrior;
            this.maze = maze;
            states.Push(new BattleWarrior3State(warrior));
            battleMap = new WalkableMap(maze);
            state.Location = battleMap[warrior.Location];
            
        }

        public void RecalculateState(IEnumerable<BattleWarrior3<T>> alliesWithoutMe, IEnumerable<BattleWarrior3<T>> enemies, bool quick = false)
        {
            var busyPoints = alliesWithoutMe.Concat(enemies).Select(u => u.Location.Point).ToList();
            BuildWays((p) => busyPoints.Contains(p.Point), alliesWithoutMe, enemies, quick);
        }

        public bool CanAttack(BattleWarrior3<T> anotherWarrior)
        {
            return maze.CanAttack(this, anotherWarrior) && this.warrior.CanTheoreticallyAttack(anotherWarrior.Location, from: Location);
        }

        public bool CanSee(BattleWarrior3<T> anotherWarrior)
        {
            return maze.CanAttack(this, anotherWarrior) && this.warrior.CanTheoreticallySee(anotherWarrior.Location, from: Location);
        }

        public bool CanSee(PossibleMove move, TrooperStance enemyPosition)
        {
            return maze.CanAttack(Location.X, Location.Y, (TrooperStance)Math.Min((int)enemyPosition, (int)Position), move.X, move.Y) && this.warrior.CanTheoreticallySee(move, from: Location);
        }


        public bool CouldSee(PossibleMove move, TrooperStance enemyPosition)
        {
            return maze.CanAttack(warrior.Location.X, warrior.Location.Y, (TrooperStance)Math.Min((int)enemyPosition, (int)warrior.Position), move.X, move.Y) && this.warrior.CanTheoreticallySee(move, from: warrior.Location);
        }


        //индекс опасности - насколько опасна точка.
        private void DefineDangerIndex(BattleWarrior3<T> self, PossibleMove move, TrooperStance ourStance, IEnumerable<BattleWarrior3<T>> alliesAndSelf)
        {
            var maxAttackRange = 12;
            var pointsAmount = 0;
            var maxAttackRadius = Tool.GetRadius(maxAttackRange);
            int minDistance = 99, maxDistance = -1;
            for (var dx = -maxAttackRange; dx <= maxAttackRange; dx++)
            {
                for (var dy = -maxAttackRange; dy <= maxAttackRange; dy++)
                {
                    var point = battleMap.Get(move.X + dx, move.Y + dy);
                    if (point != null && maxAttackRadius.Contains(dx, dy) && maze.CanAttack(point.X, point.Y, ourStance, move.X, move.Y))
                    {
                        //ok, someone in this point may attack us. check that we do not see it
                        if (!alliesAndSelf.Any(a => a.warrior.CanTheoreticallySee(point.Point) && maze.CanAttack(a.Location.X, a.Location.Y, ourStance, point.X, point.Y)))
                        {
                            var distance = (int)point.RealDistanceTo(move);
                            minDistance = Math.Min(minDistance, distance);
                            maxDistance = Math.Max(maxDistance, distance);
                            pointsAmount++;
                        }
                    }
                }
            }
            var distancesRange = maxDistance - minDistance;
            var dangerIndex = pointsAmount > 0 ? (minDistance <= 4 ? 10 : 1) + distancesRange : 0;
            if (self.Location.SamePosition(move) && dangerIndex > 0)
            {
                dangerIndex = 33;   
            }
            Console.WriteLine("Danger for " + move + ": " + (pointsAmount > 0 ? "there are " + pointsAmount + " points attack this one with distances from " + minDistance + " to " + maxDistance + ", index = " + dangerIndex : "no danger"));    //[DEBUG]
            move.DangerIndex = dangerIndex; ;
        }

        private void PredictSniper(PossibleMove move, IEnumerable<BattleWarrior3<T>> alliesAndSelf)
        {
            var stancesToCheck = new[] { TrooperStance.Standing, TrooperStance.Kneeling, TrooperStance.Prone };
            move.PossibleSniper = stancesToCheck.Any(s =>
            {
                var seeSniper = alliesAndSelf.Any(a => a.CanSee(move, s) || a.CouldSee(move, s));
                var sniperRadius = Tool.GetRadius(MyStrategy.SniperRangeFor(s));
                return !seeSniper && alliesAndSelf.Any(a =>
                    maze.CanAttack(move, a, s) &&
                    sniperRadius.Contains(a.Location.X - move.X, a.Location.Y - move.Y)
                    );
            });

        }

        //тут мы смотрим на мир вокруг и даем разные оценки точкам
        private void BuildWays(Func<PossibleMove, bool> exclude, IEnumerable<BattleWarrior3<T>> allies, IEnumerable<BattleWarrior3<T>> enemies, bool quick)
        {
            bool snipersInKnownEnemies = enemies.Any(e => e.Type == TrooperType.Sniper);
            var scaryForSnipers = MyStrategy.AreThereSnipers.GetValueOrDefault(false) && !snipersInKnownEnemies;
            var observeRange = quick ? (warrior.AbsoluteMaxSteps + 2) : warrior.AbsoluteMaxSteps * 2;
            var analysisRange = observeRange;
            var dangerIndexingRange = observeRange;
            if (battleMap[Location].Step != 0 || IsTeammate)
            {
                if (scaryForSnipers && IsDetector(allies))
                    observeRange = Math.Max(12, observeRange);  //12 = max sniper range
                battleMap.BuildMapFrom(Location.Point, observeRange, (p) => !maze.IsFree(p.X, p.Y) || exclude(p));
            }
            state.Location = battleMap[Location];
            var alliesAndSelf = allies.Concat(new List<BattleWarrior3<T>> {this}).ToList();
            state.Location.ForEach(p =>
            {
                if (p.Step <= analysisRange)
                {
                    p.VisibleToEnemy = enemies.Any(e => e.warrior.CanTheoreticallySee(p) && maze.CanAttack(e, p, Position));
                    //TODO: точки где мы стояли до этого - считаем всегда "видимыми"
                    p.CanBeAttackedOnStand = enemies.Any(e => e.warrior.CanTheoreticallyAttack(p) && maze.CanAttack(e, p, TrooperStance.Standing));
                    p.CanBeAttackedOnKneel = enemies.Any(e => e.warrior.CanTheoreticallyAttack(p) && maze.CanAttack(e, p, TrooperStance.Kneeling));
                    p.CanBeAttackedOnProne = enemies.Any(e => e.warrior.CanTheoreticallyAttack(p) && maze.CanAttack(e, p, TrooperStance.Prone));
                    p.CanAttackFromHere = enemies.Any(e => warrior.CanTheoreticallyAttack(e.Location, from: p) && maze.CanAttack(p, e, Position));
                }
                if (!quick && p.Step <= dangerIndexingRange)
                {
                    DefineDangerIndex(this, p, this.Position, alliesAndSelf);
                }
                if (scaryForSnipers)
                {
                    PredictSniper(p, alliesAndSelf);
                }
            });
            BuildAttackingProps(allies, enemies);
            if (IsTeammate) BuildDefendingProps(allies, enemies);
        }

        private bool IsDetector(IEnumerable<BattleWarrior3<T>> allies)
        {
            if (allies.Count() == 0) return false;
            return warrior.VisionRange > allies.Max(m => m.Warrior.VisionRange);
        }

        //а тут строим маршруты - к врагу, от врага, чтобы кинуть гранату, чтобы полечить, и т.д.
        private void BuildAttackingProps(IEnumerable<BattleWarrior3<T>> allies, IEnumerable<BattleWarrior3<T>> enemies)
        {
            //1. way to became attacker - ways to point I can attack from with minimal danger
            var pointsToAttackFrom = state.Location.Where(p => p.CanAttackFromHere).OrderBy(p => p.Step).ToList();
            var safePoints = pointsToAttackFrom.Where(p => p.DangerIndex <= 0);
            var coverablePoints = pointsToAttackFrom.Where(p => !p.CanBeAttackedOnKneel || !p.CanBeAttackedOnProne);
            pointsToAttackFrom = safePoints.Concat(coverablePoints).Concat(pointsToAttackFrom.Where(p => !safePoints.Contains(p) && !coverablePoints.Contains(p)).Take(5)).ToList();
            WaysToAttack = pointsToAttackFrom.Select(p => p.PathToThis()).ToList();
            //2. steps to became attacker
            StepsToAttack = WaysToAttack.Select(w => w.Count() - 1).MinOr(100);
            //3. ability to became attacker (this turn, next turn)
            CanComeAndAttack = new bool[] { StepsToAttack <= warrior.AbsoluteMaxSteps, StepsToAttack <= warrior.MaxSteps + warrior.AbsoluteMaxSteps };

            if (warrior.HasGrenade)
            {
                var gr = Tool.GetRadius(warrior.GrenadeRange);
                WaysToThrow = state.Location.Where(p => gr.Contains(p.X - Location.X, p.Y - Location.Y)).Select(a => a.PathToThis()).ToList();
                var stepsToThrow = WaysToThrow.Select(w => w.Count() - 1).MinOr(100);
                CanComeAndThrowGrenade = new bool[] { stepsToThrow <= warrior.MaxSteps, stepsToThrow <= warrior.MaxSteps + warrior.AbsoluteMaxSteps };
            }
            else {
                WaysToThrow = new List<IEnumerable<PossibleMove>>();
                CanComeAndThrowGrenade = new bool[] {false, false};
            }

            if (IsDetector(allies))
            {
                CountOfInvisibleSnipers = Location.Where(a => a.PossibleSniper).Count();
                WaysToScout = Location.Where(a => a.PossibleSniper).Select(a => a.PathToThis());
            }
            else
            {
                CountOfInvisibleSnipers = 0;
                WaysToScout = new List<IEnumerable<PossibleMove>>();
            }


            
            //16. ability to shoot right from here
            CanAttackFromHere = state.Location.CanAttackFromHere;

            WaysToSafe = new List<IEnumerable<PossibleMove>>();
            CanGoAndHide = new bool[] { };
            WaysToHide = new List<IEnumerable<PossibleMove>>();
            WaysToHeal = new List<IEnumerable<PossibleMove>>();
            CanComeAndHeal = new bool[] { };
        }


        private void BuildDefendingProps(IEnumerable<BattleWarrior3<T>> allies, IEnumerable<BattleWarrior3<T>> enemies) 
        {
            //4. way to go out from attack - ways to points not attackable from standing
            WaysToSafe = state.Location.Where(p => p.Back == null ? p.CanHideFromAttackSomehow : ((!p.CanBeAttackedOnStand && p.Back.CanBeAttackedOnStand) || (!p.CanBeAttackedOnKneel && p.Back.CanBeAttackedOnKneel) || (!p.CanBeAttackedOnProne && p.Back.CanBeAttackedOnProne))).Select(p => p.PathToThis()).ToList();
            //if (WaysToSafe.Count() == 1 && WaysToSafe.First().Count() == 1)
            {
                //if do not have ways to go - then go to ally
                WaysToSafe = WaysToSafe.Concat(allies.Select(a => battleMap.PointsAround(this[a.Location]).OrderBy(p => p.DangerIndex).FirstOrDefault()).Where(p => p != null).Select(p => p.PathToThis())).ToList();
            }
            //5. steps to gout out from attack
            var stepsToBecameNotAttackable = WaysToSafe.Select(w => w.Count() - 1).MinOr(100);
            //6. ability to go out from attack (this turn, next turn)
            CanGoAndHide = new bool[] { stepsToBecameNotAttackable <= warrior.MaxSteps, stepsToBecameNotAttackable <= warrior.MaxSteps + warrior.AbsoluteMaxSteps };
            //12. way to go out from visibility range - not only range, visibility shall be used too
            WaysToHide = state.Location.Where(p => !p.VisibleToEnemy, andParent: p => p.VisibleToEnemy).Select(p => p.PathToThis()).ToList();
            //14. steps to go out from visibility range
            var stepsToBecameInvisible = WaysToHide.Select(w => w.Count() - 1).MinOr(100);
            //13. ability to go from visibility
            var canBecameInvisible = new bool[] { stepsToBecameInvisible <= warrior.MaxSteps, stepsToBecameInvisible <= warrior.MaxSteps + warrior.AbsoluteMaxSteps };
            //15. way to sick ally
            WaysToHeal = allies.Where(a => a.warrior.IsSick).Select(a => battleMap[a.Location]).OrderBy(a => a.DistanceTo(Location)).Select(p => battleMap.FindWay(Location, p)).ToList();
            var stepsToSickAllies = WaysToHeal.Select(w => w.Count() - 1).MinOr(100);
            CanComeAndHeal = new bool[] { stepsToSickAllies <= warrior.MaxSteps, stepsToSickAllies <= warrior.MaxSteps + warrior.AbsoluteMaxSteps };
        }

        public bool CanAttackFromHere { get; private set; }
        public int StepsToAttack { get; private set; }
        public bool[] CanComeAndAttack { get; private set; }
        public bool[] CanGoAndHide { get; private set; }
        public bool[] CanComeAndHeal { get; private set; }
        public bool[] CanComeAndThrowGrenade { get; private set; }

        public IEnumerable<IEnumerable<PossibleMove>> WaysToAttack { get; private set; }
        public IEnumerable<IEnumerable<PossibleMove>> WaysToSafe { get; private set; }
        public IEnumerable<IEnumerable<PossibleMove>> WaysToHide { get; private set; }
        public IEnumerable<IEnumerable<PossibleMove>> WaysToHeal { get; private set; }
        public IEnumerable<IEnumerable<PossibleMove>> WaysToThrow { get; private set; }
        public IEnumerable<IEnumerable<PossibleMove>> WaysToScout { get; private set; }

        public int CountOfInvisibleSnipers = 0;


        public override string ToString()
        {
            return String.Format("{6} {0}[{1}]\n  WaysToAttack: {2}\n\n  WaysToSafe: {3}\n  --\n  Can come and attack: {4}\n  Can go and hide: {5}\n\n", warrior.Type, warrior.Location.Point, 
                String.Join("\n", WaysToAttack.Select(w => w.AsString(p => "d=" + p.DangerIndex + " a>=" + p.CanAttackFromHere + " a<=" + p.CanBeAttackedSomehow ))),
                String.Join("\n", WaysToSafe.Select(w => w.AsString(p => "d=" + p.DangerIndex + " a>=" + p.CanAttackFromHere + " a<=" + p.CanBeAttackedSomehow))),
                String.Join(",", CanComeAndAttack), String.Join(",", CanGoAndHide), warrior.IsTeammate ? "Our" : "Enemy's "
            );
        }

        public string ShortString()
        {
            return String.Format("{0} {1} ({2}/{3} -> {4}/{5})", warrior.IsTeammate ? "Our" : "Enemy's", warrior.Type, warrior.Location.Point, warrior.Position, Location.Point, Position);
        }

        public void BeginCase()
        {
            states.Push(state.Copy());
        }

        public BattleWarrior3State EndCase()
        {
            return states.Pop();
        }

        public void BeginCase(BattleWarrior3State state)
        {
            states.Push(state);
        }

        public BattleWarrior3State GetCase()
        {
            return state;
        }

        public int Damage {
            get { return state.Damage; }
            set { state.Damage = value; }
        }

        public int Healed {
            get { return state.Healed; }
            set { state.Healed = value; }
        }
        public int RealDamage {
            get { return state.RealDamage; }
        }
        public int Hitpoints {
            get { return state.Hitpoints; }
        }
        public bool Alive {
            get { return state.Alive;  }
        }
        public PossibleMove Location {
            get { return state.Location; }
            set { state.Location = value; }
        }
        public TrooperStance Position {
            get { return state.Position; }
            set { state.Position = value; }
        }

        public PossibleMove this[PossibleMove key]
        {
            get { return battleMap[key]; }
        }

        public PossibleMove this[AI.Point key]
        {
            get { return battleMap[key]; }
        }

        public TrooperType Type { get { return warrior.Type; } }

        public T Warrior { get { return warrior; } }

        public WalkableMap BattleMap { get { return battleMap; } }
    }


    public static class HelperMazeExtensions
    {
        public static bool CanAttack<T>(this IVisibilityMaze maze, BattleWarrior3<T> attacker, BattleWarrior3<T> attacked) where T: Warrior2
        {
            return maze.CanAttack(attacker.Location.X, attacker.Location.Y, (TrooperStance)Math.Min((int)attacker.Position, (int)attacked.Position), attacked.Location.X, attacked.Location.Y);
        }

        public static bool CanAttack<T>(this IVisibilityMaze maze, BattleWarrior3<T> attacker, BattleWarrior3<T> attacked, TrooperStance ourStance) where T : Warrior2
        {
            return maze.CanAttack(attacker.Location.X, attacker.Location.Y, (TrooperStance)Math.Min((int)attacker.Position, (int)ourStance), attacked.Location.X, attacked.Location.Y);
        }

        public static bool CanAttack<T>(this IVisibilityMaze maze, BattleWarrior3<T> attacker, PossibleMove attacked, TrooperStance ourStance) where T : Warrior2
        {
            return maze.CanAttack(attacker.Location.X, attacker.Location.Y, (TrooperStance)Math.Min((int)attacker.Position, (int)ourStance), attacked.X, attacked.Y);
        }

        public static bool CanAttack<T>(this IVisibilityMaze maze, PossibleMove attacker, BattleWarrior3<T> attacked, TrooperStance ourStance) where T : Warrior2
        {
            return maze.CanAttack(attacker.X, attacker.Y, (TrooperStance)Math.Min((int)attacked.Position, (int)ourStance), attacked.Location.X, attacked.Location.Y);
        }
    }

    //состояние всей "битвы", т.е. всех участников боя
    public class BattleCase3State
    {
        public String Name { get; set; }

        public BattleWarrior3State[] Allies { get; set; }
        public BattleWarrior3State[] Enemies { get; set; }
        public readonly static int DeadDamageBonus = 150;
        public int AlliesDamage { get { return Allies.Sum(a => a.Damage + (a.Alive ? 0 : DeadDamageBonus)); } }
        public int EnemiesDamage { get { return Enemies.Sum(e => e.Damage + (e.Alive ? 0 : DeadDamageBonus)); } }

        public override string ToString()
        {
            return String.Format("{0} - o/e {1}/{2}", Name, AlliesDamage, EnemiesDamage);
        }
    }

    //представляет собой текущую ситуацию на поле боя.
    public class BattleCase3<T>: IMapContext where T: Warrior2
    {
        public BattleCase3(T self, T enemy, IWarriorMaze<T> maze, IEnumerable<T> allies = null, IEnumerable<T> enemies = null)
            : this(self, maze, (allies ?? new List<T>()), (enemies ?? new List<T>()).Concat(new []{enemy}))
        {
            //for unit tests
        }
        public BattleCase3(T self, IWarriorMaze<T> maze, IEnumerable<T> allies, IEnumerable<T> enemies)
        {
            Self = new BattleWarrior3<T>(self, maze);
            Allies = allies.Select(a => new BattleWarrior3<T>(a, maze)).ToList();
            AlliesAndSelf = Allies.Concat(new BattleWarrior3<T>[] { Self }).ToList();
            Enemies = enemies.Select(a => new BattleWarrior3<T>(a, maze)).ToList();
            All = AlliesAndSelf.Concat(Enemies).ToList();
            SickAllies = AlliesAndSelf.Where(a => a.Warrior.IsSick);
            Self.RecalculateState(Allies, Enemies);
        }

        public BattleWarrior3<T> Self { get; private set; }
        public IList<BattleWarrior3<T>> Allies { get; private set; }
        public IList<BattleWarrior3<T>> AlliesAndSelf { get; private set; }
        public IList<BattleWarrior3<T>> Enemies { get; private set; }
        public IList<BattleWarrior3<T>> All { get; private set; }
        public IEnumerable<BattleWarrior3<T>> SickAllies { get; private set; }

        public PossibleMove this[PossibleMove key]
        {
            get { return Self[key]; }
        }

        public PossibleMove this[Point key]
        {
            get { return Self[key]; }
        }

        public void BeginCase()
        {
            All.ForEach(w => w.BeginCase());
        }

        public BattleCase3State EndCase(string name = "")
        {
            return new BattleCase3State
            {
                Name = name,
                Allies = AlliesAndSelf.Select(w => w.EndCase()).ToArray(),
                Enemies = Enemies.Select(w => w.EndCase()).ToArray()
            };
        }

        private Stack<BattleCase3State> namedStates = new Stack<BattleCase3State>();

        public void BeginCase(BattleCase3State state)
        {
            if (state.Name != null) namedStates.Push(state);
            state.Allies.ForEach((stat, idx) => AlliesAndSelf[idx].BeginCase(stat));
            state.Enemies.ForEach((stat, idx) => Enemies[idx].BeginCase(stat));
        }

        public void Reset()
        {
            foreach (var n in namedStates)
            {
                EndCase();
            }
            namedStates.Clear();

        }

        public IEnumerable<BattleCase3State> AllStates { get { return namedStates.ToList() ; } }

        public BattleCase3State GetCase(string name = "")
        {
            return new BattleCase3State { 
                Name = name, 
                Allies = AlliesAndSelf.Select(w => w.GetCase()).ToArray(), 
                Enemies = Enemies.Select(w => w.GetCase()).ToArray() 
            };
        }


        public void Recalculate(BattleWarrior3<T> unit, bool quick = false)
        {
            unit.RecalculateState(GetAlliesFor(unit), GetEnemiesFor(unit), quick: quick || unit != Self);
        }

        public IEnumerable<BattleWarrior3<T>> GetAlliesFor(BattleWarrior3<T> w)
        {
            return (w.IsTeammate ? AlliesAndSelf : Enemies).Where(wr => wr != w).ToList();
        }
        public IEnumerable<BattleWarrior3<T>> GetEnemiesFor(BattleWarrior3<T> w)
        {
            return (w.IsTeammate ? Enemies : AlliesAndSelf).ToList();
        }

        public Case3<T> NewCase()
        {
            return new Case3<T>(this);
        }


        public String ToUnitTest()
        {
            var s = new StringBuilder();
            
            return s.ToString();
        }
    }

    public class Case3<T>: IDisposable where T: Warrior2
    {
        private BattleCase3<T> battle3;
        internal Case3(BattleCase3<T> battle3)
        {
            this.battle3 = battle3;
            battle3.BeginCase();
        }



        public void Dispose()
        {
            battle3.EndCase();
        }
    }

}
