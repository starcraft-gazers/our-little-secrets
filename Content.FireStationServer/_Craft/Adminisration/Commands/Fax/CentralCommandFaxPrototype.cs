using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.FireStationServer._Craft.Administration.Commands.Fax;

[Prototype("CentralCommandFaxPrototype")]
public sealed class CentralCommandFaxPrototype : IPrototype
{
    [IdDataField]
    public string ID {get;} = default!;

    [DataField("mapPath")]
    public string MapPath = default!;
}
