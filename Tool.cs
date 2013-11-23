using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    public static class Tool
    {
        public static Direction GetDirection(Unit u1, Unit target)
        {
            return GetDirection(u1.X, u1.Y, target.X, target.Y);
        }

        public static Direction GetDirection(Point p1, Point p2)
        {
            return GetDirection(p1.X, p1.Y, p2.X, p2.Y);
        }


        public static Direction GetDirection(int x1, int y1, int x2, int y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            if (dx == 0 && dy == 0) return Direction.CurrentPoint;
            if (Math.Abs(dy) > Math.Abs(dx))
            {
                if (dy > 0) return Direction.South; else return Direction.North;
            }
            else
            {
                return dx > 0 ? Direction.East : Direction.West;
            }
        }

        public static readonly Direction[] AllDirections = new Direction[] { Direction.West, Direction.North, Direction.East, Direction.South };
        public static readonly Direction[] OppDirections = new Direction[] { Direction.CurrentPoint, Direction.South, Direction.West, Direction.North, Direction.East};

        public static Direction GoLeft(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return Direction.West;
                case Direction.West: return Direction.South;
                case Direction.South: return Direction.East;
                case Direction.East: return Direction.North;
                default: return direction;
            }
        }

        public static Direction GoRight(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return Direction.East;
                case Direction.West: return Direction.North;
                case Direction.South: return Direction.West;
                case Direction.East: return Direction.South;
                default: return direction;
            }
        }

        public static Direction Opposite(Direction direction)
        {
            return OppDirections[(int)direction];
        }

        public static bool IsOpposite(Direction direction1, Direction direction2)
        {
            return OppDirections[(int)direction1] == direction2;
        }

        public class Radius
        {
            private int radius;
            private int[] limits;

            public Radius(int radius)
            {
                this.radius = radius;
                this.limits = new int[radius + 1];
                Calculate();
            }

            private void Calculate()
            {
                int dx = 0, dy = radius;
                while (dx < radius)
                {
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    while (distance > radius)
                    {
                        dy--;
                        distance = Math.Sqrt(dx * dx + dy * dy);
                    }
                    limits[dx] = dy;
                    dx++;
                }
            }

            private int limit(int delta)
            {
                return delta >= limits.Length ? -300 : limits[delta];
            }

            public bool Contains(int dx, int dy)
            {
                return dy <= limit(dx) && dx <= limit(dy);
            }

            public int StepsToGoOutside(int dx, int dy)
            {
                return Math.Min(limit(dy) - dx, limit(dx) - dy) + 1;
            }

            public int StepsToGoInside(int dx, int dy)
            {
                return Math.Min(dx - limit(dy), dy - limit(dx));
            }

        }

        private static IDictionary<int, Radius> radiuses = new Dictionary<int, Radius>();

        public static Radius GetRadius(int radius)
        {
            if (!radiuses.ContainsKey(radius)) radiuses[radius] = new Radius(radius);
            return radiuses[radius];
        }

    }
}
