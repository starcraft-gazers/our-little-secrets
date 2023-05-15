using Robust.Shared.GameObjects;
using Content.FireStationServer.GameRules.Revolution.Components;
using System;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Server.Player;
using Content.Server.Players;
using Robust.Shared.Players;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Server.Chat.Managers;
using Robust.Shared.Localization;
using System.Drawing;
using Content.Shared.Mobs;

namespace Content.FireStationServer.GameRules.Revolution;

public sealed class RevolutionarySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    private const string RevolutionaryPrototypeId = "Revolutionary";
    public override void Initialize()
    {
        SubscribeLocalEvent<RevolutionaryComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<RevolutionaryComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
    }

    private void OnComponentRemove(EntityUid uid, RevolutionaryComponent component, ComponentRemove args)
    {
        MakeSuspend(uid);
    }

    private void OnComponentInit(EntityUid uid, RevolutionaryComponent component, ComponentInit args)
    {
        if (!TryComp<MindComponent>(uid, out var targetmindcomp) || targetmindcomp.Mind is null)
            return;

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryPrototypeId);
        var revoRole = new TraitorRole(targetmindcomp.Mind, antagPrototype);
        targetmindcomp.Mind.AddRole(revoRole);

        // var filter = Filter.Empty().AddWhere(s => HasTraitorRole(s));

        // SoundSystem.Play("/Audio/Magic/staff_chaos.ogg", filter, AudioParams.Default);
        GreetConvert(targetmindcomp);
    }

    private void GreetConvert(MindComponent mindComponent)
    {
        var message = "Вас загипнотизировали и вы стали революционером! Ваша задача избавиться от всего командования на станции! Слушайтесь вашего командира революции.";
        var messageWrapper = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        if (mindComponent.Mind?.OwnedEntity is null)
            return;

        if (mindComponent.Mind.Session == null)
            return;

        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, messageWrapper, default, false, mindComponent.Mind.Session.ConnectedClient, Color.Red);
    }

    private bool HasTraitorRole(ICommonSession session)
    {
        if (session! is IPlayerSession)
            return false;


        return ((IPlayerSession) session).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false;
    }

    private void OnMobStateChangedEvent(EntityUid uid, RevolutionaryComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        MakeSuspend(uid);
    }

    private void MakeSuspend(EntityUid uid)
    {
        if (!TryComp<MindComponent>(uid, out var targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null)
            return;

        if (!targetmindcomp.Mind.HasRole<TraitorRole>())
            return;

        foreach (var role in targetmindcomp.Mind.AllRoles)
        {
            if (role is not TraitorRole traitor)
                continue;

            if (traitor.Prototype.ID == RevolutionaryPrototypeId)
            {
                SendSuspendText(targetmindcomp.Mind.Session);
                targetmindcomp.Mind.RemoveRole(traitor);
                break;
            }
        }
    }

    private void SendSuspendText(IPlayerSession? session)
    {
        if (session == null)
            return;

        var message = "Вы больше не революционер!\nЕсли вас клонируют, не смейте поддерживать революционеров, иначе получите крупный бан!\nВас могут загипнотизировать заново, но вы не имете право просить об этом.\nВы потеряли память";
        var messageWrapper = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, message, messageWrapper, default, false, session.ConnectedClient, Color.Green);
    }

    public void MakeRevolutionary(EntityUid target, string commanderName)
    {
        if (!CanMakeTargetRevol(target))
            return;

        var component = EnsureComp<RevolutionaryComponent>(target);
        component.RevolutionaryHeadName = commanderName;
    }

    public bool CanMakeTargetRevol(EntityUid target)
    {
        if (!TryComp<MindComponent>(target, out var mind) || mind.Mind is null || mind.Mind.CurrentJob is null)
            return false;

        if (HasComp<RevolutionaryComponent>(target) || !mind.Mind.CurrentJob.Prototype.CanBeAntag || mind.Mind.HasRole<TraitorRole>())
            return false;

        return true;
    }
}
