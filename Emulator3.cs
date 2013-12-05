using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{

    public class StrategyChange3 : IComparable
    {
        public int EnemyDamage = 0;
        public int OurDamage = 0;
        public int EnemyLost = 0;
        public int OurLost = 0;
        public int OurHeal = 0;
        public int PotentiallyHeal = 0;
        public int LostMobility = 0;
        public int DangerIndex = 0;
        public int ReadyToAttack = 0;

        public int CountOfInvisibleSnipersO_o = 0;

        public String Name;

        public StrategyChange3(String name, int step)
        {
            this.Name = name + ": " + step;
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

        public int CompareTo(StrategyChange3 another)
        {
            if (another.OurLost != OurLost) return Worse(OurLost - another.OurLost);
            if (another.EnemyLost != EnemyLost) return Better(EnemyLost - another.EnemyLost);
            if (another.UnitedDamage != UnitedDamage) return Better(UnitedDamage - another.UnitedDamage);

            if (another.CountOfInvisibleSnipersO_o != CountOfInvisibleSnipersO_o) return Worse(CountOfInvisibleSnipersO_o - another.CountOfInvisibleSnipersO_o);
            if (another.PotentiallyHeal != PotentiallyHeal) return Better(PotentiallyHeal - another.PotentiallyHeal);
            if (another.DangerIndex != DangerIndex) return Worse(DangerIndex - another.DangerIndex);
            if (another.ReadyToAttack != ReadyToAttack) return Better(ReadyToAttack - another.ReadyToAttack);
            if (another.LostMobility != LostMobility) return Worse(LostMobility - another.LostMobility);

            return 0;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as StrategyChange3);
        }

        private static string F(int flag, string c)
        {
            if (flag > 0) return c + "+";
            if (flag < 0) return c + "-";
            return c + " ";
        }

        public override string ToString()
        {
            return String.Format("(o/e) lost = {0}/{1} damage = {2}/{3}(+{4}={5}) = {6}, flags = {7} {9} {11}, danger = {8}, O_o = {10}",
                OurLost,    EnemyLost,         OurDamage,      EnemyDamage, 
                OurHeal,    EnemyDamageNHeal,  UnitedDamage,   F(PotentiallyHeal, "hea"),
                DangerIndex, F(-LostMobility, "mob"), CountOfInvisibleSnipersO_o, F(ReadyToAttack, "atk")
                );
        }


        internal void InitFrom(StrategyChange3 ChangeThisTurn)
        {
            OurLost = ChangeThisTurn.OurLost;
            OurDamage = ChangeThisTurn.OurDamage;
            OurHeal = ChangeThisTurn.OurHeal;
            EnemyDamage = ChangeThisTurn.EnemyDamage;
            EnemyLost = ChangeThisTurn.EnemyLost;
            DangerIndex = ChangeThisTurn.DangerIndex;
            LostMobility = ChangeThisTurn.LostMobility;
        }
    }

    public class StrategyResult3 : IComparable
    {
        public StrategyChange3 ChangeThisTurn { get; private set; }
        public StrategyChange3 ChangeNextTurn { get; private set; }
        public List<Move> Moves { get; private set; }
        public string Name { get; set; }
        public string ReasonOfImpossibility { get; set; }
        public StrategyResult3(string name)
        {
            Name = name;
            ChangeThisTurn = new StrategyChange3(name, 0);
            ChangeNextTurn = new StrategyChange3(name, 1);
            Moves = new List<Move>();
        }
        public override string ToString()
        {
            return String.Format("{0}: {1} --> {2} --> {3}", Name, 
                ReasonOfImpossibility ?? String.Join(",", Moves.Select(m => "[" + m.AsString() + "]")), 
                ReasonOfImpossibility == null ? ChangeThisTurn.ToString() : "skip",
                ReasonOfImpossibility == null ? ChangeNextTurn.ToString() : ""
                );
        }

        public string ToString2()
        {
            return ReasonOfImpossibility == null ? Description : (Name + ": " + ReasonOfImpossibility);
        }

        public StrategyResult3 Impossible(string p)
        {
            ReasonOfImpossibility = p;
            return this;
        }

        public bool SetImpossible(string p)
        {
            ReasonOfImpossibility = p;
            return false;
        }

        public void StartNextTurn()
        {
            ChangeNextTurn.InitFrom(ChangeThisTurn);
        }


        public bool Possible { get { return ReasonOfImpossibility == null; } }

        public int CompareTo(object obj)
        {
            var sr = obj as StrategyResult3;
            if (Possible != sr.Possible) return Possible.CompareTo(sr.Possible);
            var res = ChangeNextTurn.CompareTo(sr.ChangeNextTurn);
            if (res == 0)
            {
                res = ChangeThisTurn.CompareTo(sr.ChangeThisTurn);
            }
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


        public string Description { get; set; }
    }

    public class Emulator3
    {

        public static StrategyResult3 Emulate<T>(BattleCase3<T> battleCase, AI.Battle.IStrategy strategy) where T: Warrior2
        {
            var ways = Tool.DistinctWays(GetWays<T>(battleCase, strategy), strategy.StepsCount + 1);
            //using (Tool.Timer(strategy.Name + ": " + ways.Count()))
            {
                var results = ways.Select(w => Emulate<T>(battleCase, strategy, w)).ToList();
                results.Sort();
                results.Reverse();
                return results.FirstOrDefault() ?? new StrategyResult3(strategy.Name).Impossible("There is no way found for it...");
            }
        }

        private static IEnumerable<IEnumerable<PossibleMove>> GetWays<T>(BattleCase3<T> battleCase, AI.Battle.IStrategy strategy) where T : Warrior2
        {
            var moveType = strategy.Actions.Where(IsMoving).FirstOrDefault();
            switch (moveType)
            {
                case ActionDraft.None: return new List<IEnumerable<PossibleMove>> { new List<PossibleMove> {battleCase.Self.Location} };
                case ActionDraft.StepToEnemy: return battleCase.Self.WaysToAttack;
                case ActionDraft.StepFromEnemy: return battleCase.Self.WaysToSafe.Concat(battleCase.Self.WaysToHide).ToList();
                case ActionDraft.StepToSickAlly: return battleCase.Self.WaysToHeal;
                case ActionDraft.StepToThrow: return battleCase.Self.WaysToThrow;
                case ActionDraft.StepToScout: return battleCase.Self.WaysToScout;
            }
            throw new Exception("Illegal state");
        }

        public static bool IsMoving(ActionDraft arg)
        {
 	        return arg == ActionDraft.StepFromEnemy || arg == ActionDraft.StepToEnemy || arg == ActionDraft.StepToSickAlly || arg == ActionDraft.StepToThrow || arg == ActionDraft.StepToScout;
        }

        public static StrategyResult3 Emulate<T>(BattleCase3<T> battleCase, AI.Battle.IStrategy strategy, IEnumerable<PossibleMove> wayToFollow) where T: Warrior2
        {
            var res = new StrategyResult3(strategy.Name);
            battleCase.BeginCase();
            var succeed = EmulateStrategy(res, battleCase, strategy, wayToFollow);
            var dirty = false;
            if (succeed)
            {
                //using (Tool.Timer("..enemies"))
                {
                    EmulateEnemiesTurn(res, battleCase);
                }
                //using (Tool.Timer("..after enemies"))
                {
                    Fill(res.ChangeThisTurn, battleCase);
                    res.StartNextTurn();
                }
                //using (Tool.Timer("..self"))
                {
                    PredictNextTurn(res, battleCase);
                    Fill(res.ChangeNextTurn, battleCase);
                    dirty = true;
                }
                res.Description = ToDescription(res, battleCase);   //[DEBUG]
                battleCase.Reset();
            }
            battleCase.EndCase();
            if (dirty)
                battleCase.Recalculate(battleCase.Self, quick: true);
            return res;
        }

        private static void Fill<T>(StrategyChange3 strategyChange3, BattleCase3<T> battleCase) where T : Warrior2
        {
            strategyChange3.OurLost = battleCase.AlliesAndSelf.Count(a => !a.Alive);
            strategyChange3.OurDamage = battleCase.AlliesAndSelf.Sum(a => a.RealDamage);
            strategyChange3.OurHeal = battleCase.AlliesAndSelf.Sum(a => a.Healed);
            strategyChange3.EnemyLost = battleCase.Enemies.Count(e => !e.Alive);
            strategyChange3.EnemyDamage = battleCase.Enemies.Sum(e => e.RealDamage);
            strategyChange3.DangerIndex = battleCase.Self.Location.DangerIndex;
            var oldLocation = battleCase.Self[battleCase.Self.Warrior.Location];
            var newLocation = battleCase.Self[battleCase.Self.Location];
            if (battleCase.Self.Warrior.CanHeal)
                strategyChange3.PotentiallyHeal = battleCase.Allies.Select(a => a.Location.DistanceTo(oldLocation) - a.Location.DistanceTo(newLocation)).MaxOr(0);
            var oldPosition = battleCase.Self.Warrior.Position;//2
            var newPosition = battleCase.Self.Position;//0
            strategyChange3.LostMobility = (oldPosition - newPosition);
            if (battleCase.Enemies.Any(e => e.Alive && battleCase.Self.CanAttack(e)))
            {
                strategyChange3.LostMobility = 0;
                if (!oldLocation.CanAttackFromHere)
                    strategyChange3.ReadyToAttack = 0;
            }
            if (newLocation.CanHideFromAttackSomehow)
                strategyChange3.LostMobility = 0;
            if (battleCase.Self.CountOfInvisibleSnipers > 0)
                strategyChange3.CountOfInvisibleSnipersO_o = battleCase.Self.CountOfInvisibleSnipers;

        }

        private static String ToDescription<T>(StrategyResult3 result, BattleCase3<T> battleCase) where T: Warrior2
        {
            var s = new StringBuilder();
            s.AppendLine(battleCase.Self.ShortString() + " " + result.Name + " --> " + String.Join(",", result.Moves.Select(m => "[" + m.AsString() + "]")) + " --> " + result.ChangeNextTurn.ToString());
            var rs = battleCase.AllStates;
            rs = rs.Reverse();
            foreach (var stateInStack in rs)
                if (stateInStack.Name != null) s.AppendLine("   then " + stateInStack.Name);
            s.AppendLine("leads to");
            foreach (var trooper in battleCase.All)
            {
                s.AppendLine(trooper.ShortString() + " " + (trooper.Alive ? (trooper.Warrior.Hitpoints + " -> " + trooper.Hitpoints + ("(-" + trooper.Damage + "+" + trooper.Healed + ")")) : "alive -> dead"));
            }
            return s.ToString();
        }


        private static bool EmulateStrategy<T>(StrategyResult3 sr, BattleCase3<T> battleCase, AI.Battle.IStrategy strategy, IEnumerable<PossibleMove> way) where T : Warrior2
        {
            int points = battleCase.Self.Warrior.Actions;
            var actions = strategy.Actions.ToList();
            var hasRation = battleCase.Self.Warrior.HasFieldRation;
            var hasMedkit = battleCase.Self.Warrior.HasMedkit;
            var self = battleCase.Self;
            if (self.Warrior.IsSick && self.Warrior.HasMedkit && self.Warrior.DoIfCan(ActionType.UseMedikit, ref points))
            {
                sr.Moves.Add(new Move { Action = ActionType.UseMedikit, X = battleCase.Self.Location.X, Y = battleCase.Self.Location.Y });
                self.Healed += self.Warrior.MedkitHealth;
                hasMedkit = false;
            }
            int distance = 0, overallDistance = 0;
            //
            foreach (var action in actions)
            {
                ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                if (IsMoving(action))
                {
                    distance += 1;
                    continue;
                }
                else if (action == ActionDraft.StepBack)
                {
                    distance -= 1;
                    continue;
                }
                else if (distance != 0)
                {
                    if (!Move(sr, way.ToList(), distance, battleCase, ref points, ref hasRation)) return false;
                    overallDistance += distance;
                    distance = 0;
                }
                if (action == ActionDraft.Shoot)
                {
                    var pointsAfterRepeatedAction = (actions.IndexOf(action) == actions.Count - 1) ? 0 : actions.GetRange(actions.IndexOf(action) + 1, actions.Count - actions.IndexOf(action) - 1).Select(n => battleCase.Self.Warrior.Cost(n)).Sum();
                    points -= pointsAfterRepeatedAction;
                    if (points <= 0) return sr.SetImpossible(String.Format("Not enogh points to shoot at least once"));
                    while (Shoot(sr, battleCase.Self, battleCase, ref points))
                    {
                        ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                    };
                    if (!sr.Possible) return false;
                    points += pointsAfterRepeatedAction;
                }
                else if (action == ActionDraft.StandUp || action == ActionDraft.OnKneel || action == ActionDraft.LieDown)
                {
                    var targetPosition = (TrooperStance)((int)TrooperStance.Standing - (action - ActionDraft.StandUp));
                    if (!TryToChangePosition(sr, self, targetPosition, ref points)) return false;
                }
                else if (action == ActionDraft.Skip)
                {
                    sr.Moves.Add(new Move().Wait());
                }
                else if (action == ActionDraft.HealAlly)
                {
                    if (!battleCase.Self.Warrior.CanHeal) return sr.SetImpossible("Unit is not a medic and does not have medkit!");
                    if (!Heal(sr, battleCase, ref points, ref hasRation, ref hasMedkit)) return false;
                }
                else if (action == ActionDraft.HealSelf)
                {
                    //TODO: reduce .Warrior calls - wrap...
                    if (!hasMedkit) return sr.SetImpossible("Unit does not have medkit!");
                    if (!self.Warrior.DoIfCan(ActionType.UseMedikit, ref points)) return sr.SetImpossible(String.Format("Cannot use medkit - not enough points({0})", points));
                    sr.Moves.Add(new Move { Action = ActionType.UseMedikit });
                    self.Healed += battleCase.Self.Warrior.MedicHealth;
                    hasMedkit = false;
                }
                else if (action == ActionDraft.ThrowGrenade)
                {
                    if (!ThrowGrenate(sr, battleCase, ref points)) return false;
                }
                
            }
            if (distance != 0)
            {
                if (!Move(sr, way.ToList(), distance, battleCase, ref points, ref hasRation)) return false;
            }
            if (sr.Moves.Any(m => m.Action == ActionType.EatFieldRation))
            {
                if (sr.Moves.Last().Action == ActionType.EatFieldRation)
                {
                    sr.Moves.RemoveAt(sr.Moves.Count - 1);
                }
                else if (points >= battleCase.Self.Warrior.FieldRationExtraPoints)
                {
                    Console.WriteLine("Cancel eating ration - it was not used"); //[DEBUG]
                    sr.Moves.RemoveAll(s => s.Action == ActionType.EatFieldRation);
                }
            }
            //after all
            if (self.Location.CanBeAttacked && !self.Location.CanBeAttackedOnKneel && self.Position == TrooperStance.Standing)
            {
                TryToChangePosition(sr, self, TrooperStance.Kneeling, ref points, required: false);
            }
            if (self.Location.CanBeAttacked && self.Location.CanBeAttackedOnKneel && !self.Location.CanBeAttackedOnProne)
            {
                TryToChangePosition(sr, self, TrooperStance.Prone, ref points, required:false);
            }
            return true;
        }

        private static bool TryToChangePosition<T>(StrategyResult3 res, BattleWarrior3<T> self, TrooperStance targetPosition, ref int points, bool required = true) where T: Warrior2
        {
            var down = targetPosition < self.Position; var count = Math.Abs(targetPosition - self.Position);
            if (targetPosition == self.Position) return true;
            var actionType = down ? ActionType.LowerStance : ActionType.RaiseStance;
            if (!self.Warrior.DoIfCan(actionType, count, ref points))
            {
                return required && res.SetImpossible(String.Format("Cannot {0} {1} times - not enough points ({2})", actionType, count, points));
            }
            for (var i = 0; i < count; i++) res.Moves.Add(new Move { Action = actionType });
            self.Position = targetPosition;
            return true;
        }

        private static void EmulateEnemiesTurn<T>(StrategyResult3 res, BattleCase3<T> battleCase) where T : Warrior2
        {
            EmulateTurn(battleCase, battleCase.Enemies.Where(e => e.Alive).ToList(), new MaxAlliesDamage());
        }

        private static void EmulateTurn<T>(BattleCase3<T> battleCase, IEnumerable<BattleWarrior3<T>> unitsToTurn, IComparer<BattleCase3State> comparer) where T : Warrior2
        {
            foreach (var unit in unitsToTurn)
            {
                //using (Tool.Timer("...recalculate"))
                {
                    battleCase.Recalculate(unit, quick: true);
                }
                var maxPointsWithRation = unit.Warrior.MaxActions + (unit.Warrior.HasFieldRation ? unit.Warrior.FieldRationExtraPoints : 0);
                int pointsBeforeMove = (TrooperStance.Standing - unit.Position) * unit.Warrior.Cost(ActionType.RaiseStance);

                var cases = new List<BattleCase3State>();
                //using (Tool.Timer("...cases "))
                //TODO: for moves - analyze position of enemy. If Prone/Kneeling - let them stand up
                {
                    for (var wl = 0; wl <= MyStrategy.MaxStepsEnemyWillDo; wl++)
                    {
                        var waysToTry = wl == 0 ? new List<IEnumerable<PossibleMove>> { new[] { unit.Location } } : Tool.DistinctWays(unit.WaysToAttack, wl + 1);
                        foreach (var way in waysToTry)
                        {
                            var enemyPoints = maxPointsWithRation;
                            var wayLength = way.Count() - 1;
                            if (wayLength > 0)
                            {
                                enemyPoints -= pointsBeforeMove;
                                if (!unit.Warrior.DoIfCan(ActionType.Move, wayLength, ref enemyPoints)) continue;
                            }
                            battleCase.BeginCase();
                            unit.Location = way.Last();
                            var attackedUnits = new List<BattleWarrior3<T>>();
                            while (unit.Warrior.Can(ActionType.Shoot, 1, enemyPoints))
                            {
                                var lastAttackedUnit = battleCase.GetEnemiesFor(unit).Where(e => e.Alive && unitsToTurn.Any(a => a.CanSee(e))).Where(a => unit.CanAttack(a)).OrderBy(a => a.Hitpoints).FirstOrDefault();
                                if (lastAttackedUnit == null) break;
                                if (attackedUnits.Count() == 0 || !attackedUnits.Last().Location.SamePosition(lastAttackedUnit.Location))
                                    attackedUnits.Add(lastAttackedUnit);
                                unit.Warrior.DoIfCan(ActionType.Shoot, ref enemyPoints);
                                lastAttackedUnit.Damage += unit.Warrior.GetDamage();
                            }
                            if (wl == 0 || attackedUnits.Count() > 0)   //otherwise we emulate cases like 'enemy just comes closer to get bullet'
                                cases.Add(battleCase.EndCase(unit.ShortString() + " come " + wl + " steps and shoot " + (attackedUnits.Count() == 0 ? "nowhere" : String.Join(",", attackedUnits.Select(au => au.ShortString())))));
                            else
                                battleCase.EndCase();
                        }
                    }
                    if (unit.CanComeAndThrowGrenade[0])
                    {
                        var pointsToMove = Math.Max(0, maxPointsWithRation - unit.Warrior.Cost(ActionType.ThrowGrenade) - pointsBeforeMove);

                        var canDoSteps = pointsToMove / unit.Warrior.Cost(ActionType.Move);
                        var ways = unit.WaysToThrow.Where(w => w.Count() <= canDoSteps + 1);
                        foreach (var wayToThrow in ways)
                        {
                            unit.Location = wayToThrow.Last();
                            var pointsToBeAttacked = new List<PossibleMove>();
                            var myEnemies = battleCase.GetEnemiesFor(unit).ToList();
                            myEnemies.Select(e => e.BattleMap.PointsAround(e.Location).Concat(new[] { e.Location })).ForEach(p => pointsToBeAttacked.AddRange(p));
                            pointsToBeAttacked = pointsToBeAttacked.Where(p => p.RealDistanceTo(unit.Location) <= unit.Warrior.GrenadeRange).ToList();
                            foreach (var grenadeTarget in pointsToBeAttacked)
                            {
                                battleCase.BeginCase();
                                var affectedEnemies = myEnemies.Where(w => w.Location.DistanceTo(grenadeTarget) <= 1);

                                if (affectedEnemies.Any())
                                {
                                    affectedEnemies.ForEach(w => w.Damage += unit.Warrior.GetGrenadeDamage(w.Location.DistanceTo(grenadeTarget)));
                                    cases.Add(battleCase.EndCase(unit.ShortString() + " come " + wayToThrow.Count() + " steps and throw grenade to " + grenadeTarget.Point + ", attacking " + String.Join(",", affectedEnemies.Select(a => a.ShortString()))));
                                }
                                else
                                {
                                    battleCase.EndCase();
                                }

                            }
                        }
                    }
                    cases.Sort(comparer);
                    cases.Reverse();
                }
                //Console.WriteLine(String.Join("\n", cases));
                //using (Tool.Timer("...begin case"))
                {
                    var bestCase = cases.FirstOrDefault();
                    if (bestCase != null)
                    {
                        battleCase.BeginCase(bestCase);
                    }
                }
            }
            //TODO: expected damage VS real damage
        }

        class MaxAlliesDamage: IComparer<BattleCase3State>
        {
            public int Compare(BattleCase3State x, BattleCase3State y)
            {
                return x.AlliesDamage.CompareTo(y.AlliesDamage);
            }
        }
        class MaxEnemyDamage : IComparer<BattleCase3State>
        {
            public int Compare(BattleCase3State x, BattleCase3State y)
            {
                return x.EnemiesDamage.CompareTo(y.EnemiesDamage);
            }
        }

        private static void PredictNextTurn<T>(StrategyResult3 res, BattleCase3<T> battleCase) where T : Warrior2
        {
            EmulateTurn(battleCase, battleCase.Allies.Where(e => e.Alive).ToList(), new MaxEnemyDamage());
            battleCase.Recalculate(battleCase.Self, quick: true);
        }

        private static void ExtraRation<T>(StrategyResult3 sr, BattleCase3<T> battleCase, ref int points, ref bool hasRation) where T : Warrior2
        {
            if (hasRation)
                if (points < battleCase.Self.Warrior.MaxActions && battleCase.Self.Warrior.Can(ActionType.EatFieldRation, 1, points))
                {
                    sr.Moves.Add(new Move { Action = ActionType.EatFieldRation, X = battleCase.Self.Location.X, Y = battleCase.Self.Location.Y });
                    points += battleCase.Self.Warrior.FieldRationExtraPoints;
                    if (points > battleCase.Self.Warrior.MaxActions) points = battleCase.Self.Warrior.MaxActions;
                    hasRation = false;
                }

        }

        private static bool Heal<T>(StrategyResult3 sr, BattleCase3<T> battleCase, ref int points, ref bool hasRation, ref bool hasMedkit) where T : Warrior2
        {
            if (battleCase.SickAllies.All(a => a.Location.DistanceTo(battleCase.Self.Location) > 1)) return sr.SetImpossible("Cannot heal - distance > 1");
            if (!battleCase.Self.Warrior.Can(ActionType.Heal, 1, points)) return sr.SetImpossible("Not enough points to just heal once");
            var sickAllyNear = battleCase.SickAllies.Where(a => a.Location.DistanceTo(battleCase.Self.Location) <= 1).OrderBy(a => a.Warrior.Hitpoints).FirstOrDefault();
            if (hasMedkit && battleCase.Self.Warrior.DoIfCan(ActionType.UseMedikit, ref points))
            {
                sr.Moves.Add(new Move { Action = ActionType.UseMedikit, X = sickAllyNear.Location.X, Y = sickAllyNear.Location.Y });
                sickAllyNear.Healed += battleCase.Self.Warrior.GetMedkitHealth(sickAllyNear.Warrior);
                ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                hasMedkit = false;
            }
            while (battleCase.Self.Warrior.IsMedic && battleCase.Self.Warrior.DoIfCan(ActionType.Heal, ref points))
            {
                sr.Moves.Add(new Move { Action = ActionType.Heal, X = sickAllyNear.Location.X, Y = sickAllyNear.Location.Y });
                sickAllyNear.Healed += battleCase.Self.Warrior.MedicHealth;
                ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                if (sickAllyNear.Hitpoints >= 100)
                {
                    break;
                }
            }

            return true;
        }


        private static bool CheckStanceBeforeMoving<T>(StrategyResult3 sr, BattleCase3<T> battleCase, ref int points) where T : Warrior2
        {
            if (battleCase.Self.Position == TrooperStance.Standing) return true;
            var count = (int)(TrooperStance.Standing - battleCase.Self.Position);
            if (!battleCase.Self.Warrior.DoIfCan(ActionType.RaiseStance, count, ref points)) return sr.SetImpossible("Trying to stand up to move, but cannot - not enough points");
            for (var i = 0; i < count; i++) sr.Moves.Add(new Move { Action = ActionType.RaiseStance });
            battleCase.Self.Position = TrooperStance.Standing;
            return true;
        }

        private static bool ThrowGrenate<T>(StrategyResult3 sr, BattleCase3<T> battleCase, ref int points) where T : Warrior2
        {
            if (!battleCase.Self.Warrior.HasGrenade) return sr.SetImpossible("Unit does not have grenade!");
            if (!battleCase.Self.Warrior.Can(ActionType.ThrowGrenade, 1, points)) return sr.SetImpossible("Not enought points to throw grenade");

            var reason = "";
            var done = false;
            foreach (var enemy in battleCase.Enemies)
            {
                Point target = enemy.Location.Point;

                var grenadeRadis = Tool.GetRadius(battleCase.Self.Warrior.GrenadeRange);
                var dxes = new int[] { 0, 1, 0, -1, 0 };
                var dyes = new int[] { 0, 0, 1, 0, -1 };
                int distance = -1;
                for (var i = 0; i < dxes.Length; i++)
                {
                    target.X = enemy.Location.X + dxes[i];
                    target.Y = enemy.Location.Y + dyes[i];
                    int dx = Math.Abs(target.X - battleCase.Self.Location.X), dy = Math.Abs(target.Y - battleCase.Self.Location.Y);
                    if (grenadeRadis.Contains(dx, dy))
                    {
                        distance = i == 0 ? 0 : 1;
                        break;
                    }
                }
                if (distance == -1)
                {
                    reason += "\n" + String.Format("It is not possible to throw grenade from {0},{1} to {2},{3}", battleCase.Self.Location.X, battleCase.Self.Location.Y, enemy.Location.X, enemy.Location.Y);
                    continue;
                }

                int damage = battleCase.Self.Warrior.GetGrenadeDamage(distance);
                enemy.Damage += damage;

                if (Math.Abs(battleCase.Self.Location.X - target.X) + Math.Abs(battleCase.Self.Location.Y - target.Y) <= 1)
                {
                    reason += "\n" + "I do not want to throu grenade to myself...";
                    continue;
                }
                battleCase.Allies.Where(a => Math.Abs(a.Location.X - target.X) + Math.Abs(a.Location.Y - target.Y) <= 1).ToList().ForEach(a =>
                {
                    var adamage = battleCase.Self.Warrior.GetGrenadeDamage(Math.Abs(a.Location.X - target.X) + Math.Abs(a.Location.Y - target.Y));
                    a.Damage += adamage;
                });

                battleCase.Self.Warrior.DoIfCan(ActionType.ThrowGrenade, ref points);
                sr.Moves.Add(new Move { Action = ActionType.ThrowGrenade, X = target.X, Y = target.Y });
                done = true;
                break;      //TODO: select enemy for more damage
            }
            if (!done) return sr.SetImpossible(reason);
            

            return true;
        }

        private static bool Shoot<T>(StrategyResult3 sr, BattleWarrior3<T> self, BattleCase3<T> battleCase, ref int points) where T : Warrior2
        {
            var location = self.Location;
            var currentPosition = self.Position;
            //TODO: do not forget to recalculate after move
            var enemy = battleCase.Enemies.Where(e => e.Alive && self.CanAttack(e)).OrderBy(e => e.Hitpoints).FirstOrDefault();
            if (enemy == null || !self.CanAttack(enemy)) return sr.SetImpossible(String.Format("Cannot shoot from poition {0}", currentPosition));
            if (!location.CanAttackFromHere) return sr.SetImpossible(String.Format("We cannot attack from {0},{1} enemy at {2},{3}", location.X, location.Y, enemy.Location.X, enemy.Location.Y));
            if (self.Warrior.DoIfCan(ActionType.Shoot, ref points))
            {
                sr.Moves.Add(new Move().Shoot(enemy.Location.Point));
                var damage = self.Warrior.GetDamage(currentPosition);
                enemy.Damage += damage;
                if (!enemy.Alive) return false;
                return true;
            }
            return false;
        }

        private static bool Move<T>(StrategyResult3 sr, List<PossibleMove> way, int distance, BattleCase3<T> battleCase, ref int points, ref bool hasRation) where T : Warrior2
        {
            var location = battleCase.Self.Location;
            var position = battleCase.Self.Position;
            if (!CheckStanceBeforeMoving(sr, battleCase, ref points)) return false;
            var oldLoc = location;
            var point = oldLoc;
            if (distance > 0)
            {
                if (way.Count() - 1 < distance) return sr.SetImpossible("There is no appropriate way");
                for (var wayIdx = 1; wayIdx <= distance; wayIdx++)
                {
                    ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                    if (!battleCase.Self.Warrior.DoIfCan(ActionType.Move, 1, ref points)) return sr.SetImpossible(String.Format("Not enought action points ({0}) to perform {1} steps", points, distance));
                    if (point.DistanceTo(way[wayIdx]) != 1)
                    {
                      var a = 5;
                    }
                    sr.Moves.Add(new Move().Move(point.DirectionTo(way[wayIdx])));
                    point = way[wayIdx];
                }
            }
            else
            {
                var currentPoint = way.Select((p, i) => new { Point = p, Index = i }).Where(pi => pi.Point.Point.Equals(point.Point)).Select(pi => pi.Index).First();
                for (var wayIdx = currentPoint - 1; wayIdx >= currentPoint + distance; wayIdx--)
                {
                    ExtraRation<T>(sr, battleCase, ref points, ref hasRation);
                    if (!battleCase.Self.Warrior.DoIfCan(ActionType.Move, 1, ref points)) return sr.SetImpossible(String.Format("Not enought action points ({0}) to perform {1} steps", points, distance));
                    sr.Moves.Add(new Move().Move(point.DirectionTo(way[wayIdx])));
                    point = way[wayIdx];

                }
            }
            if (battleCase.Self.Warrior.IsMedic && battleCase.SickAllies.Any(s => s.Location.DistanceTo(oldLoc) > s.Location.DistanceTo(point)))
            {
                sr.ChangeThisTurn.PotentiallyHeal++;
            }
            battleCase.Self.Location = point;
            return true;

        }

    }
}
