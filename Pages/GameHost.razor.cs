using System;
using System.Collections.Generic;
using System.Numerics;
using CardGame.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace CardGame.Pages;

public partial class GameHost(ILogger<GameHost> logger, GameService gameService) : ComponentBase, IDisposable
{
    private readonly Vector2 AimScale = new(64.0f, 64.0f);
    
    private Game Game { get; } = gameService.CreateGame();

    private Dictionary<Guid, Player> Players { get; } = [];

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Game.PlayerAction += OnPlayerAction;
        }

        base.OnAfterRender(firstRender);
    }

    private void OnPlayerAction(PlayerAction playerAction)
    {
        logger.LogDebug("PlayerAction: {Values}", playerAction);
        switch (playerAction)
        {
            case PlayerAction.Join(var id):
                OnPlayerJoin(id);
                break;
            case PlayerAction.Aim(var id, var angle):
                OnPlayerAim(id, angle);
                break;
            case PlayerAction.Leave(var id):
                OnPlayerLeave(id);
                break;
            case var action:
                logger.LogWarning("Unhandled PlayerAction: {Values}", action);
                break;
        }
    }

    private void OnPlayerJoin(Guid playerId)
    {
        Players[playerId] = new(GetRandomColor(playerId), Vector2.Zero);
        _ = InvokeAsync(StateHasChanged);
    }

    private static int GetRandomColor(Guid playerId) => playerId.GetHashCode() % 360;

    private void OnPlayerAim(Guid playerId, Vector2 angle)
    {
        if (!Players.TryGetValue(playerId, out var player))
        {
            return;
        }

        Players[playerId] = player with { Angle = angle * AimScale };
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnPlayerLeave(Guid playerId)
    {
        Players.Remove(playerId);
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        gameService.DeleteGame(Game.GameId);
    }

    private record Player(int Hue, Vector2 Angle);
}
