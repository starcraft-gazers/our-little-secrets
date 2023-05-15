// using System.Collections.Generic;
// using System.Linq;
// using Content.FireStationServer.GameRules.Cult;
// using Content.FireStationServer.GameRules.Cult.Components;
// using Content.Server.Chat.Managers;
// using Content.Server.GameTicking.Rules.Components;
// using Content.Server.Mind.Components;
// using Content.Server.NPC.Systems;
// using Content.Server.Players;
// using Content.Server.Roles;
// using Content.Server.RoundEnd;
// using Content.Server.Station.Components;
// using Content.Server.Station.Systems;
// using Content.Shared.Mobs;
// using Content.Shared.Mobs.Components;
// using Robust.Server.GameObjects;
// using Robust.Server.Player;
// using Robust.Shared.GameObjects;
// using Robust.Shared.IoC;
// using Robust.Shared.Localization;
// using Robust.Shared.Log;
// using Robust.Shared.Maths;
// using Robust.Shared.Player;
// using Robust.Shared.Prototypes;
// using Robust.Shared.Random;
// using Robust.Shared.Utility;

// namespace Content.Server.GameTicking.Rules;

// public sealed class CultistRuleSystem : GameRuleSystem<CultistRuleComponent>
// {
//     [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
//     [Dependency] private readonly IRobustRandom _random = default!;
//     [Dependency] private readonly IChatManager _chatManager = default!;
//     [Dependency] private readonly FactionSystem _faction = default!;
//     [Dependency] private readonly StationSystem _stationSystem = default!;
//     [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
//     [Dependency] private readonly SharedAudioSystem _audioSystem = default!;


//     private const string CultistPrototypeId = "Cultist";

//     private enum RoundState
//     {
//         None,
//         Started,
//         Ended
//     }

//     private enum WinType
//     {
//         CultMajor,
//         CultMinor,
//         Neutral,
//         CrewMinor,
//         CrewMajor
//     }

//     private enum WinCondition
//     {
//         NarsiWasSummonedOnCorrectStation,
//         NarsiWasSummonedOnIncorrectLocation,
//         AllCultistsDead,
//         SomeCultistsAlive,
//         MoreThanNeededCultistsAlive,
//         AllCultistsAlive
//     }

//     private WinType _winType = WinType.Neutral;

//     private WinType RuleWinType
//     {
//         get => _winType;
//         set
//         {
//             _winType = value;

//             if (value == WinType.CrewMajor || value == WinType.CultMajor)
//             {
//                 _roundEndSystem.EndRound();
//             }
//         }
//     }
//     private readonly List<WinCondition> _winConditions = new();

//     private EntityUid? _targetStation;

//     private readonly PlayerManager _playerSystem = new();

//     private readonly Dictionary<string, IPlayerSession> _cultistsPlayers = new();

//     private RoundState _roundState = RoundState.None;
//     public override void Initialize()
//     {
//         base.Initialize();

//         SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
//         SubscribeLocalEvent<CultistComponent, MobStateChangedEvent>(OnMobStateChanged);
//         SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
//         SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
//         SubscribeLocalEvent<NarsiSystem.NarsiHasBeenSummonedEvent>(OnNarsiSummoned);
//         SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
//     }

//     private void OnComponentInit(EntityUid uid, CultistComponent component, ComponentAdd args)
//     {
//         TryComp<MindComponent>(uid, out var mind);
//         if (mind == null || mind.Mind == null || !mind.Mind.TryGetSession(out var session))
//             return;
//         var name = MetaData(mind.Owner).EntityName;
//         if (_cultistsPlayers.ContainsKey(name))
//             return;
//         MakeCultist(mind.Mind);
//         _cultistsPlayers.Add(name, session);
//     }

//     private void OnNarsiSummoned(NarsiSystem.NarsiHasBeenSummonedEvent ev)
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {

//             if (_roundState == RoundState.None || _roundState == RoundState.Ended)
//                 return;

//             _roundState = RoundState.Ended;
//             foreach (var player in Filter.GetAllPlayers())
//             {
//                 _audioSystem.PlayGlobal(cultComp.NarsiRisesSound, player);
//                 _audioSystem.PlayGlobal(cultComp.IAmHereSound, player);

//             }

//             var currentStationUid = _stationSystem.GetOwningStation(ev.Uid);
//             if (currentStationUid != null)
//             {
//                 if (TryComp(_targetStation, out StationDataComponent? data))
//                 {
//                     foreach (var grid in data.Grids)
//                     {
//                         if (grid != currentStationUid)
//                         {
//                             continue;
//                         }

//                         _winConditions.Add(WinCondition.NarsiWasSummonedOnCorrectStation);
//                         RuleWinType = WinType.CultMajor;
//                         return;
//                     }
//                 }

//                 _winConditions.Add(WinCondition.NarsiWasSummonedOnIncorrectLocation);
//             }
//             else
//             {

//                 _winConditions.Add(WinCondition.NarsiWasSummonedOnIncorrectLocation);
//             }

//             RuleWinType = WinType.CultMajor;
//         }
//     }

//     private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
//     {
//         switch (ev.New)
//         {
//             case GameRunLevel.InRound:
//                 OnRoundStart();
//                 break;
//             case GameRunLevel.PostRound:
//                 OnRoundEnd();
//                 break;
//         }
//     }

//     private List<IPlayerSession> FindPotentialCultists(RulePlayerJobsAssignedEvent ev)
//     {
//         var list = new List<IPlayerSession>(ev.Players).Where(x =>
//             x.Data.ContentData()?.Mind?.AllRoles.All(role => role is not Job { CanBeAntag: false }) ?? false
//         ).ToList();

//         var prefList = new List<IPlayerSession>();

//         foreach (var player in list)
//         {
//             if (!ev.Profiles.ContainsKey(player.UserId))
//             {
//                 continue;
//             }

//             var profile = ev.Profiles[player.UserId];
//             if (profile.AntagPreferences.Contains(CultistPrototypeId))
//             {
//                 prefList.Add(player);
//             }
//         }

//         if (prefList.Count == 0)
//         {
//             Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
//             prefList = list;
//         }

//         return prefList;
//     }

//     private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {
//             var numTraitors = MathHelper.Clamp(ev.Players.Length / cultComp.PlayersPerCultist, 2, cultComp.MaxCultists);

//             var traitorPool = FindPotentialCultists(ev);
//             var selectedTraitors = PickTraitors(numTraitors, traitorPool);

//             foreach (var traitor in selectedTraitors)
//             {
//                 TryComp<MindComponent>(traitor.AttachedEntity, out var mind);
//                 if (mind == null || mind.Mind == null || !mind.Mind.TryGetSession(out var session))
//                     continue;
//                 var name = MetaData(mind.Owner).EntityName;
//                 if (_cultistsPlayers.ContainsKey(name))
//                     continue;
//                 _cultistsPlayers.Add(name, session);
//                 MakeCultist(mind.Mind);
//             }
//         }
//     }

//     private List<IPlayerSession> PickTraitors(int traitorCount, List<IPlayerSession> traitorPool)
//     {
//         var results = new List<IPlayerSession>(traitorCount);
//         if (traitorPool.Count == 0)
//         {
//             return results;
//         }
//         if (traitorPool.Count < traitorCount)
//         {
//             Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
//             return traitorPool;
//         }

//         for (var i = 0; i < traitorCount; i++)
//         {
//             results.Add(_random.PickAndTake(traitorPool));
//             Logger.InfoS("preset", "Selected a preferred traitor.");
//         }
//         return results;
//     }

//     private void OnRoundStart()
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {
//             _targetStation = _stationSystem.Stations.FirstOrNull();

//             if (_targetStation == null)
//                 return;

//             foreach (var cultist in EntityQuery<CultistComponent>())
//             {
//                 if (!TryComp<ActorComponent>(cultist.Owner, out _))
//                 {
//                     continue;
//                 }

//                 TryComp<MindComponent>(cultist.Owner, out var mind);
//                 if (mind == null || mind.Mind == null || !mind.Mind.TryGetSession(out var session))
//                     continue;
//                 var name = MetaData(mind.Owner).EntityName;
//                 if (_cultistsPlayers.ContainsKey(name))
//                     continue;
//                 _cultistsPlayers.Add(name, session);
//                 MakeCultist(mind.Mind);

//             }
//         }
//     }

//     private void OnRoundEnd()
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {
//             if (RuleWinType == WinType.CultMajor || RuleWinType == WinType.CrewMajor)
//             {
//                 return;
//             }

//             foreach (var (_, narsiTransform) in EntityManager.EntityQuery<NarsiComponent, TransformComponent>(true))
//             {

//                 if (narsiTransform.GridUid == null || _targetStation == null)
//                 {
//                     continue;
//                 }

//                 if (!TryComp(_targetStation.Value, out StationDataComponent? data))
//                 {
//                     continue;
//                 }

//                 foreach (var grid in data.Grids)
//                 {
//                     if (grid != narsiTransform.GridUid)
//                     {
//                         continue;
//                     }

//                     _winConditions.Add(WinCondition.NarsiWasSummonedOnCorrectStation);
//                     RuleWinType = WinType.CultMajor;
//                     return;
//                 }
//             }

//             var allAlive = true;
//             var aliveCounter = 0;

//             foreach (var (_, state) in EntityQuery<CultistComponent, MobStateComponent>())
//             {
//                 if (state.CurrentState is MobState.Alive)
//                 {
//                     ++aliveCounter;
//                     continue;
//                 }

//                 allAlive = false;
//                 break;
//             }

//             if (allAlive)
//             {
//                 RuleWinType = WinType.CultMajor;
//                 _winConditions.Add(WinCondition.AllCultistsAlive);
//                 return;
//             }

//             if (_cultistsPlayers.Count >= 9 && aliveCounter >= 9)
//             {
//                 _winConditions.Add(WinCondition.MoreThanNeededCultistsAlive);
//                 return;
//             }
//             _winConditions.Add(WinCondition.SomeCultistsAlive);
//         }
//     }

//     private void OnRoundEndText(RoundEndTextAppendEvent ev)
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {
//             var winText = Loc.GetString($"cultist-{_winType.ToString().ToLower()}");

//             ev.AddLine(winText);

//             foreach (var cond in _winConditions)
//             {
//                 var text = Loc.GetString($"cultist-cond-{cond.ToString().ToLower()}");

//                 ev.AddLine(text);
//             }

//             ev.AddLine(Loc.GetString("cultist-list-start"));
//             foreach (var (name, session) in _cultistsPlayers)
//             {
//                 var listing = Loc.GetString("cultist-list-name", ("name", name), ("user", session.Name));
//                 ev.AddLine(listing);
//             }
//         }
//     }

//     public void CheckRoundShouldEnd()
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {
//             if (RuleWinType == WinType.CrewMajor || RuleWinType == WinType.CultMajor)
//                 return;

//             // If there are any nuclear bombs that are active, immediately return. We're not over yet.
//             if (EntityQuery<NarsiComponent>().Any())
//             {
//                 return;
//             }

//             // Check if there are nuke operatives still alive on the same map as the shuttle,
//             // or on the same map as the station.
//             // If there are, the round can continue.
//             var cultists = EntityQuery<CultistComponent, MobStateComponent, TransformComponent>(true);
//             var cultistsAlive = cultists
//                 .Where(ent =>
//                     ent.Item3.GridUid == _targetStation)
//                 .Any(ent => ent.Item2.CurrentState == MobState.Alive && ent.Item1.Running);

//             if (cultistsAlive)
//                 return; // There are living operatives than can access the shuttle, or are still on the station's map.

//             _winConditions.Add(WinCondition.AllCultistsDead);

//             RuleWinType = WinType.CrewMajor;
//         }
//     }

//     private void OnMobStateChanged(EntityUid uid, CultistComponent component, MobStateChangedEvent ev)
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var componentUid, out var cultComp, out var gameRule))
//         {
//             if (ev.NewMobState == MobState.Dead)
//                 CheckRoundShouldEnd();
//         }
//     }

//     //For admins forcing someone to nukeOps.
//     public void MakeCultist(Mind.Mind mind)
//     {
//         if (mind.OwnedComponent == null)
//             return;

//         if (!HasComp<CultistComponent>(mind.OwnedComponent.Owner))
//             AddComp<CultistComponent>(mind.OwnedComponent.Owner);
//     }

//     private void OnStartAttempt(RoundStartAttemptEvent ev)
//     {
//         var query = EntityQueryEnumerator<CultistRuleComponent, GameRuleComponent>();
//         while (query.MoveNext(out var uid, out var cultComp, out var gameRule))
//         {
//             var minPlayers = cultComp.MinPlayers;

//             if (!ev.Forced && ev.Players.Length < minPlayers)
//             {
//                 _chatManager.DispatchServerAnnouncement(Loc.GetString("cultists-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
//                 ev.Cancel();
//                 return;
//             }

//             if (ev.Players.Length != 0)
//                 return;

//             _chatManager.DispatchServerAnnouncement(Loc.GetString("cultists-no-one-ready"));
//             ev.Cancel();
//         }
//     }

//     protected override void Started(EntityUid uid, CultistRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
//     {
//         _roundState = RoundState.Started;
//         RuleWinType = WinType.Neutral;
//         _winConditions.Clear();

//         _cultistsPlayers.Clear();

//         var query = EntityQuery<CultistComponent, MindComponent>(true);
//         foreach (var (_, mindComp) in query)
//         {
//             if (mindComp.Mind == null || !mindComp.Mind.TryGetSession(out var session))
//                 continue;

//             var name = MetaData(mindComp.Owner).EntityName;
//             if (_cultistsPlayers.ContainsKey(name))
//                 continue;

//             MakeCultist(mindComp.Mind);
//             _cultistsPlayers.Add(name, session);
//         }
//     }
// }
