using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    public interface IMovingStrategy
    {
        void DoMove(Trooper self, World world, Move move);

        void OnStop(World world);
    }

    public class RG: IMovingStrategy
    {
        private bool walking = false;
        private GroupObserver2 observer = new GroupObserver2(MyStrategy.MinDistance, MyStrategy.MaxDistance, MyStrategy.BackDistance);
        public void OnStop(World world)
        {
            world.Troopers.Where(t => t.IsTeammate).Select(t => t.Ext()).ToList().ForEach(t => t.OnWay = false);
        }
        public void DoMove(Trooper self, World world, Move move)
        {
            IEnumerable<Moveable> alltogether = world.Troopers.Where(t => t.IsTeammate).Select(t => t.Ext());
            var selfExt = self.Ext();
            selfExt.Move = move;
            observer.OnTurn(selfExt, world.ToMaze());
            observer.SuggestMove(selfExt, new Group2(alltogether.ToList()), world.ToMaze());
            //TODO: check move possibility
            //TODO: check still on way
        }

        public static RG Instance = new RG();
    }
}
