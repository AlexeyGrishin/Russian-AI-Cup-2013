using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{

    public interface IMapContext
    {
        PossibleMove this[PossibleMove key] {get;}
        PossibleMove this[Point key] { get; }
    }

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
        public bool CanBeAttackedSomehow { get { return CanBeAttackedOnKneel || CanBeAttackedOnProne || CanBeAttackedOnStand ;} }
        public bool CanHideFromAttackSomehow { get { return !CanBeAttackedOnKneel || !CanBeAttackedOnProne || !CanBeAttackedOnStand; } }
        public bool CanBeAttackedOnStand { get; set; }
        public bool CanBeAttackedOnKneel { get; set; }
        public bool CanBeAttackedOnProne { get; set; }
        public bool CanBeAttacked { get; set; }//TODO: delete

        public int DistanceToEnemy { get; set; }
        public int DistanceToTeam { get; set; }
        public bool CloserToEnemy { get; set; }
        public bool VisibleToEnemy { get; set; }
        public bool VisibleToUs { get; set; }

        public bool FreeSpace { get; set; } //TODO: delete
        /// > 0 - dangerous, =0 - neutral, < 0 - good to stand
        public int DangerIndex { get; set; }
        public int AllWayDangerIndex
        {
            get { return DangerIndex + (Back == null ? 0 : Back.AllWayDangerIndex); }
        }

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

        public IEnumerable<PossibleMove> Where(Func<PossibleMove, bool> func, Func<PossibleMove, bool> andParent)
        {
            return Where(p => func(p) && (p.Back == null || andParent(p.Back)));
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

        public bool SamePosition(PossibleMove unitOldPos)
        {
            return Point.Equals(unitOldPos.Point);
        }
    }

    public class WalkableMap : IMapContext
    {
        private PossibleMove[,] map;

        public WalkableMap(IMaze maze)
        {
            map = new PossibleMove[maze.Width, maze.Height];
            for (var x = 0; x < maze.Width; x++)
            {
                for (var y = 0; y < maze.Height; y++)
                {
                    map[x, y] = maze.IsFree(x, y) ? new PossibleMove { X = x, Y = y, DangerIndex = maze.DangerIndex(x, y) } : null;
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
                    item.CanBeAttackedOnStand = false;
                    item.CanBeAttackedOnKneel = false;
                    item.CanBeAttackedOnProne = false;
                    item.CloserToEnemy = false;
                    item.DistanceToEnemy = 0;
                    item.DistanceToTeam = 0;
                    item.FreeSpace = false;
                    item.VisibleToUs = false;   //TODO: delete
                    item.VisibleToEnemy = false;
                }
            }
        }

        public PossibleMove BuildMapFrom(Point point, int maxDistance, Func<PossibleMove, bool> exclude = null, Func<PossibleMove, int> orderBy = null)
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
                if (orderBy != null) 
                    list = list.OrderBy(orderBy).ToList();
                step++;
            }
            myMove.ForEach(m =>
            {
                Func<PossibleMove, bool> isCorridor = (p) => PointsAround(p).Count <= 2;
                m.FreeSpace = !isCorridor(m) && !PointsAround(m).Any(isCorridor);
            });
            return myMove;
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

        public IEnumerable<PossibleMove> FindWay(PossibleMove from, PossibleMove to, Func<PossibleMove, bool> through)
        {
            var way1 = FindWay(from, through).ToList();
            var lastPoint = way1.LastOrDefault() ?? from;
            var eraseFrom = lastPoint.Step;
            var way2 = FindWay(lastPoint, to, continueWay: true).Skip(eraseFrom);
            if (way1.Count > 0) way1.RemoveAt(way1.Count - 1);
            return way1.Concat(way2);
        }

        public IEnumerable<PossibleMove> FindWay(PossibleMove from, PossibleMove to, int closeDistance = 0, bool continueWay = false)
        {
            return FindWay(from, to.Point, closeDistance, continueWay);
        }
        public IList<IEnumerable<PossibleMove>> FindWays(PossibleMove from, Point to, int closeDistance = 0, bool continueWay = false)
        {
            if (from.Step < 0 || (from.Step != 0 && !continueWay)) throw new Exception("Call BuildMapFrom first for this point");
            var minKnownDistance = Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
            from.ForEach(a =>
            {
                a.DistanceToTarget = Math.Abs(a.X - to.X) + Math.Abs(a.Y - to.Y);
                if (a.DistanceToTarget < minKnownDistance) minKnownDistance = a.DistanceToTarget;
            });
            return from.Where(a => a.DistanceToTarget <= Math.Max(closeDistance, minKnownDistance)).Select(a => a.PathToThis()).ToList();

        }
        public IEnumerable<PossibleMove> FindWay(PossibleMove from, Point to, int closeDistance = 0, bool continueWay = false)
        {
            return FindWays(from, to, closeDistance, continueWay)
                .Select(a => new { Path = a, Distance = a.Last().DistanceToTarget })
                .OrderBy(a => (a.Path.Count() + a.Distance * 2) * 10).Select(a => a.Path).FirstOrDefault();
        }


        public PossibleMove Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1)) return null;
            return map[x, y];
        }

        public PossibleMove this[PossibleMove key] { get { return Get(key.X, key.Y); } }
        public PossibleMove this[Point key] { get { return Get(key.X, key.Y); } }

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

        //Singletone - for walking only

        private static WalkableMap instance;

        public static WalkableMap Instance()
        {
            return instance;
        }
        public static WalkableMap Instance(IMaze maze)
        {
            return instance ?? (instance = new WalkableMap(maze));
        }

        //for tests
        public static WalkableMap Create(IMaze maze)
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
