using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.AI
{
    public class Positioned2Mock : Positioned2
    {
        public PossibleMove Location { get; set; }
        public int AttackRange { get; set; }



        public bool IsSick
        {
            get { return false; }
        }
    }

    public class MockTool
    {
        public static Point FindChar(string[] data, char c)
        {
            for (int y = 0; y < data.Length; y++)
            {
                var x = data[y].IndexOf(c);
                if (x > -1) return new Point { X = x, Y = y };
            }
            return null;

        }
    }

    public class MockMaze<T> : IMaze, IWalkingMaze, IWarriorMaze<T> where T: Positioned2
    {
        private string[] text;
        public MockMaze(String[] text)
        {
            this.text = text;
        }

        public bool IsFree(int x, int y)
        {
            if (y < 0 || x < 0) return false;
            if (y >= text.Length) return false;
            var str = text[y];
            if (x >= str.Length) return false;
            return str[x] != 'x';
        }

        public bool HasNotWallOrUnit(int x, int y)
        {
            return IsFree(x, y) && text[y][x] == ' ';
        }

        public Point Point
        {
            get
            {
                return FindChar('!');
            }
        }

        public Point FindChar(char c)
        {
            return MockTool.FindChar(text, c);
        }



        public Point Trooper(TrooperType type)
        {
            var c = '5';
            switch (type)
            {
                case TrooperType.Commander: c = 'c'; break;
                case TrooperType.FieldMedic: c = 'm'; break;
                case TrooperType.Soldier: c = 's'; break;
                case TrooperType.Scout: c = 't'; break;
                case TrooperType.Sniper: c = 'r'; break;
            }
            return FindChar(c);
        }

        public bool CanAttack(T attacker, int xFrom, int yFrom, T attackWho, int xTo, int yTo)
        {
            return true;
        }

        public int Width
        {
            get { return text[0].Length; }
        }

        public int Height
        {
            get { return text.Length; }
        }

    }
}
