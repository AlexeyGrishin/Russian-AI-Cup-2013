using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze
{
    public class RHWayFinder
    {

        private IList<RHWay> ways = new List<RHWay>();
        private int steps;

        public RHWayFinder(int steps = 20)
        {
            this.steps = steps;
        }

        public IEnumerable<Direction> HowToGoToWay(int number, IMaze maze, int x, int y)
        {
            var MA = WalkableMap.Instance();
            var way = GetWay(number);
            var path = MA.FindWay(MA.Get(x, y), p => way.Contains(p.Point)).ToList();
            var answer = new List<Direction>();
            for (var i = 1; i < path.Count(); i++ )
            {
                answer.Add(path[i-1].DirectionTo(path[i]));
            }
            /*
                while (way.GetIndex(point) == -1)
                {
                    var dir = Direction.East;
                    var newPoint = point.To(dir);
                    if (!maze.HasNotWallOrUnit(newPoint.X, newPoint.Y))
                    {
                        dir = Direction.North;
                        newPoint = point.To(dir);
                        if (!maze.HasNotWallOrUnit(newPoint.X, newPoint.Y))
                        {
                            dir = Direction.South;
                            newPoint = point.To(dir);
                            if (!maze.HasNotWallOrUnit(newPoint.X, newPoint.Y))
                            {
                                //well, do not know what to do in this case at v 1.
                                break;
                            }

                        }
                    }
                    answer.Add(dir);
                    point = newPoint;
                }*/
            return answer;
        }

        public RHWay DefineWay(int number, IMaze maze, int x, int y)
        {
            return GetWay(number) ?? SaveWay(number, maze, x, y);
        }

        private RHWay SaveWay(int number, IMaze maze, int x, int y)
        {
            RHWay way = new RHWay();
            var observer = new RightHandObserver();

            observer.NextSteps(maze, Point.Get(x, y), (point, direction, isOnWay) => {
                if (isOnWay)
                {
                    way.AddFirstPoint(point);
                    return way.Add(direction);
                }
                return false;
            });

            if (ways.Count == number)
                ways.Add(way);
            else
                ways[number] = way;
            return way;
        }

        public RHWay GetWay(int number)
        {
            return number < ways.Count ? ways[number] : null;
        }

        public static RHWayFinder Instance()
        {
            return instance ?? (instance = new RHWayFinder());
        }

        private static RHWayFinder instance = null;

        
    }
}
