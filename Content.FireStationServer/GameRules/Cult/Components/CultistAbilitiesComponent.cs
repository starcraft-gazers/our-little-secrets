// using System;
// using Content.Shared.Actions.ActionTypes;
// using Robust.Shared.GameObjects;
// using Robust.Shared.Serialization.Manager.Attributes;
// using Robust.Shared.Utility;

// namespace Content.FireStationServer.GameRules.Cult.Components;


// [RegisterComponent]
// public sealed class CultistAbilitiesComponent : Component
// {
//     [DataField("cultistBloodSpellsInstantAction")]
//     public InstantAction CultistBloodSpellsInstantAction = new()
//     {
//         UseDelay = TimeSpan.FromSeconds(5),
//         Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/FireStation/Mobs/tome.png")),
//         DisplayName = "cultist-abilities-blood-spells-action-name",
//         Description = "cultist-abilities-blood-spells-action-description",
//         Priority = -1,
//         Event = new CultistBloodSpellsInstantActionEvent(),
//     };
// }

