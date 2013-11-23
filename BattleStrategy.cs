using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{
    public class BattleStrategy
    {
        public void DoMove(Trooper self, World world, Move move)
        {
            if (!self.Ext().HasNextMove())
            {
                var enemies = world.Troopers.Where(t => !t.IsTeammate).OrderBy(e => e.Hitpoints);
                var allies = world.Troopers.Where(t => t.IsTeammate && t.Id != self.Id);
                bool done = false;
                var resolutions = new List<StrategyResult>();
                var casesToLog = new List<Tuple<BattleCase2<TrooperExt>, IList<StrategyResult>>>();  //[DEBUG]
                foreach (var enemy in enemies)
                {
                    var battleCase = new BattleCase2<TrooperExt>(self.Ext(), enemy.Ext(), world.ToMaze(), allies.Select(a => a.Ext()), enemies.Where(e => e != enemy).Select(e => e.Ext()));
                    var steps = Battle.All(battleCase);
                    resolutions.AddRange(steps);
                    casesToLog.Add(new Tuple<BattleCase2<TrooperExt>, IList<StrategyResult>>(battleCase, steps));  //[DEBUG]
                }
                resolutions.Sort();
                resolutions.Reverse();
                var best = resolutions.FirstOrDefault(r => r.Possible);
                foreach (var battleCase in casesToLog)                                              //[DEBUG]
                    Logger.Logger.Instance().LogBattle(battleCase.Item1, battleCase.Item2, resolutions, world, best);       //[DEBUG]
                if (best != null)
                {
                    self.Ext().AddMoves(best.Moves);
                    done = true;
                }
                if (!done)
                {
                    Console.WriteLine("Do not know what to do :(");
                    self.Ext().AddMove(new Move().Wait());
                }
                if (!self.Ext().HasNextMove())
                {
                    self.Ext().AddMove(new Move().Wait());
                }
                
            }
            self.Ext().ExecuteMove(move);
        }

        public void CancelSteps(Trooper self)
        {
            self.Ext().NextMoves.Clear();
        }
    }
}
