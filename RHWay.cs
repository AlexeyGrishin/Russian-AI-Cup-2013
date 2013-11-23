using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Maze
{
    public class RHWay
    {
        public class DirectedCell
        {
            public DirectedCell(Point apoint, Direction adirection)
            {
                Point = apoint;
                Direction = adirection;
            }
            public DirectedCell(Point apoint)
            {
                Point = apoint;
            }

            public Direction Direction {get;internal set;}
            public Point Point { get; internal set; }
            public int NextIndex { get; internal set; }
            public Direction BackDirection { get; set; }
            public int BackIndex { get; internal set; }

            public override int GetHashCode()
            {
                return Point.GetHashCode() << 2 + Direction.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                DirectedCell dc = obj as DirectedCell;
                if (dc == null) return false;
                return dc.Point.Equals(Point) && dc.Direction.Equals(Direction);
 	            
            }

            public override string ToString()
            {
                return String.Format("{0} {1} --> {2}", Point, Direction, NextIndex);
            }
        }


        private IList<DirectedCell> way = new List<DirectedCell>();
        private IDictionary<Point, IList<DirectedCell>> byPoint = new Dictionary<Point, IList<DirectedCell>>();

        public DirectedCell[] All { get { return way.ToArray(); } }

        public Point LastPoint { get { return way.Last().Point; } }

        public int GetIndex(Point point)
        {
            return way.IndexOf(GetCellsAt(point).FirstOrDefault());
        }

        public bool Contains(Point point)
        {
            return byPoint.ContainsKey(point);
        }

        public bool OnWay(Point point, Point currentPos)
        {
            return Contains(point) && Direction(GetIndex(currentPos), GetIndex(point)) == 1;
        }

        public IEnumerable<DirectedCell> GetCellsAt(Point point)
        {
            if (byPoint.ContainsKey(point)) return byPoint[point];
            return new List<DirectedCell>();
        }

        public DirectedCell GetCell(int pointIndex)
        {
            return way[pointIndex % way.Count];
        }

        public int Distance(int index1, int index2)
        {
            if (index1 == index2) return 0;
            if (index1 > index2) return Distance(index2, index1);
            return Math.Min(index2 - index1, index1 + way.Count - index2);
        }

        //returns 1 if we can get from index1 to index2 fastest way with index1+1 several times
        //returns -1 if we need to go in reverse way
        public int Direction(int index1, int index2)
        {
            if (index1 == index2) return 0;
            if (index1 > index2) return -Direction(index2, index1);
            var d1 = index2 - index1;
            var d2 = index1 + way.Count - index2;
            if (d1 < d2)
                return 1;
            else
                return -1;
        }

        public int GoTo(int index, int direction)
        {
            return (index % way.Count + (direction > 0 ? 1 : -1)) % way.Count;
        }

        public IList<T> Sort<T>(IList<T> original, Func<T, int> GetWayIndex)
        {
            if (original.Count == 1) return original;
            var allIndexes = original.Select(t => GetWayIndex(t)).ToList();
            allIndexes.Sort();
            while (Direction(allIndexes[0], allIndexes[allIndexes.Count-1]) == -1)
            {
                allIndexes.Add(allIndexes[0]);
                allIndexes.RemoveAt(0);
            }
            return original.OrderBy(t => allIndexes.IndexOf(GetWayIndex(t))).ToList();

        }



        public bool Add(Direction direction)
        {
            var prevPoint = way.LastOrDefault();
            var newPoint = prevPoint.Point.To(direction);
            prevPoint.Direction = direction;
            return AddTo(newPoint, prevPoint);
        }

        public bool Add(Point point)
        {
            var prevPoint = way.LastOrDefault();
            if (prevPoint != null)
            {
                prevPoint.Direction = prevPoint.Point.To(point);
                return AddTo(point, prevPoint);
            }
            way.Add(new DirectedCell(point));
            return false;
        }

        public void AddFirstPoint(Point point)
        {
            if (way.Count == 0) Add(point);
        }

        private bool DetectDeadends(DirectedCell prevPoint, Point nextPoint)
        {
            var pointPrevToPrev = way[way.Count - 2];
            if (pointPrevToPrev.Point.Equals(nextPoint) && Tool.IsOpposite(prevPoint.Direction, pointPrevToPrev.Direction))
            {
                //here we go!
                way.RemoveAt(way.Count - 1);
                return true;
            }
            return false;
        }

        private bool AddTo(Point point, DirectedCell prevPoint)
        {
            if (way.Count >= 2)
            {
                if (DetectDeadends(prevPoint, point))
                {
                    return false;
                }
                int index = way.IndexOf(prevPoint);
                if (index != way.Count - 1)
                {
                    //we have a loop. remove last point
                    way.RemoveAt(way.Count - 1);
                    way.Last().NextIndex = 0;
                    way.First().BackIndex = way.Count - 1;
                    way.First().BackDirection = way.First().Point.To(way.Last().Point);
                    FillDictionary();
                    return true;
                }
            }
            prevPoint.NextIndex = way.Count;
            var dc = new DirectedCell(point);
            way.Add(dc);
            dc.BackIndex = prevPoint.NextIndex - 1;
            dc.BackDirection = point.To(prevPoint.Point);
            return false;
        }

        private void FillDictionary()
        {
            foreach (var dc in way)
            {
                if (!byPoint.ContainsKey(dc.Point))
                    byPoint[dc.Point] = new List<DirectedCell>();
                byPoint[dc.Point].Add(dc);
            }
        }
    }
}
