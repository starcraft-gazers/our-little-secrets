using System.Collections.Generic;
using Content.FireStationServer._Craft.StationGoals.Graph.Steps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.FireStationServer._Craft.StationGoals.Graph.Shuttle;

public sealed class ShuttleMoveFromStation : Step
{
    internal override ExecuteState ExecuteStep(Dictionary<StepDataKey, object> results, StationGoalPaperSystem system)
    {
        var mapId = results.GetOrFallback<MapId>(StepDataKey.MAP_ID, MapId.Nullspace);
        var shuttleUid = results.GetOrFallback<EntityUid>(StepDataKey.SHUTTLE_UID, EntityUid.Invalid);

        if (mapId == MapId.Nullspace || shuttleUid == EntityUid.Invalid){
            system.logger.RootSawmill.Debug($"Step: {Name} Map or Shuttle uid invalid");
            return ExecuteState.Interrupted;
        }

        var mapManager = IoCManager.Resolve<IMapManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        var shuttleSystem = entitySystemManager.GetEntitySystem<ShuttleSystem>(); ;
        var entityManager = IoCManager.Resolve<IEntityManager>();

        var shuttleComponent = entityManager.EnsureComponent<ShuttleComponent>(shuttleUid);
        shuttleSystem.FTLTravel(
            shuttleUid: shuttleUid,
            component: shuttleComponent,
            target: mapManager.GetMapEntityId(mapId),
            startupTime: 5,
            hyperspaceTime: 30,
            dock: false
        );

        system.logger.RootSawmill.Debug($"Step: {Name} finished success");
        return ExecuteState.Finished;
    }
}
