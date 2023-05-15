using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Traitor;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Content.Shared.Mobs;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Mobs.Systems;
using Robust.Shared.IoC;
using Content.Server.GameTicking.Rules.Components;
using System.Collections.Generic;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Log;
using Robust.Shared.GameObjects;
using Content.Server.GameTicking.Rules;
using Content.FireStationServer.GameRules.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.FireStationServer.GameRules.Revolution.Components;

namespace Content.FireStationServer.GameRules;

public sealed class RevolutionaryRuleSystem : GameRuleSystem<RevolutionaryRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(GetHead);
    }

    private void GetHead(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revolComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            var mind = ev.Player.Data.ContentData()?.Mind;
            if (mind is null || mind.CurrentJob is null)
                return;

            foreach (var department in mind.CurrentJob.Prototype.Access)
            {
                if (department == "Command")
                {
                    revolComp._aliveCommandHeads.Add(mind, true);
                    return;
                }
            }

            foreach (var accessgroups in mind!.CurrentJob.Prototype.AccessGroups)
            {
                if (accessgroups == "AllAccess")
                {
                    revolComp._aliveCommandHeads.Add(mind, true);
                    return;
                }
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revolComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            ev.AddLine(revolComp._revsWon ? "Революционеры победили!" : "Экипаж одержал победу!");
            ev.AddLine("Список командиров революции");
            foreach (var player in revolComp._revolutionHeadNames)
            {
                ev.AddLine(player.Key + " (" + player.Value + ")");
            }
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revolComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            var headsPerRevoHead = revolComp.HeadsPerRevoHead;
            var maxRevoHeads = revolComp.MaxHeads;

            var list = new List<IPlayerSession>(ev.Players)
                .Where(x => x.Data.ContentData()?.Mind?.AllRoles
                .All(role => role is not Job { CanBeAntag: false }) ?? false)
                .ToList();

            var prefList = new List<IPlayerSession>();

            foreach (var player in list)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                {
                    continue;
                }

                var profile = ev.Profiles[player.UserId];
                if (profile.AntagPreferences.Contains(revolComp.RevolutionaryHeadPrototypeId))
                {
                    prefList.Add(player);
                }
            }

            var numRevoHeads = MathHelper.Clamp(revolComp._aliveCommandHeads.Count / headsPerRevoHead, 1, maxRevoHeads);

            for (var i = 0; i < numRevoHeads; i++)
            {
                IPlayerSession revoHead;
                if (prefList.Count == 0)
                {
                    if (list.Count == 0)
                    {
                        Logger.InfoS("preset", "Insufficient preffered players ready to fill up with revo heads, stopping the selection.");
                        break;
                    }
                    revoHead = _random.PickAndTake(list);
                    Logger.InfoS("preset", "Insufficient preferred revo heads, picking at random.");
                }
                else
                {
                    revoHead = _random.PickAndTake(prefList);
                    list.Remove(revoHead);
                    Logger.InfoS("preset", "Selected a preferred revo head.");
                }

                MakeRevolHead(revoHead);
            }

            // var filter = Filter.Empty().AddWhere(s => ((IPlayerSession) s).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false);
            // SoundSystem.Play(revolComp._addedSound.GetSound(), filter, AudioParams.Default);
        }
    }

    public void MakeRevolHead(IPlayerSession session)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revolComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            var mind = session.ContentData()?.Mind;
            if (mind is null)
            {
                Logger.ErrorS("preset", "Failed getting mind for picked revo head.");
                return;
            }

            MakeRevolHead(mind, revolComp);
        }
    }
    private void MakeRevolHead(Mind mind, RevolutionaryRuleComponent revolComp)
    {
        var antagPrototype = _prototypeManager.Index<AntagPrototype>(revolComp.RevolutionaryHeadPrototypeId);
        var revoHeadRole = new TraitorRole(mind, antagPrototype);

        mind.AddRole(revoHeadRole);
        revolComp._aliveRevoHeads.Add(mind, true);

        if (mind.OwnedEntity is null)
            return;

        var message = "Вы один из командиров революции. Ваша задача убить все командование станции. Используйте флеш гранату в вашем рюкзаке, чтобы завербовать членов экипажа.";
        var messageWrapper = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _stationSpawningSystem.EquipStartingGear(mind.OwnedEntity.Value, _prototypeManager.Index<StartingGearPrototype>("RevoHeadGear"), null);

        if (mind.Session == null)
            return;

        var inCharacterName = mind.CharacterName;
        if (inCharacterName != null)
            revolComp._revolutionHeadNames.Add(inCharacterName, mind.Session.Name);

        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message,
           messageWrapper, default, false, mind.Session.ConnectedClient, Color.Red);

        EnsureComp<RevolutionaryHeadComponent>((EntityUid) mind.OwnedEntity);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revolComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (revolComp._aliveRevoHeads.TryFirstOrNull(x => x.Key is not null && x.Key.OwnedEntity == ev.Target, out var revohead))
            {
                revolComp._aliveRevoHeads[revohead.Value.Key] = IsPlayerStable(uid);

                if (revolComp._aliveRevoHeads.Values.All(x => !x))
                {
                    _roundEndSystem.EndRound();
                }

            }

            if (revolComp._aliveCommandHeads.TryFirstOrNull(x => x.Key is not null && x.Key.OwnedEntity == ev.Target, out var staffhead))
            {
                revolComp._aliveCommandHeads[staffhead.Value.Key] = IsPlayerStable(uid);

                if (revolComp._aliveCommandHeads.Values.All(x => !x))
                {
                    revolComp._revsWon = true;
                    _roundEndSystem.EndRound();
                }
            }
        }
    }

    private bool IsPlayerStable(EntityUid uid)
    {
        return _mobStateSystem.IsAlive(uid) || _mobStateSystem.IsCritical(uid);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var revolComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            var minPlayers = revolComp.MinPlayers;
            if (!ev.Forced && (ev.Players.Length < minPlayers || ev.Players.Length == 0))
            {
                _chatManager.DispatchServerAnnouncement("Не удалось запустить режим революции. Недостаточно игроков");
                ev.Cancel();
                return;
            }
        }
    }

    protected override void Started(EntityUid uid, RevolutionaryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        component._aliveRevoHeads.Clear();
        component._aliveCommandHeads.Clear();
        component._revolutionHeadNames = new();
        component._aliveCommandHeads = new();
        component._revsWon = false;
    }
}
