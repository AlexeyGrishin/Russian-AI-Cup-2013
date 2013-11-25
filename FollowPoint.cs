using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public class FollowPoint
    {
        private int checkPointIdx;
        private int backDistance;
        private WalkableMap map;
        private int closeDistance;

        public FollowPoint(Warrior2 self, int backDistance, WalkableMap walkableMap, int closeDistance = 0)
        {
            this.map = walkableMap;
            this.backDistance = backDistance;
            this.closeDistance = closeDistance;
            var corners = new Point[] { Point.Get(0, 0), Point.Get(map.Width - 1, 0), Point.Get(0, map.Height - 1), Point.Get(map.Width - 1, map.Height - 1) };
            var center = Point.Get(map.Width >> 1, map.Height >> 1);
            var nearestCorner = corners.Select(c => new { Point = c, Distance = Tool.GetDistance(self.Location, c) }).OrderBy(d => d.Distance).First().Point;
            var oppositeCorner = corners.Select(c => new { Point = c, Distance = Tool.GetDistance(self.Location, c) }).OrderBy(d => d.Distance).Last().Point;
            checkPoints = new List<Point>(5);
            checkPoints.Add(nearestCorner);
            checkPoints.Add(oppositeCorner);
            checkPoints.Add(center);
            checkPoints.AddRange(corners.Where(c => c != nearestCorner && c != oppositeCorner));
        }

        private List<Point> checkPoints;

        private Point CheckPoint { get { return checkPoints[checkPointIdx]; } }

        private void GoNext()
        {
            checkPointIdx = (checkPointIdx + 1) % checkPoints.Count;
        }

        private void GoPrev()
        {
            checkPointIdx = (checkPointIdx - 1 + checkPoints.Count) % checkPoints.Count;
        }

        private TrooperType eyesType;
        private int maxVis, globalMaxVis = -1;

        private void Analyze(IEnumerable<Warrior2> troopers)
        {
            maxVis = (int)troopers.Select(t => t.VisionRange).Max();
            if (globalMaxVis == -1) globalMaxVis = maxVis;
            eyesType = troopers.OrderBy(t => t.Type).First(t => t.VisionRange == maxVis).Type;
        }

        private bool IsEyes(Warrior2 trooper)
        {
            return trooper.Type == eyesType;
        }

        private bool eyesStuck = false;

        public void SuggestMove(Warrior2 self, IEnumerable<Warrior2> all, Move move, bool secondAttempt = false)
        {
            Analyze(all);
            List<PossibleMove> way = null;
            if (IsEyes(self))
            {
                var distanceToTarget = Tool.GetDistance(self.Location, CheckPoint);
                if (distanceToTarget <= closeDistance)
                {
                    Console.WriteLine("We are in " + distanceToTarget + " steps from target, go next");
                    GoNext();
                }
                var stepsLeft = self.Actions / self.Cost(ActionType.Move);
                var possibleEnemyAt = (int)(1 + globalMaxVis - self.VisionRange);
                if (stepsLeft <= possibleEnemyAt)
                {
                    Console.WriteLine("There could be enemy in " + possibleEnemyAt + "steps, so I stop");
                    move.Wait();
                    return;
                }
                var distance = all.Select(t => Tool.GetDistance(t, self)).Max();
                if (distance >= backDistance)
                {
                    var lostOne = all.First(t => Tool.GetDistance(t, self) == distance);
                    way = map.FindWay(self.Location, lostOne.Location).ToList();
                    Console.WriteLine("Eyes[" + self.Type + "] goes to lost one");
                }
                else
                {
                    way = map.FindWay(self.Location, CheckPoint).ToList();
                    Console.WriteLine("Eyes[" + self.Type + "] goes to checkpoint");
                }
            }
            else
            {
                if (eyesStuck)
                {
                    way = map.FindWay(self.Location, CheckPoint).ToList();
                    Console.WriteLine("Unit[" + self.Type + "] goes to checkpoint because eyes are blocked");

                }
                else
                {
                    way = map.FindWay(self.Location, all.First(t => IsEyes(t)).Location).ToList();
                    Console.WriteLine("Unit[" + self.Type + "] follows");
                }
            }
            if (way.Count() > 1)
            {
                if (IsEyes(self)) eyesStuck = false;
                var dir = self.Location.DirectionTo(way[1]);
                move.Move(dir);
            }
            else
            {
                if (IsEyes(self))
                {
                    if (secondAttempt)
                    {
                        //we are stuck by our friends...
                        eyesStuck = true;
                        GoPrev();
                        return;
                    }
                    GoNext();
                    SuggestMove(self, all, move, true);
                    return;
                }
                move.Wait();
            }
        }
    }
}
