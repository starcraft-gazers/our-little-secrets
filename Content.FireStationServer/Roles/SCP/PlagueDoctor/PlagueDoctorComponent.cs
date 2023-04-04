using System;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.FireStationServer.Roles.SCP.PlagueDoctor;

[RegisterComponent]
public sealed class PlagueDoctorComponent : Component
{

    [DataField("HealAction")]
    public EntityTargetAction HealAction = new()
    {
        Icon = new SpriteSpecifier.Rsi(new ResourcePath("Structures/Decoration/banner.rsi"), "banner_medical"),
        ItemIconStyle = ItemActionIconStyle.NoItem,
        DisplayName = "Вылечить",
        Description = "Полностью излечите вашу цель",
        Priority = -1,
        Event = new PlagueDoctorHealEvent(),
        CheckCanAccess = false,
        UseDelay = TimeSpan.FromSeconds(300f),
        Range = 6f
    };

    [DataField("MakeZombieAction")]
    public EntityTargetAction MakeZombieAction = new EntityTargetAction()
    {
        Icon =new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/zombie-turn.png")),
        ItemIconStyle = ItemActionIconStyle.NoItem,
        DisplayName = "Сделать зомби",
        Description = "Делает из цели зомби, который является вашим спутником. Он не может заражать других игроков",
        Priority = -1,
        Event = new PlagueDoctorZombieEvent(),
        CheckCanAccess = false,
        UseDelay = TimeSpan.FromSeconds(600f),
        Range = 6f
    };
}
