using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle
{


    //введено для удобных описаний маршрутов
    public enum ActionDraft
    {
        None,
        StepToEnemy,
        StepFromEnemy,
        StepToSickAlly,
        StepBack,
        StepToThrow,
        StepToScout,
        Shoot,
        HealSelf,   //medkit
        HealAlly,
        ThrowGrenade,
        StandUp,
        OnKneel,
        LieDown,
        Skip,
        EatFieldRation
    }

    public interface Positioned2
    {
        PossibleMove Location { get; }
        int AttackRange { get;  }
        int VisionRange { get; }
        bool IsSick { get; }
        
    }

    public class Battle
    {

        public static IList<StrategyResult3> All3<T>(BattleCase3<T> battleCase) where T: Warrior2
        {
            var allCases = StrategiesFor<T>(battleCase.Self.StepsToAttack, battleCase.Self.Warrior.HasGrenade, battleCase.Self.Warrior.CanHeal, battleCase.SickAllies.Any()).Select(s => Emulator3.Emulate(battleCase, s)).ToList();
            allCases.Sort();
            allCases.Reverse();
            Console.WriteLine(String.Join("\n\n", allCases.Select(a => a.ToString2())));
            return allCases;
        }

        //мозг бота - набор стратегий на все случаи жизни :) подаются на вход эмулятору
        private static List<IStrategy> StrategiesFor<T>(int stepsToAttack, bool hasGrenade, bool isMedic, bool hasSickAlly) where T : Warrior2
        {
            var list = new List<IStrategy>();
            if (stepsToAttack == 0)
            {
                Add(list, "just shoot",                 ActionDraft.Shoot);
                Add(list, "shoot and go away",          ActionDraft.Shoot, ActionDraft.StepFromEnemy);
                Add(list, "shoot and go away: 2 steps", ActionDraft.Shoot, ActionDraft.StepFromEnemy, ActionDraft.StepFromEnemy);
                Add(list, "shoot and go away: 3 steps", ActionDraft.Shoot, ActionDraft.StepFromEnemy, ActionDraft.StepFromEnemy, ActionDraft.StepFromEnemy);
                Add(list, "kneel and shoot",                ActionDraft.OnKneel, ActionDraft.Shoot);
                Add(list, "kneel and shoot and stand back", ActionDraft.OnKneel, ActionDraft.Shoot, ActionDraft.StandUp);
                Add(list, "line down and shoot", ActionDraft.LieDown, ActionDraft.Shoot);
                Add(list, "shoot and kneel to hide", ActionDraft.Shoot, ActionDraft.OnKneel);
                Add(list, "shoot and lie down to hide", ActionDraft.Shoot, ActionDraft.LieDown);
                Add(list, "shoot and go away and kneel", ActionDraft.Shoot, ActionDraft.StepFromEnemy, ActionDraft.OnKneel);
                Add(list, "shoot and go away and lie down", ActionDraft.Shoot, ActionDraft.StepFromEnemy, ActionDraft.LieDown);
            }

            Add(list, "come and shoot", ActionDraft.StepToEnemy, 1, Math.Min(Math.Max(1, stepsToAttack), 5), ActionDraft.Shoot);
            //if (battleCase.Self.Type == TrooperType.Sniper)
            {
                Add(list, "kneel to hide", ActionDraft.OnKneel);
                Add(list, "lie down to hide", ActionDraft.LieDown);
                Add(list, "come and kneel", ActionDraft.StepToEnemy, 1, Math.Min(Math.Max(1, stepsToAttack), 5), ActionDraft.OnKneel);   //for sniper
                Add(list, "come and lie down", ActionDraft.StepToEnemy, 1, Math.Min(Math.Max(1, stepsToAttack), 5), ActionDraft.LieDown);   //for sniper
            }
            Add(list, "step away and shoot", ActionDraft.StepFromEnemy, 1, 1, ActionDraft.Shoot);
            if (hasGrenade)
            {
                Add(list, "come and throw grenade", ActionDraft.StepToEnemy, 0, 3, ActionDraft.ThrowGrenade);
            }
            Add(list, "go to enemy", ActionDraft.StepToEnemy, 1, 5);
            Add(list, "go away from enemy", ActionDraft.StepFromEnemy, 1, 5);
            Add(list, "stand still", ActionDraft.Skip);
            if (isMedic && hasSickAlly)
            {
                Add(list, "heal sick", ActionDraft.HealAlly);
                Add(list, "heal sick", ActionDraft.StepToSickAlly, 0, 5, ActionDraft.HealAlly);
                Add(list, "go to sick", ActionDraft.StepToSickAlly, 0, 5);
            }
            //Add(list, "scout", ActionDraft.StepToScout);
            //Add(list, "scout", ActionDraft.StepToScout, ActionDraft.StepToScout);
            //Add(list, "scout", ActionDraft.StepToScout, ActionDraft.StepBack);        //--> scout and go back - remember that there is no snipers at that points, for 1-2 moves
            Add(list, "come, shoot, and go back", ActionDraft.StepToEnemy, ActionDraft.Shoot, ActionDraft.StepBack);
            Add(list, "come x 2, shoot, and go back", ActionDraft.StepToEnemy, ActionDraft.StepToEnemy, ActionDraft.Shoot, ActionDraft.StepBack);
            Add(list, "come x 2, shoot, and go back x 2", ActionDraft.StepToEnemy, ActionDraft.StepToEnemy, ActionDraft.Shoot, ActionDraft.StepBack, ActionDraft.StepBack);
            return list;
        }

        private static void Add(List<IStrategy> list, string name, params ActionDraft[] actions)
        {
            list.Add(Strategy.Create(name, actions));
        }

        private static void Add(List<IStrategy> list, string name, ActionDraft move, int from, int to, params ActionDraft[] actions)
        {
            for (var steps = from; steps <= to; steps++)
            {
                list.Add(Strategy.Create(name, move, steps, actions));
            }
        }

    }


    public abstract class IStrategy
    {
        public abstract string Name { get; }
        public abstract List<ActionDraft> Actions { get; }
        public int StepsCount { get { return Actions.Count(Emulator3.IsMoving); } }
    }

    public class Strategy: IStrategy
    {
        private string name;
        private List<ActionDraft> actions;

        public Strategy(string name, params ActionDraft[] actions)
        {
            this.name = name;
            this.actions = actions.ToList();
        }

        public override string Name
        {
            get { return name; }
        }

        public override List<ActionDraft> Actions
        {
            get { return actions; }
        }

        public static Strategy Create(string name, params ActionDraft[] actions)
        {
            return new Strategy(name, actions);
        }

        public static Strategy Create(string name, ActionDraft moveAction, int count, params ActionDraft[] actions)
        {
            name = name + ": " + count + " steps";
            List<ActionDraft> drafts = new List<ActionDraft>();
            for (int i = 0; i < count; i++)
            {
                drafts.Add(moveAction);
            }
            drafts.AddRange(actions);
            return new Strategy(name, drafts.ToArray());
        }
    }
}
