using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Robust.Shared.GameObjects;
using Content.Server.Flash;
using Robust.Shared.IoC;

namespace Content.FireStationServer.GameRules.Revolution;

internal sealed class RevoHeadFlashSystem : EntitySystem
{
    [Dependency] private RevolutionarySystem _revolutionarySystem = default!;
    private const string RevolutionaryHeadPrototypeId = "RevolutionaryHead";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlashAttemptEvent>(RevoFlash);
    }

    public void RevoFlash(FlashAttemptEvent ev)
    {
        if (ev.User == ev.Target)
            return;

        if (!TryComp<MindComponent>(ev.User, out var usermindcomp) || usermindcomp.Mind is null)
            return;

        foreach (var role in usermindcomp.Mind.AllRoles)
        {
            if (role is not TraitorRole traitor || traitor.Prototype.ID != RevolutionaryHeadPrototypeId)
                continue;

            _revolutionarySystem.MakeRevolutionary(ev.Target, usermindcomp.Mind.CharacterName ?? "Unknown");
            return;
        }
    }
}

