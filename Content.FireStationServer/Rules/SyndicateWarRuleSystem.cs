using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Humanoid;
using Content.Server.Mind.Components;
using Content.Server.NPC.Systems;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Server.Traitor;
using Content.Shared.Dataset;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Content.FireStationServer.Rules;
using Content.Server.Fax;
using Content.Server.Nuke;
using Content.Server.AlertLevel;
using Content.Server.Players;
using Content.Shared.Mobs.Systems;

namespace Content.Server.GameTicking.Rules;

//Базовая реализация на ядерных
//Посмотреть, понравится ли режим игрока, после дополнять

public sealed class SyndicateWarRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly FactionSystem _faction = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;


    private enum WinType
    {
        IDLE,
        Ops,
        Crew
    }

    private enum WinCondition
    {
        AllSyndicateDead,
        AllSyndicateAlive,
        AllCrewDead
    }

    private WinType _winType = WinType.IDLE;

    private WinType RuleWinType
    {
        get => _winType;
        set
        {
            _winType = value;

            if (value == WinType.Crew || value == WinType.Ops)
            {
                _roundEndSystem.EndRound();
            }
        }
    }
    private List<WinCondition> _winConditions = new();

    private MapId? _syndicatePlanet;
    private EntityUid? _syndicateOutpost;
    private EntityUid? _syndicateShuttle;
    private EntityUid? _mainStation;
    private float Elapsed { get; set; }
    private float SyndicateShuttleDockDelay { get; set; }
    private float SendFaxToCaptainDelay {get; set; }

    public override string Prototype => "SyndicateWar";

    private SyndicateWarRuleConfiguration _syndicateWarRuleConfig = new();

    private readonly Dictionary<string, StartingGearPrototype> _startingGearPrototypes = new();

    private readonly Dictionary<string, List<string>> _operativeNames = new();

    private readonly Dictionary<EntityUid, string> _operativeMindPendingData = new();

    private readonly Dictionary<string, IPlayerSession> _operativePlayers = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnPlayersSpawning);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<SyndicateWarOperativeComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SyndicateWarOperativeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SyndicateWarOperativeComponent, ComponentRemove>(OnComponentRemove);
    }

    public override void Update(float frameTime)
    {
        if (!RuleAdded || Configuration is not SyndicateWarRuleConfiguration data)
            return;

        Elapsed += frameTime;

        if (SyndicateShuttleDockDelay != -1 && Elapsed >= SyndicateShuttleDockDelay)
        {
            DockSyndicateShuttle();
            SyndicateShuttleDockDelay = -1;
        }

        if(SendFaxToCaptainDelay != -1 && Elapsed >= SendFaxToCaptainDelay)
        {
            SendFaxToCaptain();
            SendFaxToCaptainDelay = -1;
        }
    }

    private void DockSyndicateShuttle()
    {
        if (_syndicateShuttle == null || _syndicateShuttle == EntityUid.Invalid || _syndicateOutpost == null)
            return;

        if (TryComp<ShuttleComponent>(_syndicateShuttle, out var shuttle))
        {
            _shuttle.TryFTLDock((EntityUid) _syndicateShuttle, shuttle, _syndicateOutpost.Value);
        }

        if (_mainStation == null)
            return;

        _alertSystem.SetLevel(_mainStation.Value, "gamma", true, true, true, true);
    }

    private void OnComponentInit(EntityUid uid, SyndicateWarOperativeComponent component, ComponentInit args)
    {
        // If entity has a prior mind attached, add them to the players list.
        if (!TryComp<MindComponent>(uid, out var mindComponent) || !RuleAdded)
            return;

        var session = mindComponent.Mind?.Session;
        var name = MetaData(uid).EntityName;
        if (session != null)
            _operativePlayers.Add(name, session);
    }

    private void OnComponentRemove(EntityUid uid, SyndicateWarOperativeComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.InRound:
                OnRoundStart();
                break;
            case GameRunLevel.PostRound:
                OnRoundEnd();
                break;
        }
    }

    private void OnRoundStart()
    {
        _mainStation = _stationSystem.Stations.FirstOrNull();

        if (_mainStation == null)
            return;

        var filter = Filter.Empty();
        foreach (var syndi in EntityQuery<SyndicateWarOperativeComponent>())
        {
            if (!TryComp<ActorComponent>(syndi.Owner, out var actor))
            {
                continue;
            }

            _chatManager.DispatchServerMessage(actor.PlayerSession, Loc.GetString("syndicatewar-welcome"));
            filter.AddPlayer(actor.PlayerSession);
        }

        _audioSystem.PlayGlobal(_syndicateWarRuleConfig.GreetSound, filter, recordReplay: false);
    }

    private void OnRoundEnd()
    {
        // If the win condition was set to operative/crew major win, ignore.
        if (RuleWinType == WinType.Ops || RuleWinType == WinType.Crew)
        {
            return;
        }

        var allAlive = true;
        foreach (var (_, state) in EntityQuery<SyndicateWarOperativeComponent, MobStateComponent>())
        {
            if (state.CurrentState is MobState.Alive)
            {
                continue;
            }

            allAlive = false;
            break;
        }

        if (allAlive)
        {
            RuleWinType = WinType.Ops;
            _winConditions.Add(WinCondition.AllSyndicateAlive);
            return;
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        var winText = Loc.GetString($"syndicatewar-{_winType.ToString().ToLower()}");

        ev.AddLine(winText);

        ev.AddLine(Loc.GetString("syndicatewar-list-start"));
        foreach (var (name, session) in _operativePlayers)
        {
            var listing = Loc.GetString("syndicatewar-list-name", ("name", name), ("user", session.Name));
            ev.AddLine(listing);
        }
    }

    private void CheckRoundShouldEnd()
    {
        if (!RuleAdded || RuleWinType == WinType.Crew || RuleWinType == WinType.Ops)
            return;

        if (IsAllSyndiesDead())
        {
            RuleWinType = WinType.Crew;
            return;
        }

        if (IsAllCrewDead())
        {
            RuleWinType = WinType.Ops;
            return;
        }
    }

    private bool IsAllSyndiesDead()
    {
        MapId? shuttleMapId = EntityManager.EntityExists(_syndicateShuttle)
            ? Transform(_syndicateShuttle!.Value).MapID
            : null;

        var operatives = EntityQuery<SyndicateWarOperativeComponent, MobStateComponent, TransformComponent>(true);
        var operativesAlive = operatives
            .Where(ent => ent.Item3.MapID == shuttleMapId)
            .Any(ent => ent.Item2.CurrentState == MobState.Alive && ent.Item1.Running);

        if (operativesAlive)
            return false;

        var spawnsAvailable = EntityQuery<SyndicateWarSpawnerComponent>(true).Any();
        if (spawnsAvailable && shuttleMapId == _syndicatePlanet)
            return false;

        if (!spawnsAvailable)
        {
            _winConditions.Add(WinCondition.AllSyndicateDead);
            return true;
        }

        return false;
    }

    private bool IsAllCrewDead()
    {
        var aliveCrew = Filter.Broadcast()
             .AddWhere(session => IsPlayerStable((IPlayerSession) session))
             .AddWhere(session => !IsPlayerTraitor((IPlayerSession) session))
             .Recipients
             .Count();

        return aliveCrew > 0;
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
        {
            CheckRoundShouldEnd();
        }
    }

    private void OnPlayersSpawning(RulePlayerSpawningEvent ev)
    {
        if (!RuleAdded)
            return;

        // Basically copied verbatim from traitor code
        var playersPerOperative = _syndicateWarRuleConfig.PlayersPerOperative;
        var maxOperatives = _syndicateWarRuleConfig.MaxOperatives;

        var everyone = new List<IPlayerSession>(ev.PlayerPool);
        var prefList = new List<IPlayerSession>();
        var cmdrPrefList = new List<IPlayerSession>();
        var operatives = new List<IPlayerSession>();

        // The LINQ expression ReSharper keeps suggesting is completely unintelligible so I'm disabling it
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var player in everyone)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
            {
                continue;
            }

            var profile = ev.Profiles[player.UserId];
            if (profile.AntagPreferences.Contains(_syndicateWarRuleConfig.OperativeRoleProto))
            {
                prefList.Add(player);
            }

            if (profile.AntagPreferences.Contains(_syndicateWarRuleConfig.CommanderRolePrototype))
            {
                cmdrPrefList.Add(player);
            }
        }

        var numSyndies = MathHelper.Clamp(ev.PlayerPool.Count / playersPerOperative, 1, maxOperatives);

        for (var i = 0; i < numSyndies; i++)
        {
            IPlayerSession syndiOp;
            if (i == 0)
            {
                if (cmdrPrefList.Count == 0)
                {
                    if (prefList.Count == 0)
                    {
                        if (everyone.Count == 0)
                        {
                            Logger.InfoS("preset", "Insufficient ready players to fill up with syndicatewar, stopping the selection");
                            break;
                        }
                        syndiOp = _random.PickAndTake(everyone);
                        Logger.InfoS("preset", "Insufficient preferred syndicatewar commanders or syndiOp, picking at random.");
                    }
                    else
                    {
                        syndiOp = _random.PickAndTake(prefList);
                        everyone.Remove(syndiOp);
                        Logger.InfoS("preset", "Insufficient preferred syndicatewar commanders, picking at random from regular op list.");
                    }
                }
                else
                {
                    syndiOp = _random.PickAndTake(cmdrPrefList);
                    everyone.Remove(syndiOp);
                    prefList.Remove(syndiOp);
                    Logger.InfoS("preset", "Selected a preferred syndicatewar commander.");
                }
            }
            else
            {
                if (prefList.Count == 0)
                {
                    if (everyone.Count == 0)
                    {
                        Logger.InfoS("preset", "Insufficient ready players to fill up with syndicatewar, stopping the selection");
                        break;
                    }
                    syndiOp = _random.PickAndTake(everyone);
                    Logger.InfoS("preset", "Insufficient preferred syndicatewar, picking at random.");
                }
                else
                {
                    syndiOp = _random.PickAndTake(prefList);
                    everyone.Remove(syndiOp);
                    Logger.InfoS("preset", "Selected a preferred syndicatewar.");
                }
            }
            operatives.Add(syndiOp);
        }

        SpawnOperatives(numSyndies, operatives, false);

        foreach (var session in operatives)
        {
            ev.PlayerPool.Remove(session);
            GameTicker.PlayerJoinGame(session);
            var name = session.AttachedEntity == null
                ? string.Empty
                : MetaData(session.AttachedEntity.Value).EntityName;
            // TODO: Fix this being able to have duplicates
            _operativePlayers[name] = session;
        }
    }

    private void OnMindAdded(EntityUid uid, SyndicateWarOperativeComponent component, MindAddedMessage args)
    {
        if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind == null)
            return;

        var mind = mindComponent.Mind;

        if (_operativeMindPendingData.TryGetValue(uid, out var role))
        {
            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(role)));
            _operativeMindPendingData.Remove(uid);
        }

        if (!mind.TryGetSession(out var playerSession) || _operativePlayers.ContainsValue(playerSession))
            return;

        var name = MetaData(uid).EntityName;

        _operativePlayers.Add(name, playerSession);

        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (_syndicateWarRuleConfig.GreetSound != null)
        {
            _audioSystem.PlayGlobal(_syndicateWarRuleConfig.GreetSound, playerSession);
            _chatManager.DispatchServerMessage(playerSession, Loc.GetString("syndicatewar-welcome"));
        }
    }

    private bool SpawnMap()
    {
        if (_syndicatePlanet != null)
            return true; // Map is already loaded.

        var path = _syndicateWarRuleConfig.SyndicateOutpostMap;
        var shuttlePath = _syndicateWarRuleConfig.SyndicateShuttleMap;
        if (path == null)
        {
            Logger.ErrorS("syndicatewar", "No station map specified for syndicate!");
            return false;
        }

        if (shuttlePath == null)
        {
            Logger.ErrorS("syndicatewar", "No shuttle map specified for syndicate!");
            return false;
        }

        var mapId = _mapManager.CreateMap();
        var options = new MapLoadOptions()
        {
            LoadMap = true,
        };

        if (!_map.TryLoad(mapId, path.ToString(), out var outpostGrids, options) || outpostGrids.Count == 0)
        {
            Logger.ErrorS("syndicatewar", $"Error loading map {path} for syndicatewar!");
            return false;
        }

        _syndicateOutpost = outpostGrids[0];

        if (!_map.TryLoad(mapId, shuttlePath.ToString(), out var grids, new MapLoadOptions { Offset = Vector2.One * 1000f }) || !grids.Any())
        {
            Logger.ErrorS("syndicatewar", $"Error loading grid {shuttlePath} for syndicatewar!");
            return false;
        }

        var shuttleId = grids.First();

        // Naughty, someone saved the shuttle as a map.
        if (Deleted(shuttleId))
        {
            Logger.ErrorS("syndicatewar", $"Tried to load syndicatewar shuttle as a map, aborting.");
            _mapManager.DeleteMap(mapId);
            return false;
        }

        _syndicatePlanet = mapId;
        _syndicateShuttle = shuttleId;

        RemoveNukeFromShuttle(shuttleId);

        return true;
    }

    private void RemoveNukeFromShuttle(EntityUid shuttleId)
    {
        var shuttleTransfrom = EntityManager.EnsureComponent<TransformComponent>(shuttleId);

        foreach (var (nuke, nukeTransform) in EntityManager.EntityQuery<NukeComponent, TransformComponent>(true))
        {
            if (nukeTransform.MapID == null || nukeTransform.MapID != shuttleTransfrom.MapID || nuke?.Owner == null)
                continue;

            EntityManager.QueueDeleteEntity(nuke.Owner);
        }

        foreach (var (nukePaper, transform) in EntityManager.EntityQuery<NukeCodePaperComponent, TransformComponent>(true))
        {
            if (transform.MapID == null || transform.MapID != shuttleTransfrom.MapID || nukePaper?.Owner == null)
                continue;

            EntityManager.QueueDeleteEntity(nukePaper.Owner);
        }
    }

    private (string Name, string Role, string Gear) GetOperativeSpawnDetails(int spawnNumber)
    {
        string name;
        string role;
        string gear;

        // Spawn the Commander then Agent first.
        switch (spawnNumber)
        {
            case 0:
                name = Loc.GetString("syndicatewar-role-commander") + " " + _random.PickAndTake(_operativeNames[_syndicateWarRuleConfig.EliteNames]);
                role = _syndicateWarRuleConfig.CommanderRolePrototype;
                gear = _syndicateWarRuleConfig.CommanderStartGearPrototype;
                break;
            case 1:
                name = Loc.GetString("syndicatewar-role-agent") + " " + _random.PickAndTake(_operativeNames[_syndicateWarRuleConfig.NormalNames]);
                role = _syndicateWarRuleConfig.OperativeRoleProto;
                gear = _syndicateWarRuleConfig.MedicStartGearPrototype;
                break;
            default:
                name = Loc.GetString("syndicatewar-role-operator") + " " + _random.PickAndTake(_operativeNames[_syndicateWarRuleConfig.NormalNames]);
                role = _syndicateWarRuleConfig.OperativeRoleProto;
                gear = _syndicateWarRuleConfig.OperativeStartGearPrototype;
                break;
        }

        return (name, role, gear);
    }

    private void SetupOperativeEntity(EntityUid mob, string name, string gear, HumanoidCharacterProfile? profile)
    {
        MetaData(mob).EntityName = name;
        EntityManager.EnsureComponent<SyndicateWarOperativeComponent>(mob);

        if (profile != null)
        {
            _humanoidSystem.LoadProfile(mob, profile);
        }

        if (_startingGearPrototypes.TryGetValue(gear, out var gearPrototype))
        {
            _stationSpawningSystem.EquipStartingGear(mob, gearPrototype, profile);
        }

        _faction.RemoveFaction(mob, "NanoTrasen", false);
        _faction.AddFaction(mob, "Syndicate");
    }

    private void SpawnOperatives(int spawnCount, List<IPlayerSession> sessions, bool addSpawnPoints)
    {
        if (_syndicateOutpost == null)
            return;

        var outpostUid = _syndicateOutpost.Value;
        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityManager.EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != _syndicateWarRuleConfig.SpawnPointPrototype)
                continue;

            if (xform.ParentUid != _syndicateOutpost)
                continue;

            spawns.Add(xform.Coordinates);
            break;
        }

        if (spawns.Count == 0)
        {
            spawns.Add(EntityManager.GetComponent<TransformComponent>(outpostUid).Coordinates);
            Logger.WarningS("syndicatewar", $"Fell back to default spawn for syndicatewar!");
        }

        for (var i = 0; i < spawnCount; i++)
        {
            var spawnDetails = GetOperativeSpawnDetails(i);
            var syndicateOpsAntag = _prototypeManager.Index<AntagPrototype>(spawnDetails.Role);

            if (sessions.TryGetValue(i, out var session))
            {
                var profile = _prefs.GetPreferences(session.UserId).SelectedCharacter as HumanoidCharacterProfile;
                if (!_prototypeManager.TryIndex(profile?.Species ?? HumanoidAppearanceSystem.DefaultSpecies, out SpeciesPrototype? species))
                {
                    species = _prototypeManager.Index<SpeciesPrototype>(HumanoidAppearanceSystem.DefaultSpecies);
                }

                var mob = EntityManager.SpawnEntity(species.Prototype, _random.Pick(spawns));
                SetupOperativeEntity(mob, spawnDetails.Name, spawnDetails.Gear, profile);

                var newMind = new Mind.Mind(session.UserId)
                {
                    CharacterName = spawnDetails.Name
                };
                newMind.ChangeOwningPlayer(session.UserId);
                newMind.AddRole(new TraitorRole(newMind, syndicateOpsAntag));

                newMind.TransferTo(mob);
            }
        }
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded || Configuration is not SyndicateWarRuleConfiguration syndicateWarConfig)
            return;

        _syndicateWarRuleConfig = syndicateWarConfig;
        var minPlayers = syndicateWarConfig.MinPlayers;

        if (ev.Forced)
            return;

        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("syndicatewar-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length != 0)
            return;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("syndicatewar-no-one-ready"));
        ev.Cancel();
    }

    public override void Started()
    {
        RuleWinType = WinType.IDLE;
        _winConditions.Clear();
        _syndicateOutpost = null;
        _syndicatePlanet = null;
        _syndicateShuttle = null;

        _startingGearPrototypes.Clear();
        _operativeNames.Clear();
        _operativeMindPendingData.Clear();
        _operativePlayers.Clear();

        Elapsed = 0f;


        SyndicateShuttleDockDelay = 1200f;
        SendFaxToCaptainDelay = 120f;

        foreach (var proto in new[]
                 {
                     _syndicateWarRuleConfig.CommanderStartGearPrototype,
                     _syndicateWarRuleConfig.MedicStartGearPrototype,
                     _syndicateWarRuleConfig.OperativeStartGearPrototype
                 })
        {
            _startingGearPrototypes.Add(proto, _prototypeManager.Index<StartingGearPrototype>(proto));
        }

        foreach (var proto in new[] { _syndicateWarRuleConfig.EliteNames, _syndicateWarRuleConfig.NormalNames })
        {
            _operativeNames.Add(proto, new List<string>(_prototypeManager.Index<DatasetPrototype>(proto).Values));
        }


        if (!SpawnMap())
        {
            Logger.InfoS("syndicatewar", "Failed to load map for syndicatewar");
            return;
        }

        var query = EntityQuery<SyndicateWarOperativeComponent, MindComponent>(true);
        foreach (var (_, mindComp) in query)
        {
            if (mindComp.Mind == null || !mindComp.Mind.TryGetSession(out var session))
                continue;
            var name = MetaData(mindComp.Owner).EntityName;
            _operativePlayers.Add(name, session);
        }
    }

    private void SendFaxToCaptain()
    {
        var faxes = EntityManager.EntityQuery<FaxMachineComponent>();

        foreach (var fax in faxes)
        {
            if (!fax.ReceiveNukeCodes)
                continue;

            var printout = new FaxPrintout(
                Loc.GetString("syndicatewar-captain-announcements"),
                Loc.GetString("syndicatewar-captain-announcements-paper-name"),
                null,
                "paper_stamp-cent",
                new() { Loc.GetString("stamp-component-stamped-name-centcom") });
            _faxSystem.Receive(fax.Owner, printout, null, fax);
        }
    }


    private bool IsPlayerTraitor(IPlayerSession session)
    {
        return session.ContentData()
                ?.Mind
                ?.HasRole<TraitorRole>() ?? false;
    }

    private bool IsPlayerStable(IPlayerSession session)
    {
        var attached = session.AttachedEntity;
        if (attached != null)
        {
            return _mobState.IsAlive((EntityUid) attached) && !_mobState.IsCritical((EntityUid) attached);
        }

        return false;
    }

    public override void Ended() { }
}
