using Content.FireStationServer.GameRules;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Roles;

public sealed class RevolutionaryMaker : IRevolutionaryMaker
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public void Initialize() { }

    public void MakeRevolutionary(IPlayerSession playerSession)
    {
        _entityManager.System<RevolutionaryRuleSystem>().MakeRevolHead(playerSession);
    }
}
