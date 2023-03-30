using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.FireStationServer.AdminEvents;

public sealed class ComponentAdder
{
    private IEntityManager EntityManager;

    public ComponentAdder(IEntityManager entityManager)
    {
        EntityManager = entityManager;
    }

    public void AddComponent(IPlayerSession playerSession, List<Component> components)
    {
        var attachedEntity = playerSession.AttachedEntity;

        if (attachedEntity == null || attachedEntity == EntityUid.Invalid)
            return;

        foreach (var component in components)
        {
            EntityManager.AddComponent((EntityUid) attachedEntity, component, true);
        }
    }
}
