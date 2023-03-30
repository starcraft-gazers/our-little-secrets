using System.Collections.Generic;
using System.Linq;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;

namespace Content.FireStationServer.Roles;

//Фильтрация антагов для выбора
//В базовой реализации, если после фильтрации не хватает игроков, просто возвращаем текущий список
public sealed class AntagManager : IAntagManager
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public List<IPlayerSession> GetPreferedAntags(List<IPlayerSession> players, int requiredCount)
    {
        if (!_config.GetCVar(CCVars.UseAntagManager))
            return players;


        var filteredPlayers = players
            .Where(player => IsValidPlayedTime(player))
            .ToList();

        if (filteredPlayers.Count() < requiredCount)
            return players;

        return  filteredPlayers;
    }

    public Dictionary<IPlayerSession, HumanoidCharacterProfile> GetPreferedAntags(Dictionary<IPlayerSession, HumanoidCharacterProfile> players, int requiredCount)
    {
        if (!_config.GetCVar(CCVars.UseAntagManager))
            return players;

        var filtered = players
            .Where(keyValue => IsValidPlayedTime(keyValue.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        if (filtered.Count() < requiredCount)
            return players;

        return filtered;
    }

    public bool IsValidPlayedTime(IPlayerSession player)
    {
        var values = _playTimeTracking.GetTrackerTimes(player);
        var playedTime = 0d;

        foreach (var item in values)
        {
            playedTime += item.Value.TotalMinutes;
        }

        _logManager.GetSawmill("AntagManager").Info($"Player Time for {player.Name} is - {playedTime}");
        return playedTime > _config.GetCVar(CCVars.MinTimeToPlayAntag);
    }
}
