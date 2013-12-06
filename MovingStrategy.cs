using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    //ага, интерфейсы, стратегии - размечтался
    public interface IMovingStrategy
    {
        void DoMove(Trooper self, World world, Move move);

        void OnStop(World world);
    }

    public class FollowToPoints: IMovingStrategy
    {
        private FollowPoint follow = null;

        public void DoMove(Trooper self, World world, Move move)
        {
            if (follow == null)
            {
                follow = new FollowPoint(self.Ext(), MyStrategy.BackDistance, WalkableMap.Instance(), MyStrategy.CloseDistance);
            }
            follow.SuggestMove(self.Ext(), world.Troopers.Where(t => t.IsTeammate).Select(t => t.Ext()), move);
        }

        public void OnStop(World world)
        {
            //nothing
        }

        public static FollowToPoints Instance = new FollowToPoints();
    }


}
