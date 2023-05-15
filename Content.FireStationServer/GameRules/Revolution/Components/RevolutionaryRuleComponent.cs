using System.Collections.Generic;
using Content.Server.Mind;
using Robust.Shared.Analyzers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.FireStationServer.GameRules.Components;

[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed class RevolutionaryRuleComponent : Component
{
    [DataField("minPlayers")]
    public int MinPlayers = 20;

    [DataField("maxHeads")]
    public int MaxHeads = 2;

    [DataField("HeadsPerRevoHead")]
    public int HeadsPerRevoHead = 1;

    [DataField("aliveRevols")]
    public Dictionary<Mind, bool> _aliveRevoHeads = new();

    [DataField("aliveCommands")]
    public Dictionary<Mind, bool> _aliveCommandHeads = new();

    [DataField("RevsWon")]
    public bool _revsWon;

    [DataField("RevsAddedSound")]
    public readonly SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");

    [DataField("RevolHeadNames")]
    public Dictionary<string, string> _revolutionHeadNames = new();

    [DataField("RevolutionaryHead")]
    public string RevolutionaryHeadPrototypeId = "RevolutionaryHead";
}
