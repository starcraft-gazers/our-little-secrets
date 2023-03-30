using Content.FireStationServer.Roles;
using Content.Server.Roles;
using Robust.Shared.IoC;

namespace Content.FireStationServer.IoC;

public static class SecretIoCRegister
{
    public static void RegisterIoC()
    {
        IoCManager.RegisterInstance<IAntagManager>(new AntagManager());
    }
}
