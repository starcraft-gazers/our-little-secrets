using System;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.FireStationServer.Roles.SCP.Science;

public sealed class SCPStationSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoaderSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private SCPStationPrototype? scpStationPrototype = default;
    private string scpShuttlePath = default!;
    private string scpStationPath = default!;
    private EntityUid scpStationUid = EntityUid.Invalid;
    private bool IsFallback = false;
    public override void Initialize()
    {
        scpStationPrototype = _prototypeManager
            .EnumeratePrototypes<SCPStationPrototype>()
            .First();

        if (scpStationPrototype == null)
            return;

        scpShuttlePath = scpStationPrototype.ShuttlePath;
        scpStationPath = scpStationPrototype.MapPath;

        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized, after: new[] { typeof(StationJobsSystem) });
    }

    private void OnStationInitialized(StationInitializedEvent msg)
    {
        if (!TryComp<StationJobsComponent>(msg.Station, out var stationJobs) || IsFallback)
            return;

        //Поменять потом на динамический парсинг количества работ и прототипов
        //Целенаправлено влезаем в stationJobs, чтобы учитывались приоритеты игроков и т.д.
        stationJobs.RoundStartTotalJobs = stationJobs.RoundStartTotalJobs + 6;
        stationJobs.MidRoundTotalJobs = stationJobs.MidRoundTotalJobs + 6;
        stationJobs.TotalJobs = stationJobs.MidRoundTotalJobs;

        stationJobs.JobList["SCPSecurity"] = 3;
        stationJobs.JobList["SCPScientist"] = 3;
        stationJobs.RoundStartJobList["SCPSecurity"] = 3;
        stationJobs.RoundStartJobList["SCPScientist"] = 3;

        _stationJobsSystem.UpdateJobsAvailable();
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        if (scpStationPrototype == null || ev.Map == MapId.Nullspace)
            return;

        var minPlayersToSCP = _configManager.GetCVar(CCVars.MinPlayersForSCP);
        IsFallback = _playerManager.GetAllPlayers().Count() < minPlayersToSCP;

        if (IsFallback)
            return;

        var stationUid = ev.Grids.First();
        var stationCoordinates = Transform(stationUid).Coordinates;
        if (!LoadSCPStation(ev.Map, stationUid, stationCoordinates, out scpStationUid))
            return;

        SetSCPStationName(scpStationUid);
        LoadSCPShuttle(ev.Map, stationCoordinates, scpStationUid);
    }

    private bool LoadSCPStation(MapId mapId, EntityUid mainStation, EntityCoordinates mainStationCoordinates, out EntityUid scpStationEntity)
    {
        var scpStationLoadOptions = new MapLoadOptions()
        {
            Offset = new Vector2(mainStationCoordinates.X + 1000, mainStationCoordinates.Y)
        };

        _mapLoaderSystem.TryLoad(mapId, scpStationPath, out var scpStationGrids, scpStationLoadOptions);
        scpStationEntity = scpStationGrids is null ? EntityUid.Invalid : scpStationGrids.First();

        return scpStationEntity != EntityUid.Invalid;
    }

    private void SetSCPStationName(EntityUid scpStationUid)
    {
        if (scpStationPrototype == null)
            return;

        var scpStationMeta = MetaData(scpStationUid);
        var nameGenerator = scpStationPrototype.NameGenerator;
        if (nameGenerator == null)
        {
            scpStationMeta.EntityName = scpStationPrototype.StationNameTemplate;
            return;
        }

        scpStationMeta.EntityName = nameGenerator.FormatName(scpStationPrototype.StationNameTemplate);
    }

    private void LoadSCPShuttle(MapId mapId, EntityCoordinates mainStationCoordinates, EntityUid scpStationUid)
    {

        var scpShuttleLoadOptions = new MapLoadOptions()
        {
            Offset = new Vector2(mainStationCoordinates.X + 1000, mainStationCoordinates.Y + 100)
        };

        if (!_mapLoaderSystem.TryLoad(mapId, scpShuttlePath, out var scpShuttleGrids, scpShuttleLoadOptions) || scpShuttleGrids == null)
            return;

        var scpShuttleUid = scpShuttleGrids.First();

        if (TryComp<ShuttleComponent>(scpShuttleUid, out var scpShuttleComponent))
        {
            _shuttleSystem.TryFTLDock(
                shuttleUid: scpShuttleUid,
                component: scpShuttleComponent,
                targetUid: scpStationUid
            );
        }
    }

    public bool IsSpawnPointAtSCPStation(EntityUid uid) => uid == scpStationUid && !IsFallback;

    public bool ShouldSpawnSCP() => !IsFallback;
}
