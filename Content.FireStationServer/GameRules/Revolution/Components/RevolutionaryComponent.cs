using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.FireStationServer.GameRules.Revolution.Components;

[RegisterComponent]
public sealed class RevolutionaryComponent : Component
{
    [DataField("RevolutionaryHeadName")]
    public string RevolutionaryHeadName = "Admin";
}
