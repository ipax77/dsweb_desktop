using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using System.Numerics;
using paxgame3.Client.Data;
using paxgame3.Client.Models;
using paxgame3.Client.Service;
using sc2dsstats.Data;
using pax.s2decode.Models;

namespace sc2dsstats.Pages
{
    public class BuildAreaBase : ComponentBase, IDisposable
    {
        [Parameter]
        public Player _player { get; set; }

        [Parameter]
        public double PlayerID { get; set; } = 0;

        [Parameter]
        public bool BestBuildMode { get; set; } = false;

        [Parameter]
        public bool ReverseBuild { get; set; } = false;

        [Parameter]
        public dsplayer dsPlayer { get; set; }

        [Inject] StartUp _startUp { get; set; }

        [Inject] Refresh _refresh { get; set; }
        [Inject] RefreshBB _refreshBB { get; set; }
        [Inject] RefreshPl _refreshPl { get; set; }
        [Inject] IMatToaster Toaster { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        public Unit DialogUnit;
        public Unit DialogSellUnit;
        public Unit ContainerUnit;
        public string _message { get; private set; }
        public string _title { get; private set; }
        public string snackBarTitleBadge { get; private set; }
        public bool snackBarInfo { get; private set; }
        public bool doUpdateBB { get; private set; }
        public bool dialogIsOpen { get; set; }
        public bool dialogSellIsOpen { get; set; }
        public bool startFight { get; set; } = false;
        public bool ShowHideAbilityUpgrade_bool { get; set; } = true; 
        public bool ShowHideAvailableUnits_bool { get; set; } = true;
        public bool showEnemyBuild { get; set; } = false;
        public bool buildrow_toggle_first { get; set; } = false;
        public bool buildrow_toggle_second { get; set; } = false;
        public bool isLoadformReplay = false;
        public string ContainerClass { get; set; } = "badge-info";
        public string ContainerInfo { get; set; } = "";
        public HashSet<UnitUpgrades> UpgradesAvailable { get; set; } = new HashSet<UnitUpgrades>();
        public HashSet<UnitAbilities> AbilityUpgradesAvailable { get; set; } = new HashSet<UnitAbilities>();
        public Dictionary<UnitAbilities, bool> AbilitiesGlobalDeactivated { get; set; } = new Dictionary<UnitAbilities, bool>();
        public Dictionary<int, Dictionary<UnitAbilities, bool>> AbilitiesSingleDeactivated { get; set; } = new Dictionary<int, Dictionary<UnitAbilities, bool>>();

        public float sizeone = 35;
        public float sizetwo = 70;
        public float sizethree = 105;
        public float diagone = 0;
        public float diagtwo = 0;
        public float diagthree = 0;
        public float distone = 0;
        public float disttwo = 0;
        public float distthree = 0;
        public int zindexone = 3;
        public int zindextwo = 2;
        public int zindexthree = 1;

        public string bab = "Und es war Sommer";


        protected override Task OnInitializedAsync()
        {
            if (_player == null && PlayerID != 0)
            {
                _player = _startUp.Players[PlayerID];
                ResetUpgrades();
            }
            else
            {
                _player = new Player();
                _player.MineralsCurrent = 500;
                _player.Race = UnitRace.Zerg;
                _player.Units.AddRange(UnitPool.Units.Where(x => x.Race == _player.Race && x.Cost > 0));
                _player.Game = new GameHistory();
                _player.Game.Players.Add(_player);
            }

            if (BestBuildMode == true && ReverseBuild == true)
            {
                ShowHideAvailableUnits_bool = false;
                ShowHideAbilityUpgrade_bool = false;
                _refreshBB.PropertyChanged += UpdateBB;
            }
            else if (BestBuildMode == false)
            {
                _refresh.PropertyChanged += Update;
            }

            _refreshPl.PropertyChanged += UpdatePl;

            diagone = MathF.Sqrt(2) * sizeone;
            diagtwo = MathF.Sqrt(2) * sizetwo;
            diagthree = MathF.Sqrt(2) * sizethree;

            distone = MathF.Sqrt(2) * (diagone - sizeone) * -1;
            disttwo = MathF.Sqrt(2) * (diagtwo - sizetwo) * -1;
            distthree = MathF.Sqrt(2) * (diagthree - sizethree) * -1;

            return base.OnInitializedAsync();
        }

        public async Task StartFight()
        {
            //TODO if vs Bot
            GameService.Bot(_player, _player.Game.Players.Single(x => x.Pos == 4));

            ConcurrentDictionary<int, AddUnit> addunits;
            addunits = await GameService2.GenFight(_player.Game);
            _player.Game.Units = new List<Unit>(_player.Game.battlefield.Units);
            _player.Game.Style = await GameService2.GenStyle(_player.Game, addunits);
            //_player.Game.Units.AddRange(temp);
            startFight = !startFight;
            await StatsService.GenRoundStats(_player.Game);
            // TODO: if auth
            await _player.Game.SaveSpawn();
            await _startUp.SaveGame(_player.Game, _player);
        }

        public EventCallback BuildCellClicked(Unit unit, Vector2 vec)
        {
            ContainerClass = "badge-info";
            ContainerInfo = "";

            if (ContainerUnit != null && unit == null)
            {
                // check size 
                int cellsize = 1;
                if (MathF.Floor(vec.X) == vec.X && MathF.Floor(vec.Y) != vec.Y)
                    cellsize = 2;
                // TODO size 3 (.25)

                if (ContainerUnit.BuildSize != cellsize)
                {
                    ContainerClass = "badge-danger";
                    ContainerInfo = "no space";
                    return new EventCallback();
                } else if (cellsize == 1)
                {
                    // check for size 2 unit arround

                }

                if (ContainerUnit.Status == UnitStatuses.Available)
                {
                    if (BestBuildMode == false && _player.MineralsCurrent < ContainerUnit.Cost)
                    {
                        ContainerUnit = null;
                        return new EventCallback();
                    }
                    Unit myunit = new Unit();
                    myunit = ContainerUnit.DeepCopy();

                    myunit.ID = UnitID.GetID(_player.Game.ID);
                    myunit.Status = UnitStatuses.Placed;
                    myunit.BuildPos = new Vector2(vec.X, vec.Y);
                    myunit.RealPos = myunit.BuildPos;
                    myunit.Pos = myunit.BuildPos;
                    myunit.SerPos = new Vector2Ser();
                    myunit.SerPos.x = myunit.BuildPos.X;
                    myunit.SerPos.y = myunit.BuildPos.Y;
                    myunit.RelPos = MoveService.GetRelPos(myunit.RealPos);
                    myunit.Owner = _player.Pos;
                    myunit.Ownerplayer = _player;
                    if (myunit.Bonusdamage != null)
                        myunit.Bonusdamage.Ownerplayer = myunit.Ownerplayer;

                    _player.MineralsCurrent -= myunit.Cost;
                    _player.Units.Add(myunit);
                    UpgradesAvailable.Add(myunit.AttacType);
                    UpgradesAvailable.Add(myunit.ArmorType);
                    if (myunit.Shieldpoints > 0)
                        UpgradesAvailable.Add(UnitUpgrades.ShieldArmor);

                    AbilitiesSingleDeactivated[myunit.ID] = new Dictionary<UnitAbilities, bool>();
                    foreach (UnitAbility ability in ContainerUnit.Abilities)
                    {
                        AbilityUpgradesAvailable.Add(ability.Ability);
                        if (!AbilitiesGlobalDeactivated.ContainsKey(ability.Ability))
                            AbilitiesGlobalDeactivated[ability.Ability] = false;
                        else
                            ability.Deactivated = AbilitiesGlobalDeactivated[ability.Ability];

                        if (!AbilitiesSingleDeactivated[myunit.ID].ContainsKey(ability.Ability))
                            AbilitiesSingleDeactivated[myunit.ID][ability.Ability] = false;
                        else
                            ability.Deactivated = AbilitiesSingleDeactivated[myunit.ID][ability.Ability];
                    }

                    UnitAbility imageability = myunit.Abilities.SingleOrDefault(x => x.Type.Contains(UnitAbilityTypes.Image));
                    if (imageability != null)
                        if (_player.AbilityUpgrades.SingleOrDefault(x => x.Ability == imageability.Ability) != null)
                            myunit.Image = imageability.Image;

                    ContainerInfo = "Buy unit";
                }
                else if (ContainerUnit.Status == UnitStatuses.Placed || ContainerUnit.Status == UnitStatuses.Spawned)
                {
                    ContainerUnit.BuildPos = new Vector2(vec.X, vec.Y);
                    ContainerUnit.RealPos = ContainerUnit.BuildPos;
                    ContainerUnit.Pos = ContainerUnit.BuildPos;
                    ContainerUnit.SerPos = new Vector2Ser();
                    ContainerUnit.SerPos.x = ContainerUnit.BuildPos.X;
                    ContainerUnit.SerPos.y = ContainerUnit.BuildPos.Y;
                    ContainerUnit = null;
                    ContainerInfo = "Replace unit";
                }
            }
            else if (ContainerUnit == null && unit != null)
            {
                zindexone = 3;
                zindextwo = 2;
                zindexthree = 1;

                if (unit.BuildSize == 2)
                    zindextwo = 5;
                else if (unit.BuildSize == 3)
                    zindexthree = 5;
                ContainerUnit = unit;
            }
            else if (ContainerUnit != null && unit != null)
            {
                //ContainerClass = "badge-danger";
                //ContainerInfo = "Only one unit allowed.";
                zindexone = 3;
                zindextwo = 2;
                zindexthree = 1;

                if (unit.BuildSize == 2)
                    zindextwo = 5;
                else if (unit.BuildSize == 3)
                    zindexthree = 5;
                ContainerUnit = unit;
                ContainerInfo = "Replace unit";
            }
            else
            {
                ContainerInfo = "Click one Unit first to replace or buy.";
            }
            StateHasChanged();
            return new EventCallback();
        }

        public void OpenDialog(Unit unit)
        {
            if (unit != null)
            {
                dialogIsOpen = false;
                DialogUnit = null;
                StateHasChanged();
                DialogUnit = unit;
                dialogIsOpen = true;
            }
        }

        public EventCallback BuyUnit(Unit unit)
        {
            ContainerClass = "badge-info";
            ContainerInfo = "Buy unit";

            ContainerUnit = unit;

            zindexone = 3;
            zindextwo = 2;
            zindexthree = 1;

            if (unit.BuildSize == 2)
                zindextwo = 5;
            else if (unit.BuildSize == 3)
                zindexthree = 5;

            return new EventCallback();
        }

        public EventCallback UpgradeUnit(UnitUpgrades upgrade)
        {
            (int cost, int lvl) = GetUpgradeCost(upgrade);
            _player.MineralsCurrent -= cost;

            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();
            if (myupgrade == null) return new EventCallback();

            UnitUpgrade plup = _player.Upgrades.Where(x => x.Upgrade == myupgrade.Name).FirstOrDefault();
            if (plup != null)
            {
                if (plup.Level < 3)
                    plup.Level++;
            }
            else
            {
                UnitUpgrade newup = new UnitUpgrade();
                newup.Upgrade = myupgrade.Name;
                newup.Level = 1;
                _player.Upgrades.Add(newup);
            }
            return new EventCallback();
        }

        public EventCallback AbilityUpgradeUnit(UnitAbility ability)
        {
            _player.MineralsCurrent -= BBService.AbilityUpgradeUnit(ability, _player);

            return new EventCallback();
        }

        public (int, int) GetUpgradeCost(UnitUpgrades upgrade)
        {
            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();

            if (myupgrade == null) return (0, 0);

            if (_player.Upgrades != null && _player.Upgrades.Count() > 0)
            {
                UnitUpgrade plup = _player.Upgrades.Where(x => x.Upgrade == myupgrade.Name).FirstOrDefault();
                if (plup != null)
                {
                    if (plup.Level == 3)
                    {
                        return (0, 4);
                    }
                    else
                        return (myupgrade.Cost.SingleOrDefault(x => x.Key == plup.Level + 1).Value, plup.Level + 1);
                }
            }

            return (myupgrade.Cost[0].Value, 1);
        }

        public void DeactivateAbilityGlobal(UnitAbility ability)
        {
            AbilitiesGlobalDeactivated[ability.Ability] = !AbilitiesGlobalDeactivated[ability.Ability];
            if (AbilitiesGlobalDeactivated[ability.Ability] == true)
            {
                _player.AbilitiesDeactivated.Add(ability.Ability);
                foreach (Unit unit in _player.Units.Where(x => (x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned) && x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null))
                    foreach (UnitAbility uability in unit.Abilities.Where(x => x.Ability == ability.Ability))
                    {
                        uability.Deactivated = true;
                        AbilitiesSingleDeactivated[unit.ID][uability.Ability] = true;
                    }
            }
            else
            {
                _player.AbilitiesDeactivated.Remove(ability.Ability);
                foreach (Unit unit in _player.Units.Where(x => (x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned) && x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null))
                    foreach (UnitAbility uability in unit.Abilities.Where(x => x.Ability == ability.Ability))
                    {
                        uability.Deactivated = false;
                        AbilitiesSingleDeactivated[unit.ID][uability.Ability] = false;
                    }
            }
        }

        public void DeactivateAbilitySingle(UnitAbility ability, Unit myunit)
        {
            AbilitiesSingleDeactivated[myunit.ID][ability.Ability] = !AbilitiesSingleDeactivated[myunit.ID][ability.Ability];

            Unit unit = _player.Units.SingleOrDefault(x => x.ID == myunit.ID);
            if (unit != null)
                if (AbilitiesSingleDeactivated[myunit.ID][ability.Ability] == true)
                    unit.Abilities.SingleOrDefault(x => x.Ability == ability.Ability).Deactivated = true;
                else
                    unit.Abilities.SingleOrDefault(x => x.Ability == ability.Ability).Deactivated = false;
        }

        public void SellUnit()
        {
            if (ContainerUnit == null)
            {
                DialogSellUnit = new Unit();
                DialogSellUnit.Name = "Click one unit in the Build Area first to sell it. Or one Unit in Availabe Units to sell all Units of that kind.";
                DialogSellUnit.ID = 0;
                DialogSellUnit.Image = "images/pax_cc.png";
                dialogSellIsOpen = true;
            }
            else
            {
                if (ContainerUnit.Status == UnitStatuses.Placed)
                {
                    _player.MineralsCurrent += ContainerUnit.Cost;
                    _player.Units.Remove(ContainerUnit);
                    ContainerUnit = null;
                }
                else
                {
                    DialogSellUnit = ContainerUnit;
                    dialogSellIsOpen = true;
                }
            }
        }

        public void DoSellUnit()
        {
            if (DialogSellUnit.Status == UnitStatuses.Spawned)
            {
                _player.MineralsCurrent += (int)(ContainerUnit.Cost * 0.7);
                _player.Units.Remove(ContainerUnit);
                ContainerUnit = null;
            }
            else if (DialogSellUnit.Status == UnitStatuses.Available)
            {
                foreach (Unit unit in _player.Units.Where(x => x.Name == ContainerUnit.Name && (x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned)).ToArray())
                {
                    if (unit.Status == UnitStatuses.Spawned)
                        _player.MineralsCurrent += (int)(unit.Cost * 0.7);
                    else if (unit.Status == UnitStatuses.Placed)
                        _player.MineralsCurrent += unit.Cost;

                    _player.Units.Remove(unit);
                    ContainerUnit = null;
                }
            }
            dialogSellIsOpen = false;
            DialogSellUnit = null;
        }

        public void OkClick()
        {
            DialogUnit = null;
            dialogIsOpen = false;
            dialogSellIsOpen = false;
        }

        public void Update(object sender, EventArgs e)
        {
            startFight = false;
            _player.Game.Spawn++;
            _player.MineralsCurrent += StartUp.Income;

            if (_player.Game.Stats.LastOrDefault() != null)
            {
                _message = "Damage done: " + Math.Round(_player.Game.Stats.Last().Damage[_player.Pos - 1], 2) + Environment.NewLine;
                _message += "Mineral Value Killed: " + _player.Game.Stats.Last().Killed[_player.Pos - 1] + Environment.NewLine;
                _message += "MVP: " + _player.Game.Stats.Last().Mvp[_player.Pos - 1].Name + " at " + _player.Game.Stats.Last().Mvp[_player.Pos - 1].BuildPos.X + "|" + _player.Game.Stats.Last().Mvp[_player.Pos - 1].BuildPos.X;

                if ((_player.Game.Stats.Last().winner == 1 && _player.Pos <= 3) || (_player.Game.Stats.Last().winner == 2 && _player.Pos > 3))
                {
                    _title = "Last Round Won!";
                    snackBarTitleBadge = "badge-success";
                    Toaster.Add(_message, MatToastType.Success, _title);
                }
                else
                {
                    _title = "Last Round Lost!";
                    snackBarTitleBadge = "badge-danger";
                    Toaster.Add(_message, MatToastType.Danger, _title);
                }

                snackBarInfo = true;
            }

            if (_player.inGame == false)
                NavigationManager.NavigateTo("/gameend/" + _player.Game.ID + "/" + _player.ID);
            else
                InvokeAsync(() => StateHasChanged());
        }

        public void UpdateBB(object sender, PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName == "BestBuild")
            {
                doUpdateBB = true;
            }
            else
            {
                if (doUpdateBB == true)
                {
                    _refreshBB.BestBuild.SetBuild(_player).GetAwaiter().GetResult();
                    _startUp.Players[_player.ID] = _player;
                    ResetUpgrades();

                    doUpdateBB = false;
                }
                InvokeAsync(() => StateHasChanged());
            }
        }

        public void UpdatePl(object sender, PropertyChangedEventArgs e)
        {
            _player = _refreshPl.Players[_player.Pos];
            if (_refreshPl.dsPlayers.ContainsKey(_player.Pos))
                dsPlayer = _refreshPl.dsPlayers[_player.Pos];
            ResetUpgrades();
            InvokeAsync(() => StateHasChanged());
        }

        public void ResetUpgrades()
        {
            UpgradesAvailable.Clear();
            AbilityUpgradesAvailable.Clear();
            foreach (Unit myunit in _player.Units)
            {
                UpgradesAvailable.Add(myunit.AttacType);
                UpgradesAvailable.Add(myunit.ArmorType);
                if (myunit.Shieldpoints > 0)
                    UpgradesAvailable.Add(UnitUpgrades.ShieldArmor);

                AbilitiesSingleDeactivated.Clear();
                AbilitiesSingleDeactivated[myunit.ID] = new Dictionary<UnitAbilities, bool>();
                foreach (UnitAbility ability in myunit.Abilities)
                {
                    AbilityUpgradesAvailable.Add(ability.Ability);
                    if (!AbilitiesGlobalDeactivated.ContainsKey(ability.Ability))
                        AbilitiesGlobalDeactivated[ability.Ability] = false;
                    else
                        ability.Deactivated = AbilitiesGlobalDeactivated[ability.Ability];

                    if (!AbilitiesSingleDeactivated[myunit.ID].ContainsKey(ability.Ability))
                        AbilitiesSingleDeactivated[myunit.ID][ability.Ability] = false;
                    else
                        ability.Deactivated = AbilitiesSingleDeactivated[myunit.ID][ability.Ability];
                }

                UnitAbility imageability = myunit.Abilities.SingleOrDefault(x => x.Type.Contains(UnitAbilityTypes.Image));
                if (imageability != null)
                    if (_player.AbilityUpgrades.SingleOrDefault(x => x.Ability == imageability.Ability) != null)
                        myunit.Image = imageability.Image;
            }
        }

        public void Dispose()
        {
            _refresh.PropertyChanged -= Update;
            _refreshBB.PropertyChanged -= UpdateBB;
            _refreshPl.PropertyChanged -= UpdatePl;
        }
    }
}
