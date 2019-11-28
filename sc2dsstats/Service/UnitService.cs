using paxgame3.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using paxgame3.Client.Data;
using Microsoft.Extensions.Logging;
using System.Threading;
using sc2dsstats.Data;

namespace paxgame3.Client.Service
{
    public class UnitService
    {
        public static ILogger _logger;
        public static bool DEBUG = StartUp.DEBUG;

        public static KeyValuePair<Unit, Unit> EnemyinRange(Unit unit, List<Unit> enemies)
        {
            float attac_distance = -1;
            float vision_distance = -1;
            float minattac_distance = 0.25f;
            if (unit.Attributes.Contains(UnitAttributes.Suicide))
                minattac_distance = 0.25f;
            Unit myattac_enemy = new Unit();
            Unit myvision_enemy = new Unit();
            foreach (Unit enemy in enemies)
            {
                //float d = Vector2.Distance(unit.RealPos, enemy.RealPos) - enemy.Size / StartUp.Battlefieldmodifier;
                float d = Vector2.Distance(unit.RealPos, enemy.RealPos);
                d -= enemy.Size / StartUp.Battlefieldmodifier;

                if (d <= minattac_distance || d < unit.Attacrange)
                {
                    if (attac_distance == -1)
                    {
                        attac_distance = d;
                        myattac_enemy = enemy;
                    }

                    else if (d < attac_distance)
                    {
                        attac_distance = d;
                        myattac_enemy = enemy;
                    }
                }
                else if (d < unit.Visionrange)
                {
                    if (vision_distance == -1)
                    {
                        vision_distance = d;
                        myvision_enemy = enemy;
                    }

                    else if (d < vision_distance)
                    {
                        vision_distance = d;
                        myvision_enemy = enemy;
                    }
                }
            }
            return new KeyValuePair<Unit, Unit>(myattac_enemy, myvision_enemy);
        }

        public static async Task Act(Unit unit, Battlefield battlefield, List<Unit> enemies1, List<Unit> enemies2)
        {
            if (DEBUG) _logger.LogDebug(unit.ID + " act (" + unit.Healthbar + " " + unit.Speed +  ") " + unit.Name);

            if (unit.Healthbar > 0)
            {
                List<Unit> enemies = new List<Unit>();
                List<Unit> allies = new List<Unit>();
                if (unit.Owner <= 3)
                {
                    enemies = enemies1;
                    allies = enemies2;
                }
                else
                {
                    enemies = enemies2;
                    allies = enemies1;
                }

                await AbilityService.UseAbilities(unit, battlefield, enemies, allies);

                if (unit.Target != null && unit.Target.Healthbar > 0)
                {
                    await AbilityService.TriggerAbilities(unit, unit.Target, UnitAbilityTrigger.EnemyInVision, battlefield, enemies);
                    await FightService.Fight(unit, unit.Target, battlefield, enemies);
                }
                else
                {
                    KeyValuePair<Unit, Unit> myenemy = EnemyinRange(unit, enemies);

                    if (myenemy.Key.Name != null)
                    {
                        await AbilityService.TriggerAbilities(unit, myenemy.Key, UnitAbilityTrigger.EnemyInVision, battlefield, enemies);
                        unit.Target = myenemy.Key;
                        await FightService.Fight(unit, myenemy.Key, battlefield, enemies);
                    }
                    else
                    {
                        if (myenemy.Value.Name != null)
                            await AbilityService.TriggerAbilities(unit, myenemy.Value, UnitAbilityTrigger.EnemyInVision, battlefield, enemies);
                        await MoveService.Move(unit, myenemy.Value, battlefield);
                    }
                }
            }
            unit.Path.Add(new KeyValuePair<float, float>(unit.RelPos.Key, unit.RelPos.Value));
            Interlocked.Increment(ref battlefield.Done);
            if (DEBUG) _logger.LogDebug(unit.ID + " act done (" + unit.Healthbar + " " + unit.Speed + ") " + unit.Name);
        }

        public static List<Vector2> ResetUnits(List<Unit> units)
        {
            List<Vector2> pos = new List<Vector2>();
            foreach (Unit u in units)
            {
                lock (u)
                {
                    u.Healthbar = u.Healthpoints;
                    u.Shieldbar = u.Shieldpoints;
                    if (u.Energypoints > 0)
                        u.Energybar = UnitPool.Units.SingleOrDefault(x => x.Name == u.Name).Energybar;
                    u.Target = null;
                    u.Pos = u.BuildPos;
                    u.RealPos = u.BuildPos;
                    u.RelPos = MoveService.GetRelPos(u.RealPos);
                    u.Status = UnitStatuses.Spawned;
                    pos.Add(u.Pos);
                    u.Path = new List<KeyValuePair<float, float>>();

                    foreach (var ability in u.Abilities.ToArray())
                    {
                        ability.TargetPos = Vector2.Zero;
                        bool deactivated = ability.Deactivated;
                        ability.isActive = false;
                        ability.Deactivate(u);

                        if (UnitPool.Units.SingleOrDefault(x => x.Name == u.Name) != null && UnitPool.Units.SingleOrDefault(x => x.Name == u.Name).Abilities.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        {
                            UnitAbility reset = new UnitAbility();
                            reset = AbilityPool.Abilities.Where(x => x.Ability == ability.Ability).FirstOrDefault().DeepCopy();
                            reset.Deactivated = deactivated;
                            u.Abilities.Remove(ability);
                            u.Abilities.Add(reset);
                        }
                    }
                }
            }
            return pos;
        }
    }
}
