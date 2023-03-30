using System;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.FireStationServer.Roles.Priest;

[RegisterComponent]
public sealed class PriestHealDiseaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StartTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DelayEndTime = TimeSpan.FromSeconds(10);

    [DataField("HealDiseaseAction")]
    public InstantAction HealDiseaseAction = new()
    {
        UseDelay = TimeSpan.FromSeconds(10),
        Icon = new SpriteSpecifier.Texture(new ResourcePath("Structures/Decoration/banner.rsi/banner_medical.png")),
        DisplayName = "Молитва за здравие",
        Description = "Помолитесь за здравие, чтобы попытаться излечить людей вокруг от болезней",
        Priority = -1,
        Event = new HealDiseaseActionEvent(),
    };
}
