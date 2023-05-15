// using Content.FireStationServer.GameRules.Cult.Components;
// using Robust.Shared.GameObjects;

// namespace Content.FireStationServer.GameRules.Cult;

// public sealed class NarsiSystem : EntitySystem
// {
//     public override void Initialize()
//     {
//         base.Initialize();
//         SubscribeLocalEvent<NarsiComponent, ComponentInit>(OnNarsiComonentInit);
//     }

//     private void OnNarsiComonentInit(EntityUid uid, NarsiComponent component, ComponentInit ev)
//     {
//         RaiseLocalEvent(new NarsiHasBeenSummonedEvent(uid, component, ev));
//     }

//     public sealed class NarsiHasBeenSummonedEvent : EntityEventArgs
//     {
//         public readonly EntityUid Uid;
//         public readonly NarsiComponent Component;
//         public readonly ComponentInit InitEvent;

//         public NarsiHasBeenSummonedEvent(EntityUid uid, NarsiComponent component, ComponentInit ev)
//         {
//             Uid = uid;
//             Component = component;
//             InitEvent = ev;
//         }
//     }
// }
