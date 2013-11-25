using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{
    public class PossibleMove
    {
        public int Step { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Point Point { get { return Point.Get(X, Y); } }
        public Direction DirectionTo(PossibleMove anotherMove)
        {
            return Tool.GetDirection(X, Y, anotherMove.X, anotherMove.Y);
        }
        public bool CanAttackFromHere { get; set; }
        public bool CanBeAttacked { get; set; }
        public int DistanceToEnemy { get; set; }
        public int DistanceToTeam { get; set; }
        public bool CloserToEnemy { get; set; }

        public bool FreeSpace { get; set; }

        public int DistanceToTarget { get; set; }

        public List<PossibleMove> FurtherMoves { get; set; }
        public PossibleMove Back { get; set; }

        public void ForSelfAndBack(Action<PossibleMove> action)
        {
            action(this);
            if (Back != null) Back.ForSelfAndBack(action);
        }

        public void ForEach(Action<PossibleMove> action)
        {
            action(this);
            foreach (var move in FurtherMoves) move.ForEach(action);
        }

        public IEnumerable<PossibleMove> Where(Func<PossibleMove, bool> func)
        {
            var list = new LinkedList<PossibleMove>();
            ForEach(a => { if (func(a)) list.AddFirst(a); });
            return list;
        }

        public IEnumerable<PossibleMove> WhereLeafs(Func<PossibleMove, bool> func)
        {
            var list = new LinkedList<PossibleMove>();
            ForEach(a => { if (func(a) && a.Where(func).Count() - 1 == 0) list.AddFirst(a); });
            return list;
        }

        public IEnumerable<PossibleMove> PathToThis()
        {
            var list = new LinkedList<PossibleMove>();
            ForSelfAndBack(a => list.AddFirst(a));
            return list;
        }

        public int MaxLevel
        {
            get
            {
                return FurtherMoves.Count == 0 ? Step : FurtherMoves.Select(m => m.MaxLevel).Max();
            }
        }

        public int CloserLevelWhere(Func<PossibleMove, bool> func, bool fromParent = false)
        {
            var myLevel = func(this) ? Step : 553;
            var childrenLevel = FurtherMoves.Count == 0 ? 555 : FurtherMoves.Select(m => m.CloserLevelWhere(func)).Min();
            return Math.Min(myLevel, childrenLevel);
        }
        

        public PossibleMove()
        {
            FurtherMoves = new List<PossibleMove>();
        }

        public override string ToString()
        {
            return String.Format("({0},{1})[{2}]", X, Y, Step);
        }

        public int DistanceTo(PossibleMove a)
        {
            return Math.Abs(X - a.X) + Math.Abs(Y - a.Y);
        }

        public override bool Equals(object obj)
        {
            var pm = obj as PossibleMove;
            return X == pm.X && Y == pm.Y;
        }

        public override int GetHashCode()
        {
            return X + Y;
        }
    }

    public interface IWalkingMaze : IMaze
    {
        int Width { get; }
        int Height { get; }
    }

    public class WalkableMap
    {
        private PossibleMove[,] map;

        public WalkableMap(IWalkingMaze maze)
        {
            map = new PossibleMove[maze.Width, maze.Height];
            for (var x = 0; x < maze.Width; x++)
            {
                for (var y = 0; y < maze.Height; y++)
                {
                    map[x, y] = maze.IsFree(x, y) ? new PossibleMove { X = x, Y = y } : null;
                }
            }
        }

        public void Clear()
        {
            foreach (var item in map)
            {
                if (item != null)
                {
                    item.Step = -1;
                    item.FurtherMoves.Clear();
                    item.Back = null;
                    item.CanAttackFromHere = false;
                    item.CanBeAttacked = false;
                    item.CloserToEnemy = false;
                    item.DistanceToEnemy = 0;
                    item.DistanceToTeam = 0;
                    item.FreeSpace = false;
                }
            }
        }

        public PossibleMove BuildMapFrom(Point point, int maxDistance, Func<PossibleMove, bool> exclude = null)
        {
            Clear();
            int x = point.X, y = point.Y;
            var myMove = Get(x, y);
            IList<PossibleMove> list = new List<PossibleMove> { myMove };
            var step = 0;
            myMove.Step = 0;
            while (step < maxDistance && list.Count > 0)
            {
                list = BuildStep(list, step, exclude);
                step++;
            }
            myMove.ForEach(m =>
            {
                Func<PossibleMove, bool> isCorridor = (p) => PointsAround(p).Count <= 2;
                m.FreeSpace = !isCorridor(m) && !PointsAround(m).Any(isCorridor);
            });
            return myMove;
        }

        public int FindDistance(PossibleMove from, int limit, Func<PossibleMove, bool> criteria)
        {
            IList<PossibleMove> list = new List<PossibleMove> { from };
            IList<PossibleMove> processed = new List<PossibleMove>();
            var step = 0;
            while (step < limit && list.Count > 0)
            {
                var toProcess = new List<PossibleMove>();
                toProcess.AddRange(list);
                list.Clear();
                foreach (var move in toProcess)
                {
                    if (criteria(move)) return step;
                    foreach (var next in PointsAround(move))
                    {
                        if (criteria(next)) return step+1;
                        if (!toProcess.Contains(next) && !processed.Contains(next) && !list.Contains(next))
                        {
                            list.Add(next);
                        }
                    }
                    processed.Add(move);
                }
                
                step++;
            }
            return -1;
            
        }

        public IEnumerable<PossibleMove> FindWay(PossibleMove from, Func<PossibleMove, bool> criteria)
        {
            if (criteria(from)) return new List<PossibleMove>();
            from.ForEach(a =>
            {
                if (criteria(a))
                {
                    a.DistanceToTarget = Math.Abs(a.X - from.X) + Math.Abs(a.Y - from.Y);
                }
                else
                {
                    a.DistanceToTarget = -1;
                }
            });
            var target = from.Where(p => p.DistanceToTarget >= 0).OrderBy(p => p.DistanceToTarget).FirstOrDefault();
            return target == null ? new List<PossibleMove>() : FindWay(from, target);
        }

        public IEnumerable<PossibleMove> FindWay(PossibleMove from, PossibleMove to)
        {
            return FindWay(from, to.Point);
        }
        public IEnumerable<PossibleMove> FindWay(PossibleMove from, Point to)
        {
            if (from.Step != 0) throw new Exception("Call BuildMapFrom first for this point");
            var minKnownDistance = Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
            from.ForEach(a =>
            {
                a.DistanceToTarget = Math.Abs(a.X - to.X) + Math.Abs(a.Y - to.Y);
                if (a.DistanceToTarget < minKnownDistance) minKnownDistance = a.DistanceToTarget;
            });
            return from.Where(a => a.DistanceToTarget == minKnownDistance).Select(a => a.PathToThis()).OrderBy(a => a.Count()).FirstOrDefault().ToList();
        }

        private IList<PossibleMove> BuildStep(IList<PossibleMove> previousSteps, int step, Func<PossibleMove, bool> exclude)
        {
            var newSteps = new List<PossibleMove>();
            foreach (var point in previousSteps)
            {
                foreach (var near in PointsAround(point))
                {
                    if (exclude != null && exclude(near)) continue;
                    if (near.Step == -1)
                    {
                        near.Step = step + 1;
                        point.FurtherMoves.Add(near);
                        near.Back = point;
                        newSteps.Add(near);
                    }
                }
            }
            return newSteps;
        }

        public PossibleMove Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1)) return null;
            return map[x, y];
        }

        public int Width { get { return map.GetLength(0); } }
        public int Height { get { return map.GetLength(1); } }

        private List<PossibleMove> PointsAround(PossibleMove move)
        {
            int[] dxes = new int[] { -1, 0, 1, 0 };
            int[] dyes = new int[] { 0, -1, 0, 1 };
            var res = new List<PossibleMove>(4);
            for (int i = 0; i < 4; i++)
            {
                var p = Get(move.X + dxes[i], move.Y + dyes[i]);
                if (p != null) res.Add(p);
            }
            return res;

        }


        private static WalkableMap instance;

        public static WalkableMap Instance()
        {
            return instance;
        }
        public static WalkableMap Instance(IWalkingMaze maze)
        {
            return instance ?? (instance = new WalkableMap(maze));
        }

        //for tests
        public static WalkableMap Create(IWalkingMaze maze)
        {
            return (instance = new WalkableMap(maze));
        }

        public PossibleMove[][] ToArray()
        {
            var ar = new PossibleMove[map.GetLength(0)][];
            for (var i = 0; i < ar.Length; i++)
            {
                ar[i] = new PossibleMove[map.GetLength(1)];
                for (var j = 0; j < ar[i].Length; j++)
                {
                    ar[i][j] = map[i, j];
                }
            }
            return ar;
        }

        public PossibleMove Get(Point point)
        {
            return Get(point.X, point.Y);
        }
    }
}
