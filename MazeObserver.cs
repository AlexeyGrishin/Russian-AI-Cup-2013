using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    public interface IMaze
    {
        bool IsFree(int x, int y);
        bool HasNotWallOrUnit(int x, int y);
    }

    //TODO: points cache
    public class Point
    {
        public int X {get;set;}
        public int Y {get;set;}

        public static Point Get(int x, int y)
        {
            return new Point { X = x, Y = y };
        }

        public Point To(Direction direction)
        {
            switch (direction)
            {
                case Direction.West:  return new Point { X = X - 1, Y = Y };
                case Direction.East:  return new Point { X = X + 1, Y = Y };
                case Direction.North: return new Point { X = X, Y = Y - 1 };
                case Direction.South: return new Point { X = X, Y = Y + 1 };
                default: return this;
            }
        }

        public override String ToString()
        {
            return X + "," + Y;
        }

        public override bool Equals(object o)
        {
            if (o.GetType() == typeof(Point))
            {
                var p = (Point)o;
                return X == p.X && Y == p.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public Direction To(Point point)
        {
            return Tool.GetDirection(X, Y, point.X, point.Y);
        }

        public Point To(Direction? direction)
        {
            return To(direction ?? Direction.CurrentPoint);
        }
    }

    public interface IMazeObserver
    {
        IList<Direction> NextSteps(IMaze maze, Point currentPosition, int countSteps);
    }

    //TODO: on start find all ways/circles, enumerate them. 
    public class RightHandObserver: IMazeObserver
    {
        public IList<Direction> NextSteps(IMaze maze, Point currentPosition, int countSteps)
        {
            var directions = new List<Direction>();
            NextSteps(maze, currentPosition, (_ignore1, direction, _ignore) =>
            {
                directions.Add(direction);
                return directions.Count == countSteps;
            });
            return directions;
        }

        public void NextSteps(IMaze maze, Point currentPosition, Func<Point, Direction, bool, bool> process)
        {
            var calculatedPosition = currentPosition;
            var forwardDirection = Direction.CurrentPoint;
            var hasWallAtRight = false;
            var iterations = 2000;
            while (iterations-->0)
            {
                forwardDirection = NextStep(maze, calculatedPosition, forwardDirection, ref hasWallAtRight);
                var res = process(calculatedPosition, forwardDirection, hasWallAtRight);
                if (res || forwardDirection == Direction.CurrentPoint) break;
                calculatedPosition = calculatedPosition.To(forwardDirection);
            }
            //error...
        }

        private Direction NextStep(IMaze maze, Point currentPosition, Direction forwardDirection, ref bool hasWallAtRight)
        {
            //search for wall behind
            if (!hasWallAtRight)
            {
                //we do not know our direction
                var foundWall = Tool.AllDirections.Select(d => currentPosition.To(d))
                    .Where(p => !maze.IsFree(p.X, p.Y)).FirstOrDefault();
                if (foundWall != null)
                {
                    hasWallAtRight = true;
                    //go "forward", having it on right side
                    forwardDirection = Tool.GoLeft(currentPosition.To(foundWall));
                    return forwardDirection;
                }
                else
                {
                    //go east
                    return Direction.East;
                }
            }

            if (hasWallAtRight) 
            {
                //do we have a wall at the right side?
                var pointAtRight = currentPosition.To(Tool.GoRight(forwardDirection));
                if (maze.IsFree(pointAtRight.X, pointAtRight.Y))
                {
                    return Tool.GoRight(forwardDirection);
                }
                for (int i = 0; i < 4; i++)
                {
                    var pointThere = currentPosition.To(forwardDirection);
                    if (maze.IsFree(pointThere.X, pointThere.Y)) return forwardDirection;
                    forwardDirection = Tool.GoLeft(forwardDirection);
                }
                //well, we are stuck
                forwardDirection = Direction.CurrentPoint;
            }
            return forwardDirection;
        }

    }
    
}
