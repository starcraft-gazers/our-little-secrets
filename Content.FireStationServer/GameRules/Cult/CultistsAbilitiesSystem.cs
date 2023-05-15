// using Content.FireStationServer.GameRules.Cult.Components;
// using Content.Server.Actions;
// using Content.Server.Mind.Components;
// using Content.Shared.Actions;
// using Robust.Shared.GameObjects;
// using Robust.Shared.IoC;

// namespace Content.FireStationServer.GameRules.Cult;

// public sealed class CultistAbilitiesSystem : EntitySystem
// {

//     [Dependency] private readonly ActionsSystem _actionSystem = default!;

//     public override void Initialize()
//     {
//         base.Initialize();
//         SubscribeLocalEvent<CultistAbilitiesComponent, ComponentInit>(OnCultistAbilitiesComponentInit);
//     }

//     private void OnCultistAbilitiesComponentInit(EntityUid uid, CultistAbilitiesComponent component, ComponentInit args)
//     {
//         if (!TryComp<MindComponent>(uid, out var mindComponent) || !TryComp<CultistAbilitiesComponent>(uid, out var cultistAbilitiesComponent))
//             return;

//         _actionSystem.AddAction(uid, component.CultistBloodSpellsInstantAction, null);
//     }
// }

// public sealed class CultistCommunicateInstantActionEvent : InstantActionEvent { }

// public sealed class CultistBloodSpellsInstantActionEvent : InstantActionEvent { }
