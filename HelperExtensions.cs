using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI
{
    public static class HelperExtensions
    {
        public static Move Move(this Move move, Trooper self, int x, int y)
        {
            move.Action = ActionType.Move;
            move.Direction = Tool.GetDirection(self.X, self.Y, x, y);
            return move;
        }

        public static Move Move(this Move move, Direction direction)
        {
            move.Action = ActionType.Move;
            move.Direction = direction;
            return move;
        }

        public static Move Wait(this Move move)
        {
            move.Action = ActionType.EndTurn;
            move.Direction = Direction.CurrentPoint;
            return move;    
        }

        public static Move Shoot(this Move move, Trooper target)
        {
            move.Action = ActionType.Shoot;
            move.X = target.X;
            move.Y = target.Y;
            return move;
        }

        public static Move Shoot(this Move move, Point target)
        {
            move.Action = ActionType.Shoot;
            move.X = target.X;
            move.Y = target.Y;
            return move;
        }

        public static void Apply(this Move move, Move anotherMove)
        {
            anotherMove.Action = move.Action;
            anotherMove.Direction = move.Direction;
            anotherMove.X = move.X;
            anotherMove.Y = move.Y;
        }

        public static Move Heal(this Move move, Trooper self, Trooper target)
        {
            move.Action = ActionType.Heal;
            move.Direction = Tool.GetDirection(self, target);
            return move;
        }

        public static bool IsMade(this Move move)
        {
            return move.Action != ActionType.EndTurn;
        }

        private static WorldMaze worldMaze = null;

        public static WorldMaze ToMaze(this World world)
        {
            var mz = worldMaze ?? (worldMaze = new WorldMaze(world));
            mz.world = world;
            return mz;
        }

        public static Point GetPosition(this Trooper unit)
        {
            return new Point { X = unit.X, Y = unit.Y };
        }

        public static bool IsFirstTurn(this Trooper trooper)
        {
            return trooper.Type == TrooperType.Scout ? trooper.ActionPoints == 12 : trooper.ActionPoints >= 10;
        }

        private static IDictionary<long, TrooperExt> exts = new Dictionary<long, TrooperExt>();
        private static IDictionary<TrooperType, int> order = new Dictionary<TrooperType, int>();
        private static int nextOrder = 0;
        public static void Update(this Trooper trooper, Game game)
        {
            Ext(trooper).Game = game;
        }
        public static TrooperExt Ext(this Trooper trooper)
        {
            if (exts.ContainsKey(trooper.Id))
            {
                exts[trooper.Id].orig = trooper;
                return exts[trooper.Id];
            }
            else 
            {
                var te = new TrooperExt(trooper);
                if (trooper.IsTeammate)
                {
                    te.TurnOrder = nextOrder;
                    order[trooper.Type] = nextOrder;
                    nextOrder++;
                }
                exts[trooper.Id] = te;
                return te;
            }
        }

        public static int GetMaxSteps(this Trooper trooper)
        {
            return trooper.ActionPoints / 2;    //TODO: get from Game
        }

        public class WorldMaze: IMaze, IWalkingMaze, IWarriorMaze<TrooperExt>
        {
            public World world;
            public WorldMaze(World world)
            {
                this.world = world;
            }

            public bool IsFree(int x, int y)
            {
                if (x < 0 || y < 0 || x >= world.Width || y >= world.Height) return false;
                return world.Cells[x][y] == CellType.Free;
            }

            public bool HasNotWallOrUnit(int x, int y)
            {
                return IsFree(x, y) && !world.Troopers.Any(t => t.X == x && t.Y == y);
            }

            public int Width
            {
                get { return world.Width; }
            }

            public int Height
            {
                get { return world.Height; }
            }

            public bool CanAttack(TrooperExt attacker, int xFrom, int yFrom, TrooperExt attackWho, int xTo, int yTo)
            {
                return world.IsVisible(100, xFrom, yFrom, attacker.orig.Stance, xTo, yTo, attackWho.orig.Stance);
            }
        }

        public static string AsString(this Move move)
        {
            string res = move.Action.ToString();
            if (move.Action == ActionType.Move)
            {
                res += " " + move.Direction;
            }
            else if (move.Action == ActionType.Shoot || move.Action == ActionType.ThrowGrenade)
            {
                res += " " + move.X + "," + move.Y;
            }
            return res;
        }
    }
}
