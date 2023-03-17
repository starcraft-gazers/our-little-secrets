using System.Linq;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.FireStationServer._Craft.Administration.Commands.Fax;

[UsedImplicitly]
public sealed class CreateCentralCommandFaxSystem : EntitySystem
{
    [Dependency] private readonly IMapManager MapManager = default!;
    [Dependency] private readonly MapLoaderSystem MapLoader = default!;
    [Dependency] private readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem TransformSystem = default!;

    private MapId _mapId = MapId.Nullspace;
    public MapId MapId
    {
        get => _mapId;
    }

    public EntityUid MapEntityUid
    {
        get => MapManager.GetMapEntityId(MapId);
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    public void CreateFaxArea()
    {
        if (MapId != MapId.Nullspace)
            return;

        var prototypes = PrototypeManager.EnumeratePrototypes<CentralCommandFaxPrototype>();
        if (prototypes == null)
            return;

        var prototype = prototypes.ToList().First();

        _mapId = MapManager.CreateMap();

        if (!MapLoader.TryLoad(_mapId, prototype.MapPath.ToString(), out _))
        {
            MapManager.DeleteMap(_mapId);
            _mapId = MapId.Nullspace;
            return;
        }

        MapManager.SetMapPaused(MapId, false);
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        _mapId = MapId.Nullspace;
    }
}
