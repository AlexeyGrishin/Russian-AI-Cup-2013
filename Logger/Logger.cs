using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Battle;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.AI.Model;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Logger
{

    public class Logger
    {

        public string name;
        private string replayerHtml;
        private bool mapLogged = false;
        private bool givenName = false;
        private bool battleLogged = false;
        private bool battleLogged3 = false;

        public Logger()
        {
            name = String.Format("{0:yyyy-MM-dd hh-mm-ss}", DateTime.Now);
            var folder = "./Tools";
            replayerHtml = System.IO.File.ReadAllText(folder + "/universal.html");

        }

        public static void Name(string name)
        {
            Instance().name = name;
            Instance().givenName = true;
        }


        private static Logger instance = null;
        public static Logger Instance()
        {
            return (instance = instance ?? new Logger());
        }

        internal void LogMap(World map)
        {
            if (mapLogged) return;
            var mapName = LogMapTxt(map);
            LogMapVisibility(map, mapName);
        }

        private void LogMapVisibility(World map, string mapName)
        {
            StringBuilder builder = new StringBuilder();
            LogMapVisibility(map, builder);
            System.IO.File.WriteAllText(String.Format("./Res/visibility-{0}.html", mapName), WrapObserver(builder.ToString(), "visibility"));
        }

        private void LogMapVisibility(World map, StringBuilder builder)
        {
            var cells = map.Cells.Select((col, x) => col.Select((cell, y) => new Cell {
                Class = cell == CellType.Free ? "free" : ("wall " + (cell.ToString().Replace("Cover", "").ToLower())) ,
                Text = map.ToMaze().DangerIndex(x, y) == 0 ? "" : map.ToMaze().DangerIndex(x, y).ToString()
            }).ToArray()).ToArray();
            builder.Append("{'map':");
            LogMap(cells, builder);
            builder.Append(", 'visibility': [");
            builder.Append(String.Join(",", map.CellVisibilities.Select(b => b ? "1" : "0")));
            builder.Append("]}");
       }

        private string LogMapTxt(World map)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var col in map.Cells)
            {
                foreach (var cell in col)
                {
                    builder.Append(cell == CellType.Free ? " " : "x");
                }
                builder.AppendLine();
            }
            var mapName = givenName ? name : String.Join("_", map.Cells.Select(c => c.Count(cell => cell == CellType.Free) + ""));
            System.IO.File.WriteAllText(String.Format("./Res/map-{0}.txt", mapName), builder.ToString());
            return mapName;
        }

        public string LogMap(string mapName, Cell[][] map)
        {
            if (mapLogged) return "";
            StringBuilder builder = new StringBuilder();
            LogMap(map, builder);
            System.IO.File.WriteAllText(String.Format("./Res/map-{0}.json", mapName), builder.ToString());
            System.IO.File.WriteAllText(String.Format("./Res/map-{0}.html", mapName), WrapObserver(builder.ToString()));
            mapLogged = true;
            return mapName;
        }

        private void LogMap(Cell[][] map, StringBuilder builder)
        {
            builder.Append("[");
            bool firstRow = true;
            foreach (var col in map)
            {
                if (!firstRow) builder.Append(",");
                firstRow = false;
                builder.Append("[");
                bool firstCell = true;
                foreach (var cell in col)
                {
                    if (!firstCell) builder.Append(",");
                    firstCell = false;
                    if (cell == null)
                    {
                        builder.Append("{}");
                        continue;
                    }
                    builder.Append("{");
                    if (cell.Class != null)
                    {
                        builder.AppendFormat("'class': '{0}'", cell.Class);
                        if (cell.Text != null) builder.Append(",");
                    }
                    if (cell.Text != null) builder.AppendFormat("'text': '{0}'", cell.Text);
                    builder.Append("}");
                }
                builder.Append("]");
            }
            builder.Append("]");
        }

        public void LogBattle3(World world, BattleCase3<TrooperExt> battleCase3, IEnumerable<StrategyResult3> results3, StrategyResult3 best3)
        {
            if (!battleLogged3)
            {
                var b1 = new StringBuilder("[]");
                /*                b1.Append("{'map':");
                                LogMapVisibility(world, b1);
                                b1.Append("}, 'steps': []}");*/
                System.IO.File.WriteAllText(String.Format("./Res/battle3-{0}.html", name), WrapObserver(b1.ToString(), "steps"));
                battleLogged3 = true;
            }

            var builder = new StringBuilder();
            builder.Append("{name:\"");
            builder.Append(battleCase3.Self.Type + "-" + MyStrategy.Turn);
            builder.Append("\", data: ");
            LogMap(battleCase3.Self.BattleMap.ToArray().Select((row, x) => row.Select((cell, y) =>
            {
                var c = new Cell();
                if (cell != null)
                {
                    var trooperExt = battleCase3.All.Where(t => t.Location.X == x && t.Location.Y == y).Select(w => w.Warrior).FirstOrDefault();
                    //var trooper = world.Troopers.Where(t => t.X == cell.X && t.Y == cell.Y).FirstOrDefault();
                    if (trooperExt != null)
                    {
                        var trooper = trooperExt.orig;
                        c.Class = trooper.IsTeammate ? "friend" : "enemy";
                        c.Text = "cmsrt".Substring((int)trooper.Type, 1);
                        if (trooper.IsHoldingMedikit) c.Class += " medkit";
                        if (trooper.IsHoldingGrenade) c.Class += " grenade";
                        if (trooper.IsHoldingFieldRation) c.Class += "ration";
                        c.Class += " " + trooper.Type.ToString().ToLower();
                        if (trooperExt.Noticed) c.Class += " noticed";
                    }
                    else
                    {
                        c.Class = cell.Step == -1 ? "empty" : "";
                        c.Text = (cell.DangerIndex == 0 || cell.DangerIndex == 500) ? "" : cell.DangerIndex + "";
                        if (cell.PossibleSniper) { c.Class = " possible_sniper"; c.Text = "r?"; }
                    }
                    if (cell.CanBeAttackedOnStand) c.Class += " attacked";
                    if (cell.CanBeAttackedOnStand && !cell.CanBeAttackedOnKneel) c.Class += " safe_on_kneel";
                    if (cell.CanBeAttackedOnStand && cell.CanBeAttackedOnKneel && !cell.CanBeAttackedOnProne) c.Class += " safe_on_prone";
                    if (cell.CanAttackFromHere) c.Class += " canattack";
                    if (battleCase3.Self.WaysToAttack.Any(w => w.Contains(cell))) c.Class += " way_to_enemy";
                    if (battleCase3.Self.WaysToSafe.Any(w => w.Contains(cell))) c.Class += " way_to_safe";
                    if (battleCase3.Self.WaysToThrow.Any(w => w.Contains(cell))) c.Class += " way_to_throw";
                }
                else
                {
                    c.Class = "wall";
                    c.Class += " " + (world.Cells[x][y].ToString().Replace("Cover", "").ToLower());
                }
                return c;
            }).ToArray()).ToArray(), builder);
            builder.Append(", troopers: [");
            var first = true;
            foreach (var ally in battleCase3.All)
            {
                if (!first) builder.Append(", ");
                first = false;
                LogTrooper(ally.Warrior.orig, builder);
            }
            builder.Append("]");
            builder.Append(", active: \"" + battleCase3.Self.Type + "\"");
            builder.Append(", log: [");
            builder.Append('"' + (best3 == null ? "No best move" : best3.ToString2().Replace("\n", "\\n").Replace("\r", "")) + "\",'------',");
            builder.Append(String.Join(",", results3.Select(r => "\"" + r.ToString2().Replace("\n", "\\n").Replace("\r","") + '"')));
            builder.Append("]}");
            System.IO.File.AppendAllText(String.Format("./Res/battle3-{0}.html", name), WrapStep(builder.ToString()));

        }

        

        private void LogTrooper(Trooper trooper, StringBuilder builder)
        {
            builder.Append("{");
            builder.AppendFormat(" friend: {6}, ghost: {8}, type: \"{0}\", points: {1}, grenade: {2}, medkit: {3}, ration: {4}, hitpoints: {5}, sick: {7}  ",
                trooper.Type, trooper.ActionPoints, 
                trooper.IsHoldingGrenade ? 1 : 0, trooper.IsHoldingMedikit ? 1 : 0, 
                trooper.IsHoldingFieldRation ? 1 : 0, trooper.Hitpoints, 
                trooper.IsTeammate ? 1 : 0, trooper.Ext().IsSick ? 1 : 0,
                trooper.Ext().Noticed ? 1 : 0);
            builder.Append("}");
        }

        private string WrapObserver(string data, string type = "grid")
        {
            return String.Format("{0}<script>init({1},'{2}');</script>", replayerHtml, data, type);
        }

        private string WrapStep(string data)
        {
            return String.Format("<script>add({0})</script>", data);
        }

        public class Cell
        {
            public string Text { get; set; }
            public string Class { get; set; }
        }


    }

    
}
