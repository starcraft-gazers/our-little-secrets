using Content.Server.GameTicking.Rules;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.FireStationServer.Rules;

[RegisterComponent]
[Access(typeof(SyndicateWarRuleSystem))]
public sealed class SyndicateWarSpawnerComponent : Component
{
    [DataField("name")]
    public string OperativeName = "";

    [DataField("rolePrototype")]
    public string OperativeRolePrototype = "";

    [DataField("startingGearPrototype")]
    public string OperativeStartingGear = "";
}
