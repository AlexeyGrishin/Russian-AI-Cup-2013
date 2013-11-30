using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk;
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
        public int VisionRange { get; set; }



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

    public class MockMaze<T> : IMaze, IWarriorMaze<T> where T: Positioned2
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



        public bool CanAttack(int xFrom, int yFrom, TrooperStance stance, int xTo, int yTo)
        {
            return true;
        }

        public Func<int, int, int> DangerIndexGetter { get; set; }

        public int DangerIndex(int x, int y, TrooperStance stance = TrooperStance.Standing)
        {
            return DangerIndexGetter == null ? 1 : DangerIndexGetter(x, y);
        }
    }

    public class Warrior2Mock : Warrior2
    {

        public int AHitpoints { get; set; }
        public override int Hitpoints
        {
            get { return AHitpoints; }
        }

        public override bool IsSick
        {
            get { return AHitpoints <= MyStrategy.NeedHeeling; }
        }



        public int AActions { get; set; }
        public override int Actions
        {
            get { return AActions; }
        }

        public TrooperStance APosition = TrooperStance.Standing;
        public override TrooperStance Position
        {
            get { return APosition;  }
        }

        public override int GetDamage(Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model.TrooperStance stance)
        {
            return ADamage * (TrooperStance.Standing - stance + 1);
        }

        public override int GetDamage()
        {
            return ADamage;
        }
        public int ADamage { get; set; }


        public bool AHasRation { get; set; }
        public override bool HasFieldRation
        {
            get { return AHasRation; }
        }

        public bool AHasMedkit { get; set; }
        public override bool HasMedkit
        {
            get { return AHasMedkit; }
        }

        public override int MaxActions
        {
            get { return 10; }
        }

        public bool AIsMedic { get; set; }
        public override bool IsMedic
        {
            get { return AIsMedic; }
        }

        public override int MedkitHealth
        {
            get { return 20; }
        }

        public PossibleMove ALocation { get; set; }
        public override PossibleMove Location
        {
            get { return ALocation; }
        }

        public int AAttackRange { get; set; }
        public override int AttackRange
        {
            get { return AAttackRange; }
        }

        public bool AHasGrenade { get; set; }
        public override bool HasGrenade
        {
            get { return AHasGrenade; }
        }

        public override int GrenadeRange
        {
            get { return 5; }
        }

        public override int GetGrenadeDamage(int delta)
        {
            return delta == 0 ? 80 : 60;
        }

        public int ShootCost = 3;
        public override int Cost(ActionType type)
        {
            return type == ActionType.ThrowGrenade ? 8 : (type == ActionType.Shoot ? ShootCost : 2);
        }

        public override int FieldRationExtraPoints
        {
            get { return 3; }
        }

        public override int MedicHealth
        {
            get { return 30; }
        }

        public TrooperType AType { get; set; }
        public override TrooperType Type { get { return AType; } }

        public int AVisionRange { get; set; }
        public override int VisionRange { get { return AVisionRange; } }
    }

}
