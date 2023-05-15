// using Content.Server.GameTicking.Rules;
// using Robust.Shared.Analyzers;
// using Robust.Shared.Audio;
// using Robust.Shared.GameObjects;
// using Robust.Shared.Serialization.Manager.Attributes;

// namespace Content.FireStationServer.GameRules.Cult.Components;

// [RegisterComponent, Access(typeof(CultistRuleSystem))]
// public sealed class CultistRuleComponent : Component
// {
//     [DataField("minPlayers")]
//     public int MinPlayers = 0;

//     [DataField("playersPerCultist")]
//     public int PlayersPerCultist = 10;

//     [DataField("maxCultists")]
//     public int MaxCultists = 10;

//     [DataField("narsiRisesSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
//     public SoundSpecifier? NarsiRisesSound = new SoundPathSpecifier("/Audio/CultSounds/narsie_rises.ogg");

//     [DataField("iAmHereSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
//     public SoundSpecifier? IAmHereSound = new SoundPathSpecifier("/Audio/CultSounds/i_am_here.ogg");
// }
