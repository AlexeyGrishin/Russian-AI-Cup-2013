﻿using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
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
            var otherCorners = corners.Where(c => c != nearestCorner && c != oppositeCorner);
            var oneOfCorners = otherCorners.OrderBy(c => walkableMap.FindWay(self.Location, c).Count()).First();
            var wayToIt = walkableMap.FindWay(self.Location, oneOfCorners);
            var middleOfWall = wayToIt.ElementAt(wayToIt.Count() / 2);
            var middleWayToCenter = Point.Get((middleOfWall.X + center.X)/2, (middleOfWall.Y + center.Y)/2);
            checkPoints = new List<Point>(5);
            checkPoints.Add(nearestCorner);
            checkPoints.Add(middleOfWall.Point);
            checkPoints.Add(center);
            checkPoints.Add(oneOfCorners);
            checkPoints.Add(oppositeCorner);
            checkPoints.Add(center);
            checkPoints.AddRange(otherCorners.Where(c => c != oneOfCorners));
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
        private Point nextEyesPoint = Point.Get(-1, -1);

        private void SuggestMoveForEyes(Warrior2 self, IEnumerable<Warrior2> all, Move move, bool secondAttempt = false)
        {
            List<PossibleMove> way = null;
            var stepsLeft = self.Actions / self.Cost(ActionType.Move);
            var distanceToTarget = Tool.GetDistance(self.Location, CheckPoint);
            if (distanceToTarget <= closeDistance)
            {
                Console.WriteLine("We are in " + distanceToTarget + " steps from target, go next");//[DEBUG]
                GoNext();
            }
            var possibleEnemyAt = (int)(1 + globalMaxVis - self.VisionRange);
            stepsLeft -= possibleEnemyAt;
            if (stepsLeft <= 0)
            {
                Console.WriteLine("There could be enemy in " + possibleEnemyAt + "steps, so I stop");//[DEBUG]
                move.Wait();
                return;
            }
            var distance = all.Select(t => Tool.GetDistance(t, self)).Max();
            if (distance >= backDistance)
            {
                var lostOne = all.First(t => Tool.GetDistance(t, self) == distance);
                way = map.FindWay(self.Location, lostOne.Location).ToList();
                Console.WriteLine("Eyes[" + self.Type + "] goes to lost one");  //[DEBUG]
            }
            else
            {
                //TODO:!!!!!!!!!!!!
                way = Tool.LessDangerousWay(map.FindWays(self.Location, CheckPoint).ToList(), stepsLeft).ToList();
                Console.WriteLine("Eyes[" + self.Type + "] goes to checkpoint");//[DEBUG]
                Console.WriteLine(String.Join("," ,way.Select(w => "[" + w.X + ", " + w.Y + "]")));
            }

           if (way.Count() > 1)
            {
                if (stepsLeft == 1 && way[1].DangerIndex > self.Location.DangerIndex)
                {
                    Console.WriteLine("Eyes[" + self.Type + "] stops - next point is very dangerous");//[DEBUG]
                    move.Wait();
                    return;
                }
                eyesStuck = false;
                nextEyesPoint = way.Count() > 2 ? way[2].Point : Point.Get(-1, -1);
                var dir = self.Location.DirectionTo(way[1]);
                move.Move(dir);
            }
            else
            {
                if (secondAttempt)
                {
                    //we are stuck by our friends...
                    eyesStuck = true;
                    GoPrev();
                    return;
                }
                GoNext();
                SuggestMoveForEyes(self, all, move, true);
            }

        }

        private void SuggestMoveForFollower(Warrior2 self, IEnumerable<Warrior2> all, Move move)
        {
            List<PossibleMove> way = null;
            if (eyesStuck)
            {
                way = map.FindWay(self.Location, CheckPoint).ToList();
                Console.WriteLine("Unit[" + self.Type + "] goes to checkpoint because eyes are blocked");//[DEBUG]

            }
            else
            {
                var eyes = all.First(IsEyes);
                var myDistanceToEyes = Tool.GetDistance(eyes, self);
                var allWays = all.Select(a => new { Ally = a, DistanceToEyes = Tool.GetDistance(eyes, a), Path = map.FindWay(self.Location, a.Location, MyStrategy.TeamCloseDistance) })
                    .ToList()
                    .Where(a => a.Path.Count() > 1 && a.DistanceToEyes < myDistanceToEyes).OrderBy(a => a.DistanceToEyes);
                var shortWays = allWays.Where(a => a.Path.Count() < 6);

                var away = shortWays.FirstOrDefault() ?? allWays.FirstOrDefault();
                if (away != null)
                {
                    Console.WriteLine("Unit[" + self.Type + "] follows " + away.Ally.Type);//[DEBUG]
                    way = away.Path.ToList();
                }
                else
                {
                    way = new List<PossibleMove>();
                }
            }
            if (way.Count() > 1)
            {
                if (nextEyesPoint.Equals(way[1].Point))
                {
                    Console.WriteLine("Unit[" + self.Type + "] does not want to step before eyes");
                    move.Wait();
                    return;
                }
                var dir = self.Location.DirectionTo(way[1]);
                move.Move(dir);
            }
            else
            {
                move.Wait();
            }
        }

        public void SuggestMove(Warrior2 self, IEnumerable<Warrior2> all, Move move)
        {
            Analyze(all);
            if (IsEyes(self))
            {
                SuggestMoveForEyes(self, all, move);
            }
            else {
                SuggestMoveForFollower(self, all, move);
            }
        }
    }
}
