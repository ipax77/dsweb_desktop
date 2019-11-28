using paxgame3.Client.Data;
using paxgame3.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace paxgame3.Client.Service
{
    public static class StatsService
    {
        public static async Task GenRoundStats(GameHistory _game, bool mstats = true)
        {
            float armyvaluet1 = _game.Health.First().Key;
            float armyvaluet2 = _game.Health.First().Value;

            int winner = 0;
            if (_game.Health.Last().Key > 0 && _game.Health.Last().Value == 0)
                winner = 1;
            else if (_game.Health.Last().Key == 0 && _game.Health.Last().Value > 0)
                winner = 2;

            StatsRound stats = new StatsRound();
            stats.winner = winner;
            stats.ArmyHPT1 = armyvaluet1;
            stats.ArmyHPT2 = armyvaluet2;

            foreach (Player player in _game.Players.OrderBy(o => o.Pos))
            {
                float damage = 0;
                float killed = 0;
                float army = 0;
                float tech = 0;
                Unit plmvp = new Unit();

                foreach (UnitAbility ability in player.AbilityUpgrades)
                    tech += ability.Cost;

                foreach (UnitUpgrade upgrade in player.Upgrades)
                    tech += UpgradePool.Upgrades.SingleOrDefault(x => x.Race == player.Race && x.Name == upgrade.Upgrade).Cost.ElementAt(upgrade.Level - 1).Value;


                
                foreach (Unit unit in _game.battlefield.Units.Where(x => x.Status == UnitStatuses.Spawned && x.Owner == player.Pos && x.Race == player.Race))
                {
                    damage += unit.DamageDoneRound;
                    killed += unit.MineralValueKilledRound;
                    army += unit.Cost;

                    unit.DamageDone += damage;
                    unit.MineralValueKilled += killed;

                    if (unit.DamageDoneRound > plmvp.DamageDoneRound)
                        plmvp = unit;
                }
                if (plmvp.DamageDoneRound > stats.MVP.DamageDoneRound)
                    stats.MVP = plmvp;

                stats.Damage.Add(damage);
                stats.Killed.Add(killed);
                stats.Army.Add(army);
                stats.Tech.Add(tech);
                stats.Mvp.Add(plmvp);

                if (mstats == true)
                {
                    M_stats chartstats = new M_stats();
                    chartstats.ArmyHPTeam1 = MathF.Round(stats.ArmyHPT1, 2);
                    chartstats.ArmyHPTeam2 = MathF.Round(stats.ArmyHPT2, 2);
                    chartstats.ArmyValue = MathF.Round(stats.Army.Last(), 2);
                    chartstats.DamageDone = MathF.Round(stats.Damage.Last(), 2);
                    if (winner == 1 && player.Pos <= 3)
                        chartstats.RoundsWon = 1;
                    else if (winner == 2 && player.Pos > 3)
                        chartstats.RoundsWon = 1;
                    chartstats.Upgrades = MathF.Round(stats.Tech.Last(), 2);
                    chartstats.VlaueKilled = MathF.Round(stats.Killed.Last(), 2);
                    player.Stats[_game.Spawn] = chartstats;
                }
            }
            _game.Stats.Add(stats);
        }
    }


}
