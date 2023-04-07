using Content.Server.Spawners.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.FireStationServer.Roles.SCP.Science;

public sealed class SCPStationPointSystem : ISCPStationPointSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void Initialize()
    {

    }

    public bool IsSpawnPointAtSCPStation(EntityUid uid)
    {
        return _entityManager
            .System<SCPStationSystem>()
            .IsSpawnPointAtSCPStation(uid);
    }
}
