using System;
using System.Text.Json.Nodes;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.FireStationServer.GameTickerModify;

public sealed class StatusResponseProvider : EntitySystem, IStatusResponseProvider
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public void GetStatusResponse(JsonNode jObject, GameRunLevel runLevel, DateTime roundStartDateTime)
    {

        jObject["name"] = _baseServer.ServerName;
        jObject["players"] = GetPlayersCount();
        jObject["soft_max_players"] = _cfg.GetCVar(CCVars.SoftMaxPlayers);
        jObject["run_level"] = (int) runLevel;
        if (runLevel >= GameRunLevel.InRound)
        {
            jObject["round_start_time"] = roundStartDateTime.ToString("o");
        }
    }

    private bool IsFakeNumbersEnabled()
    {
        return _cfg.GetCVar(SecretCCVars.SecretCCVars.IsFakeNumbersEnabled);
    }

    private int GetPlayersCount()
    {
        if (IsFakeNumbersEnabled())
            return GetFakeNumbers();

        return _playerManager.PlayerCount;
    }

    private int GetFakeNumbers()
    {
        var players = _playerManager.PlayerCount;
        if (players <= 10)
        {
            players += _random.Next(5, 8);
        }
        else if (players <= 30)
        {
            players += _random.Next(1, 4);
        }
        else if (players <= 40)
        {
            players += _random.Next(3, 5);
        }
        else if (players <= 60)
        {
            players += _random.Next(1, 4);
        }

        return players;
    }
}
