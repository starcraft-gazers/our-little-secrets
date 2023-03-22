using System;
using System.Collections.Generic;
using System.Linq;
using Content.FireStationServer._Craft.StationGoals.Graph.Steps;
using Content.FireStationServer._Craft.Utils;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Coordinates.Helpers;
using Content.Server.Procedural;
using Content.Server.Shuttles.Components;
using Content.Shared.Gravity;
using Content.Shared.Procedural;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.FireStationServer._Craft.StationGoals.Graph.Shuttle.Dungeons;

public sealed class DungeonsGenerator : Step
{
    [DataField("DungeonPrototypeID", serverOnly: true, required: true)]
    private string DungeonPrototypeID = default!;

    [DataField("EnemiesPrototypes", serverOnly: true, required: false)]
    private string [] EnemiesPrototypes = Array.Empty<string>();

    [DataField("EnemiesCount", serverOnly: true)]
    private int EnemiesCount = 10;

    private MapId MapId = MapId.Nullspace;
    private EntityUid MapUid = EntityUid.Invalid;
    private MapGridComponent DungeonGrid = default!;

    /**
    Обертка. Посколько dungeon систем криво генерит в космосе, если там нет готового грида.
    А если грид есть, все-равно криво генерит, если карта не загрузила его через свой менеджер (т.е если просто добавим компонент)
    MR автора dungeon системы с фиксом не пропустили, а в нем менялся движок, так что мы тоже не будем его править
    И воспользуемся оберткой
    **/
    private EntityUid FTLTarget = EntityUid.Invalid;
    private Vector2 DungeonPosition = new Vector2(30f, 30f);
    internal override ExecuteState ExecuteStep(Dictionary<StepDataKey, object> results, StationGoalPaperSystem system)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var dependencies = new Dependencies(
            entityManager,
            IoCManager.Resolve<IMapManager>(),
            entityManager.System<DungeonSystem>(),
            entityManager.System<MapLoaderSystem>(),
            IoCManager.Resolve<IPrototypeManager>(),
            IoCManager.Resolve<IRobustRandom>(),
            IoCManager.Resolve<ITileDefinitionManager>(),
            entityManager.System<AtmosphereSystem>()
        );

        if (!PrepareToGeneration(dependencies, out var dungeon) || dungeon == null)
        {
            return ExecuteState.Interrupted;
        }

        GenerateDungeon(dungeon, dependencies);

        return ExecuteState.Finished;
    }

    private bool PrepareToGeneration(Dependencies dependencies, out DungeonConfigPrototype? dungeon)
    {
        var mapManager = dependencies.MapManager;
        var prototypeManager = dependencies.PrototypeManager;

        if (!prototypeManager.TryIndex<DungeonConfigPrototype>(DungeonPrototypeID, out dungeon))
        {
            return false;
        }

        MapId = mapManager.CreateMap();
        MapUid = mapManager.GetMapEntityId(MapId);

        if (!IFuckedRobustEngineLoadMap(dependencies))
        {
            return false;
        }

        DungeonGrid = dependencies.EntityManager.GetComponent<MapGridComponent>(FTLTarget);

        return true;
    }

    private bool IFuckedRobustEngineLoadMap(Dependencies dependencies)
    {
        if (!dependencies.MapLoaderSystem.TryLoad(MapId, "/Maps/Salvage/small-3.yml", out var grids) || grids == null || grids.Count <= 0)
        {
            return false;
        }

        FTLTarget = grids[0];

        return true;
    }

    private async void GenerateDungeon(DungeonConfigPrototype dungeon, Dependencies dependencies)
    {
        var seed = dependencies.RobustRandom.Next(1000, 20000000);
        await dependencies.DungeonSystem.GenerateDungeonAsync(dungeon, MapUid, DungeonGrid, DungeonPosition, seed);

        AddFTLDestination(dependencies);
        SetupComponents(dependencies);
        SpawnXenos(dependencies);
    }

    private void AddFTLDestination(Dependencies dependencies)
    {
        var entityManager = dependencies.EntityManager;

        var ftlComponent = entityManager.EnsureComponent<FTLDestinationComponent>(FTLTarget);
        ftlComponent.Enabled = true;
        ftlComponent.Whitelist = null;
    }

    private void SetupComponents(Dependencies dependencies)
    {
        SetupMetaData(dependencies);
        SetupGravity(dependencies);
    }
    private void SetupMetaData(Dependencies dependencies)
    {
        var metaDataComponent = dependencies.EntityManager.EnsureComponent<MetaDataComponent>(FTLTarget);
        metaDataComponent.EntityName = "Неизвестный объект";
    }

    private void SetupGravity(Dependencies dependencies)
    {
        var gravityComponent = dependencies.EntityManager.EnsureComponent<GravityComponent>(FTLTarget);
        gravityComponent.EnabledVV = true;
    }

    private void SpawnXenos(Dependencies dependencies)
    {
        for (int i = 1; i < EnemiesCount; i++)
        {
            if (CoordinationUtils.TryFindRandomSaveTile(FTLTarget, MapUid, dependencies.MapManager, dependencies.EntityManager, dependencies.TileDefinitions, dependencies.AtmosphereSystem, dependencies.RobustRandom, 10, out var coords))
            {
                var randomMob = dependencies.RobustRandom.Pick<string>(EnemiesPrototypes);
                if (randomMob == null)
                {
                    continue;
                }

                dependencies.EntityManager.SpawnEntity(randomMob, coords.SnapToGrid());
            }
        }
    }

    internal record class Dependencies
    {
        public IEntityManager EntityManager;
        public IMapManager MapManager;
        public DungeonSystem DungeonSystem;
        public IPrototypeManager PrototypeManager;
        public MapLoaderSystem MapLoaderSystem;
        public IRobustRandom RobustRandom;
        public ITileDefinitionManager TileDefinitions;
        public AtmosphereSystem AtmosphereSystem;

        internal Dependencies(
            IEntityManager entityManager,
            IMapManager mapManager,
            DungeonSystem dungeonSystem,
            MapLoaderSystem mapLoaderSystem,
            IPrototypeManager prototypeManager,
            IRobustRandom robustRandom,
            ITileDefinitionManager tileDefinitions,
            AtmosphereSystem atmosphereSystem
        )
        {
            EntityManager = entityManager;
            MapManager = mapManager;
            DungeonSystem = dungeonSystem;
            MapLoaderSystem = mapLoaderSystem;
            PrototypeManager = prototypeManager;
            RobustRandom = robustRandom;
            TileDefinitions = tileDefinitions;
            AtmosphereSystem = atmosphereSystem;
        }
    }
}
