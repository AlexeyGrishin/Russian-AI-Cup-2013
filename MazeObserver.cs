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
        int Width { get; }
        int Height { get; }

        int DangerIndex(int x, int y, TrooperStance stance = TrooperStance.Standing);
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


}
