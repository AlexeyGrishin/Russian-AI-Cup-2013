using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze
{
    public interface Moveable
    {
        int WayIndex { get; set; }
        bool OnWay { get; set; }
        int TurnOrder { get; set; }

        int X { get;  }
        int Y { get;  }
        void DoMove(Direction direction, int wayIdx);
        void Wait(string reason = null);

    }
    public class Group2
    {
        private IList<Moveable> moveable;
        private IList<Moveable> sorted;
        private RHWay way;

        public Group2(IList<Moveable> team)
        {
            this.moveable = team;
            sorted = null;
        }

        public bool Alone()
        {
            return moveable.Count == 0;
        }

        //0 = last one, count-1 = first one
        private IList<Moveable> Sorted
        {
            get
            {
                if (sorted == null)
                {
                    sorted = way.Sort(moveable, m => m.WayIndex);
                }
                return sorted;
            }
        }

        public void CheckNotOnWay(RHWay way)
        {
            foreach (var trooper in moveable)
            {
                if (!trooper.OnWay)
                {
                    trooper.WayIndex = way.GetIndex(Point.Get(trooper.X, trooper.Y));
                    trooper.OnWay = trooper.WayIndex != -1;
                }
            }
        }

        public Moveable NextAfter(RHWay way, Moveable trooper)
        {
            this.way = way;
            var nextIdx = Sorted.IndexOf(trooper) - 1;
            return nextIdx < 0 ? null : Sorted[nextIdx];
        }

        public Moveable PrevBefore(RHWay way, Moveable trooper)
        {
            this.way = way;
            var prevIdx = Sorted.IndexOf(trooper) + 1;
            return prevIdx >= Sorted.Count ? null : Sorted[prevIdx];
        }

        public Moveable Last
        {
            get
            {
                return moveable.Last();
            }
        }


        public bool OnWay {
            get
            {
                return moveable.All(m => m.OnWay);
            }
        }

    }
}
