using paxgame3.Client.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgame3.Client.Service
{
    public static class GameService
    {
        public static async Task<Dictionary<int, Stats>> GenFightTask(GameHistory _game, Dictionary<int, Stats> RoundStats)
        {

            if (_game.battlefield == null)
                _game.battlefield = new Battlefield();

            _game.battlefield.Computing = true;
            _game.battlefield.Status = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();
            _game.battlefield.StatusKilled = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();

            //_game.battlefield.Units = new List<Unit>();
            //_game.battlefield.Units.AddRange(GameService.ShuffleUnits(_game.Players));

            List<Unit> Units = new List<Unit>(_game.battlefield.Units);

            List<Vector2> pos = new List<Vector2>(UnitService.ResetUnits(_game.battlefield.Units));
            _game.battlefield.UnitPostions = new ConcurrentDictionary<Vector2, bool>();
            foreach (var v in pos)
                _game.battlefield.UnitPostions.TryAdd(v, true);

            _game.battlefield.Units.Add(_game.battlefield.Def1);
            _game.battlefield.Units.Add(_game.battlefield.Def2);

            HashSet<double> PlayerTeam1 = new HashSet<double>();
            HashSet<double> PlayerTeam2 = new HashSet<double>();
            foreach (Player pl in _game.Players)
                if (pl.Pos <= 3)
                    PlayerTeam1.Add(pl.ID);
                else if (pl.Pos > 3)
                    PlayerTeam2.Add(pl.ID);

            foreach (Player pl in _game.Players)
                RoundStats[pl.Pos] = new Stats();

            int i = 0;
            Dictionary<int, Dictionary<int, Unit>> AddUnits = new Dictionary<int, Dictionary<int, Unit>>();
            while (true)
            {
                _game.battlefield.Done = 0;
                _game.battlefield.KilledUnits.Clear();
                _game.battlefield.Units = _game.battlefield.Units.Where(x => x.Healthbar > 0).ToList();

                if (_game.battlefield.Def1.Healthbar == 0 || _game.battlefield.Def2.Healthbar == 0)
                {
                    foreach (var pl in _game.Players)
                        pl.inGame = false;
                    break;
                }

                if (_game.battlefield.Units.Count() <= 2)
                    break;

                List<Unit> enemies1 = new List<Unit>();
                enemies1.AddRange(_game.battlefield.Units.Where(x => x.Owner > 3 && x.Race != UnitRace.Neutral));
                List<Unit> enemies2 = new List<Unit>();
                enemies2.AddRange(_game.battlefield.Units.Where(x => x.Owner <= 3 && x.Race != UnitRace.Neutral));

                foreach (Unit unit in _game.battlefield.Units.Where(x => x.Race == UnitRace.Neutral || x.Race == UnitRace.Decoy))
                {
                    if (!AddUnits.ContainsKey(unit.ID))
                        AddUnits[unit.ID] = new Dictionary<int, Unit>();
                    AddUnits[unit.ID][i] = unit.DeepCopy();
                }


                int Todo = _game.battlefield.Units.Count();

                foreach (Unit unit in _game.battlefield.Units.ToArray())
                    UnitService.Act(unit, _game.battlefield, enemies1, enemies2);


                while (true)
                {
                    if (_game.battlefield.Done >= Todo)
                    {
                        //_game.battlefield.Status[i] = new ConcurrentBag<Unit>();
                        //_game.battlefield.StatusKilled[i] = new ConcurrentBag<Unit>();
                        foreach (Unit unit in _game.battlefield.Units)
                        {
                            //Unit ent = new Unit();
                            //ent = (Unit)unit.Shallowcopy();
                            //_game.battlefield.Status[i].Add(ent);
                        }
                        foreach (Unit unit in _game.battlefield.KilledUnits)
                        {
                            //Unit ent = new Unit();
                            //ent = (Unit)unit.Shallowcopy();
                            //_game.battlefield.StatusKilled[i].Add(ent);

                            if (unit.Race != UnitRace.Defence)
                            {
                                RoundStats[unit.Owner].DamageDone += unit.DamageDone;
                                RoundStats[unit.Owner].MineralValueKilled += unit.MineralValueKilled;
                            }
                        }
                        break;
                    }
                    else
                        await Task.Delay(25);

                    if (i > 1000)
                    {
                        break;
                    }
                }
                i++;
            }
            _game.battlefield.Computing = false;

            return RoundStats;
        }

        public static async Task<(List<Unit>, Dictionary<int, Dictionary<int, Unit>>, Dictionary<int, Stats>)> GenFight(GameHistory _game)
        {

            if (_game.battlefield == null)
                _game.battlefield = new Battlefield();

            _game.battlefield.Computing = true;
            _game.battlefield.Status = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();
            _game.battlefield.StatusKilled = new ConcurrentDictionary<int, ConcurrentBag<Unit>>();
            
            _game.battlefield.Units = new List<Unit>();
            _game.battlefield.Units.AddRange(GameService.ShuffleUnits(_game.Players));

            List<Unit> Units = new List<Unit>(_game.battlefield.Units);

            List<Vector2> pos = new List<Vector2>(UnitService.ResetUnits(_game.battlefield.Units));
            _game.battlefield.UnitPostions = new ConcurrentDictionary<Vector2, bool>();
            foreach (var v in pos)
                _game.battlefield.UnitPostions.TryAdd(v, true);

            _game.battlefield.Units.Add(_game.battlefield.Def1);
            _game.battlefield.Units.Add(_game.battlefield.Def2);

            HashSet<double> PlayerTeam1 = new HashSet<double>();
            HashSet<double> PlayerTeam2 = new HashSet<double>();
            foreach (Player pl in _game.Players)
                if (pl.Pos <= 3)
                    PlayerTeam1.Add(pl.ID);
                else if (pl.Pos > 3)
                    PlayerTeam2.Add(pl.ID);

            Dictionary<int, Stats> RoundStats = new Dictionary<int, Stats>();
            foreach (Player pl in _game.Players)
                RoundStats[pl.Pos] = new Stats();

            int i = 0;
            Dictionary<int, Dictionary<int, Unit>> AddUnits = new Dictionary<int, Dictionary<int, Unit>>();
            while (true)
            {
                _game.battlefield.Done = 0;
                _game.battlefield.KilledUnits.Clear();
                _game.battlefield.Units = _game.battlefield.Units.Where(x => x.Healthbar > 0).ToList();

                if (_game.battlefield.Def1.Healthbar == 0 || _game.battlefield.Def2.Healthbar == 0)
                {
                    foreach (var pl in _game.Players)
                        pl.inGame = false;
                    break;
                }

                if (_game.battlefield.Units.Count() <= 2)
                    break;

                List<Unit> enemies1 = new List<Unit>();
                enemies1.AddRange(_game.battlefield.Units.Where(x => x.Owner > 3 && x.Race != UnitRace.Neutral));
                List<Unit> enemies2 = new List<Unit>();
                enemies2.AddRange(_game.battlefield.Units.Where(x => x.Owner <= 3 && x.Race != UnitRace.Neutral));

                foreach (Unit unit in _game.battlefield.Units.Where(x => x.Race == UnitRace.Neutral || x.Race == UnitRace.Decoy))
                {
                    if (!AddUnits.ContainsKey(unit.ID))
                        AddUnits[unit.ID] = new Dictionary<int, Unit>();
                    AddUnits[unit.ID][i] = unit.DeepCopy();
                }

                int Todo = _game.battlefield.Units.Count();

                foreach (Unit unit in _game.battlefield.Units.ToArray())
                    UnitService.Act(unit, _game.battlefield, enemies1, enemies2);

                while (true)
                    if (_game.battlefield.Done >= Todo)
                    {
                        _game.battlefield.Status[i] = new ConcurrentBag<Unit>();
                        _game.battlefield.StatusKilled[i] = new ConcurrentBag<Unit>();
                        foreach (Unit unit in _game.battlefield.Units)
                        {
                            Unit ent = new Unit();
                            ent = (Unit)unit.Shallowcopy();
                            _game.battlefield.Status[i].Add(ent);
                        }
                        foreach (Unit unit in _game.battlefield.KilledUnits)
                        {
                            Unit ent = new Unit();
                            ent = (Unit)unit.Shallowcopy();
                            _game.battlefield.StatusKilled[i].Add(ent);

                            if (unit.Race != UnitRace.Defence)
                            {
                                RoundStats[unit.Owner].DamageDone += unit.DamageDone;
                                RoundStats[unit.Owner].MineralValueKilled += unit.MineralValueKilled;
                            }
                        }
                        break;
                    }
                    else
                        await Task.Delay(25);
                i++;
            }
            _game.battlefield.Computing = false;

            return (Units, AddUnits, RoundStats);
        }

        public static async Task<(string, List<Unit>)> GenStyle(GameHistory _game, Dictionary<int, Dictionary<int, Unit>> AddUnits)
        {
            int total = _game.battlefield.Status.Count();
            string bab = "";
            List<float> HPTeam1 = new List<float>();
            List<float> HPTeam2 = new List<float>();
            List<Unit> GameAddUnits = new List<Unit>();

            foreach (Unit unit in _game.battlefield.Status.Values.First())
            {
                List<KeyValuePair<float, float>> To = new List<KeyValuePair<float, float>>();
                List<float> Health = new List<float>();
                List<float> Shield = new List<float>();
                foreach (var list in _game.battlefield.Status.Values)
                {
                    Unit myunit = list.SingleOrDefault(x => x.ID == unit.ID);
                    if (myunit != null)
                    {
                        To.Add(myunit.RelPos);
                        Health.Add(myunit.Healthbar);
                        Shield.Add(myunit.Shieldbar);
                    }
                }

                if (unit.ID == 10000 || unit.ID == 10001)
                {
                    string mc = "HPDefOne";
                    string ma = "HPTeamOneAnimation";
                    if (unit.ID == 10001)
                    {
                        mc = "HPDefTwo";
                        ma = "HPTeamTwoAnimation";
                    }

                    string myclass = string.Format(@"
.{0} {{
height: 20vh;
width: 2vw;
opacity: 0.6;
background-color: darkred;
animation-name: {1};
animation-duration: {2}s;
animation-timing-function: linear;
}}
", mc, ma, To.Count() * Battlefield.Ticks.TotalSeconds);

                    string mykeyframe = string.Format(@"
@keyframes {0} {{
", ma);
                    float lasthp = 0;
                    for (int i = 0; i < Health.Count(); i++)
                    {
                        float hp = MathF.Round(Health[i] / (unit.Healthpoints), 2);
                        int per = (int)(i * 100 / Health.Count());
                        if (i == Health.Count - 1 && i < total)
                            per = 100;

                        //if (hp != lasthp)
                        if (true)
                        {
                            mykeyframe += string.Format(@"{0}% {{
transform: scaleY({1});
transform-origin: bottom;
}}
", per, hp);
                        }
                        lasthp = hp;
                    }
                    mykeyframe += @"}
";

                    bab += myclass;
                    bab += mykeyframe;

                    mc = "SPDefOne";
                    ma = "SPTeamOneAnimation";
                    if (unit.ID == 10001)
                    {
                        mc = "SPDefTwo";
                        ma = "SPTeamTwoAnimation";
                    }

                    myclass = string.Format(@"
.{0} {{
height: 20vh;
width: 2vw;
opacity: 0.6;
background-color: darkblue;
animation-name: {1};
animation-duration: {2}s;
animation-timing-function: linear;
}}
", mc, ma, To.Count() * Battlefield.Ticks.TotalSeconds);

                    mykeyframe = string.Format(@"
@keyframes {0} {{
", ma);
                    float lastsp = 0;
                    for (int i = 0; i < Shield.Count(); i++)
                    {
                        float sp = MathF.Round(Shield[i] / (unit.Shieldpoints), 2);
                        int per = (int)(i * 100 / Shield.Count());
                        if (i == Shield.Count - 1 && i < total)
                            per = 100;

                        //if (sp != lastsp)
                        if (true)
                        {
                            mykeyframe += string.Format(@"{0}% {{
transform: scaleY({1});
transform-origin: bottom;
}}
", per, sp);
                        }
                        lastsp = sp;
                    }
                    mykeyframe += @"}
";

                    bab += myclass;
                    bab += mykeyframe;
                }
                else
                {

                    string myclass = string.Format(@"
.m{0}t {{
    animation-name: m{0}k;
    animation-duration: {1}s;
    animation-timing-function: linear;
    animation-fill-mode: forwards;
}}
", unit.ID, MathF.Round(To.Count() * (float)Battlefield.Ticks.TotalSeconds, 2));

                    string mykeyframe = string.Format(@"
@keyframes m{0}k {{
", unit.ID);

                    for (int i = 0; i < To.Count(); i++)
                    {
                        float opacity = 1;

                        int per = (int)(i * 100 / To.Count());
                        if (i == To.Count - 1 && i < total)
                        {
                            opacity = 0;
                            per = 100;
                        }

                        mykeyframe += string.Format(@"{0}% {{
transform: translate({2}vw, {1}vh);
opacity: {3}
}}
", per, To[i].Key, To[i].Value, opacity);
                    }
                    mykeyframe += @"}
";

                    bab += myclass;
                    bab += mykeyframe;
                }
            }


            foreach (var ent in AddUnits) 
            {
                GameAddUnits.Add(ent.Value.First().Value);
                double unitid = ent.Value.First().Value.ID;
                int delay = ent.Value.First().Key;
                int duration = ent.Value.Last().Key - delay;
                if (duration < 4)
                    duration = 4;

                string mynclass = string.Format(@"
.m{0}t {{
    opacity: 0;
    animation-name: m{0}k;
    animation-delay: {1}s;
    animation-duration: {2}s;
    animation-timing-function: linear;
    animation-fill-mode: forwards;
}}
", unitid, MathF.Round(((float)delay * (float)Battlefield.Ticks.TotalSeconds) - (float)Battlefield.Ticks.TotalSeconds, 2), MathF.Round((float)duration * (float)Battlefield.Ticks.TotalSeconds), 2);
                string mynkeyframe = string.Format(@"
@keyframes m{0}k {{
", unitid);

                for (int i = 0; i < duration; i++)
                {
                    int j = i + delay;
                    if (!ent.Value.ContainsKey(j))
                        j = ent.Value.Last().Key;

                    float opacity = 1;

                    int per = (int)(i * 100 / duration);
                    if (i == duration - 1)
                    {
                        opacity = 0;
                        per = 100;
                    }


                    mynkeyframe += string.Format(@"{0}% {{
transform: translate({2}vw, {1}vh);
opacity: {3}
}}
", per, ent.Value[j].RelPos.Key, ent.Value[j].RelPos.Value, opacity);
                }
                mynkeyframe += @"}
";
                bab += mynclass;
                bab += mynkeyframe;

            }

            foreach (var ent in _game.battlefield.Status)
            {
                var list = ent.Value;
                HPTeam1.Add(list.Where(x => x.Owner <= 3 && !x.Attributes.Contains(UnitAttributes.Neutral) && !x.Attributes.Contains(UnitAttributes.Defence) && !x.Attributes.Contains(UnitAttributes.Decoy)).Sum(s => s.Healthbar));
                HPTeam2.Add(list.Where(x => x.Owner > 3 && !x.Attributes.Contains(UnitAttributes.Neutral) && !x.Attributes.Contains(UnitAttributes.Defence) && !x.Attributes.Contains(UnitAttributes.Decoy)).Sum(s => s.Healthbar));
            }

            string mya1class = string.Format(@"
.ArmyTeamOne {{
height: 20vh;
width: 2vw;
opacity: 0.6;
background-color: darkmagenta;
animation-name: ArmyTeamOneAnimation;
animation-duration: {0}s;
animation-timing-function: linear;
}}
", HPTeam1.Count() * Battlefield.Ticks.TotalSeconds);

            string mya2class = string.Format(@"
.ArmyTeamTwo {{
height: 20vh;
width: 2vw;
opacity: 0.6;
background-color: darkmagenta;
animation-name: ArmyTeamTwoAnimation;
animation-duration: {0}s;
animation-timing-function: linear;
}}
", HPTeam1.Count() * Battlefield.Ticks.TotalSeconds);

            string mya1keyframe = string.Format(@"
@keyframes ArmyTeamOneAnimation {{
");

            string mya2keyframe = string.Format(@"
@keyframes ArmyTeamTwoAnimation {{
");
            for (int i = 0; i < HPTeam1.Count(); i++)
            {
                float hp1 = 0;
                if (HPTeam1.FirstOrDefault() > 0)
                    hp1 = MathF.Round(HPTeam1[i] / HPTeam1.First(), 2);
                float hp2 = 0;
                if (HPTeam2.FirstOrDefault() > 0)
                    hp2 = MathF.Round(HPTeam2[i] / HPTeam2.First(), 2);
                int per = (int)(i * 100 / HPTeam1.Count());

                mya1keyframe += string.Format(@"{0}% {{
transform: scaleY({1});
transform-origin: bottom;
}}
", per, hp1);
                mya2keyframe += string.Format(@"{0}% {{
transform: scaleY({1});
transform-origin: bottom;
}}
", per, hp2);
            }

            mya1keyframe += @"}
";
            mya2keyframe += @"}
";
            bab += mya1class;
            bab += mya1keyframe;
            bab += mya2class;
            bab += mya2keyframe;

            return (bab, GameAddUnits);
        }

        public static List<Unit> ShuffleUnits(List<Player> players)
        {
            List<List<Unit>> EvenLists = new List<List<Unit>>();
            List<List<Unit>> OddLists = new List<List<Unit>>();

            foreach (Player pl in players)
            {
                int i = 0;
                List<Unit> evenlist = new List<Unit>();
                List<Unit> oddlist = new List<Unit>();

                foreach (Unit unit in pl.Units.Where(x => x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned))
                {
                    i++;
                    if (i % 2 == 0)
                    {
                        evenlist.Add(unit);
                    }
                    else
                    {
                        oddlist.Add(unit);
                    }

                }
                EvenLists.Add(evenlist);
                OddLists.Add(oddlist);
            }

            List<Unit> combined = new List<Unit>();

            for (int i = 0; i < EvenLists.Count(); i++)
            {
                List<Unit> even = EvenLists[i];
                List<Unit> odd = OddLists[i];

                int max = even.Count();
                if (odd.Count() > max)
                    max = odd.Count();

                for (int j = 0; j < max; j++)
                {
                    if (j == max - 1)
                    {
                        if (odd.ElementAtOrDefault(j) != null)
                            combined.Add(odd[j]);
                        if (even.ElementAtOrDefault(j) != null)
                            combined.Add(even[j]);
                    }
                    else
                    {
                        combined.Add(odd[j]);
                        combined.Add(even[j]);
                    }
                }
            }
            return combined;
        }

        public static void Bot(Player pl, Player opp)
        {
            if (opp.Name == "Bot#1")
                OppService.Bot1TvZ(pl.Game.ID, pl, opp);
            else if (opp.Name == "Bot#2")
                OppService.Bot1ZvT(pl.Game.ID, pl, opp);
            else if (opp.Name == "Bot#3")
                OppService.BotRandom(pl.Game.ID, opp).GetAwaiter();
        }

        public static string GetBigPicture(string img)
        {
            return img.Replace("_tiny", "_t1");
        }

        public static string GetPicture(string img, int pos)
        {
            if (pos <= 3)
                return img.Replace(".png", "_t1.png");
            else 
                return img.Replace(".png", "_t2.png");
        }
    }
}
