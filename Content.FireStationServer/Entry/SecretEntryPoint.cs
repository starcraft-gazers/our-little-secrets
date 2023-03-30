using Content.FireStationServer.IoC;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Content.FireStationServer.Entry;

public sealed class SecretEntryPoint : GameServer
{
    public override void Init()
    {
        base.Init();
        SecretIoCRegister.RegisterIoC();
        IoCManager.BuildGraph();
    }
}
