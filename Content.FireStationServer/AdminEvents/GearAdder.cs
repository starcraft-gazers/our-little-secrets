using Content.Server.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.FireStationServer.AdminEvents;

public sealed class GearAdder
{
    private IEntityManager EntityManager;
    private IPrototypeManager PrototypeManager;
    private InventorySystem InventorySystem;

    public GearAdder(IEntityManager entityManager, IPrototypeManager prototypeManager, InventorySystem inventorySystem)
    {
        EntityManager = entityManager;
        PrototypeManager = prototypeManager;
        InventorySystem = inventorySystem;
    }

    public void SetGearForPlayer(IPlayerSession session, StartingGearPrototype gearPrototype)
    {
        var attachedEntity = session.AttachedEntity;
        if (attachedEntity == null || attachedEntity == EntityUid.Invalid)
            return;
        var target = (EntityUid) attachedEntity;

        if (!EntityManager.TryGetComponent<InventoryComponent?>(target, out var inventoryComponent) || inventoryComponent == null)
            return;

        HumanoidCharacterProfile? profile = null;

        if (!EntityManager.TryGetComponent<ActorComponent?>(target, out var actorComponent) || actorComponent == null)
            return;

        if (InventorySystem.TryGetSlots(target, out var slotDefinitions, inventoryComponent))
        {
            foreach (var slot in slotDefinitions)
            {
                InventorySystem.TryUnequip(target, slot.Name, true, true, false, inventoryComponent);
                var gearStr = gearPrototype.GetGear(slot.Name, profile);
                if (gearStr == string.Empty)
                    continue;

                var equipmentEntity = EntityManager.SpawnEntity(gearStr, EntityManager.GetComponent<TransformComponent>(target).Coordinates);
                if (slot.Name == "id" && EntityManager.TryGetComponent<PDAComponent?>(equipmentEntity, out var pdaComponent) && pdaComponent.ContainedID != null)
                {
                    pdaComponent.ContainedID.FullName = EntityManager.GetComponent<MetaDataComponent>(target).EntityName;
                }

                InventorySystem.TryEquip(target, equipmentEntity, slot.Name, silent: true, force: true, inventory: inventoryComponent);
            }
        }

        //TODO Hands
        // if(EntityManager.TryGetComponent(target, out HandsComponent? handsComponent))
        // {

        // }
    }
}
