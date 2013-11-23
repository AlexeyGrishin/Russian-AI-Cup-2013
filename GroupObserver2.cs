using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    public class GroupObserver2
    {

        private RHWay mainWay = null;
        private IMaze maze = null;

        private int minDistance;
        private int maxDistance;
        private int backDistance;

        public GroupObserver2(int min = 2, int max = 10, int? back = null)
        {
            minDistance = min;
            maxDistance = max;
            backDistance = back ?? maxDistance + 1;
        }

        public void OnTurn(Moveable trooper, IMaze maze)
        {
            if (mainWay == null)
            {
                mainWay = RHWayFinder.Instance().DefineWay(0, maze, trooper.X, trooper.Y);
                this.maze = maze;
            }
        }

        public void SuggestMove(Moveable trooper, Group2 group, IMaze maze)
        {
            group.CheckNotOnWay(mainWay);
            var currentIdx = mainWay.GetIndex(Point.Get(trooper.X, trooper.Y));
            if (currentIdx == -1)
            {
                trooper.OnWay = false;
            }
            if (!trooper.OnWay)
            {
                var nextStep = RHWayFinder.Instance().HowToGoToWay(0, maze, trooper.X, trooper.Y).FirstOrDefault();
                if (nextStep == Direction.CurrentPoint)
                {
                    trooper.OnWay = true;
                    trooper.WayIndex = mainWay.GetIndex(Point.Get(trooper.X, trooper.Y));
                }
                else
                {
                    trooper.DoMove(nextStep, -1);
                    return;
                }
            }
            if (group.OnWay)
            {
                var prevOne = group.PrevBefore(mainWay, trooper);
                if (prevOne == null || mainWay.Distance(trooper.WayIndex, prevOne.WayIndex) > minDistance)
                {
                    var nextOne = group.NextAfter(mainWay, trooper);
                    if (nextOne == null || mainWay.Distance(trooper.WayIndex, nextOne.WayIndex) <= maxDistance)
                    {
                        var cell = mainWay.GetCell(trooper.WayIndex);
                        trooper.DoMove(cell.Direction, cell.NextIndex);
                    }
                    else if (nextOne != null && mainWay.Distance(trooper.WayIndex, nextOne.WayIndex) >= backDistance)
                    {
                        Console.WriteLine("Ok, go back");
                        var cell = mainWay.GetCell(trooper.WayIndex);
                        trooper.DoMove(cell.BackDirection, cell.BackIndex);

                    }
                    else
                    {
                        trooper.Wait("too far from previous one");
                        return;
                    }
                }
                else
                {
                    trooper.Wait("Next one is too close");
                    return;
                }
            }
            else
            {
                trooper.Wait("Wait for other group");
            }
                
        }

        private static GroupObserver2 instance;
        public static GroupObserver2 Instance()
        {
            return instance ?? (instance = new GroupObserver2());
        }
    }
}
