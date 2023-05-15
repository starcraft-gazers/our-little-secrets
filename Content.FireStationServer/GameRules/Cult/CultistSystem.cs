// using Content.FireStationServer.GameRules.Cult.Components;
// using Content.Server.Chat.Managers;
// using Content.Server.GameTicking.Rules;
// using Content.Server.Mind.Components;
// using Content.Server.NPC.Systems;
// using Content.Server.Station.Systems;
// using Content.Shared.CombatMode.Pacification;
// using Content.Shared.Roles;
// using Robust.Shared.Audio;
// using Robust.Shared.GameObjects;
// using Robust.Shared.IoC;
// using Robust.Shared.Localization;
// using Robust.Shared.Prototypes;
// using Robust.Shared.Timing;
// using Robust.Shared.Utility;

// namespace Content.FireStationServer.GameRules.Cult;

// public sealed class CultistSystem : EntitySystem
// {
//     [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
//     [Dependency] private readonly IChatManager _chatManager = default!;
//     [Dependency] private readonly FactionSystem _faction = default!;
//     [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
//     [Dependency] private readonly StationSystem _stationSystem = default!;
//     [Dependency] private readonly CultistRuleSystem _cultistRuleSystem = default!;

//     private readonly SoundSpecifier? _cultistGreetSound = new SoundPathSpecifier("/Audio/CultSounds/fart.ogg");

//     public override void Initialize()
//     {
//         base.Initialize();

//         SubscribeLocalEvent<CultistComponent, ComponentInit>(OnCultistComponentInit);
//         SubscribeLocalEvent<CultistComponent, ComponentRemove>(OnComponentRemove);
//     }

//     private void OnComponentRemove(EntityUid uid, CultistComponent component, ComponentRemove args)
//     {
//         _cultistRuleSystem.CheckRoundShouldEnd();
//     }

//     private void OnCultistComponentInit(EntityUid uid, CultistComponent component, ComponentInit args)
//     {
//         if (!TryComp<MindComponent>(uid, out var mindComponent))
//             return;

//         if (mindComponent.Mind == null || mindComponent.Mind.OwnedComponent == null)
//             return;

//         var mind = mindComponent.Mind;

//         mind.AddRole(new CultistRole(mind, _prototypeManager.Index<AntagPrototype>("Cultist")));

//         if (mind.OwnedEntity != null)
//             RemComp<PacifiedComponent>(mind.OwnedEntity.Value);

//         if (!TryComp<CultistAbilitiesComponent>(uid, out _))
//             AddComp<CultistAbilitiesComponent>(uid);

//         if (!TryComp<BloodMagicComponent>(uid, out _))
//             AddComp<BloodMagicComponent>(uid);

//         _faction.RemoveFaction(mind.OwnedComponent.Owner, "NanoTrasen", false);
//         _faction.AddFaction(mind.OwnedComponent.Owner, "Cult");

//         if (!mind.TryGetSession(out var playerSession))
//             return;

//         _audioSystem.PlayGlobal(_cultistGreetSound, playerSession);

//         var targetStation = _stationSystem.GetOwningStation(uid);

//         if (targetStation == null)
//             return;

//         _chatManager.DispatchServerMessage(playerSession, Loc.GetString("cultists-welcome", ("station", (targetStation.Value))));
//     }
// }
