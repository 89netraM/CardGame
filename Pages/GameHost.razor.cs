using System;
using CardGame.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace CardGame.Pages;

public partial class GameHost(ILogger<GameHost> logger, GameService gameService) : ComponentBase, IDisposable
{
    private Game Game { get; } = gameService.CreateGame();

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
        logger.LogInformation("PlayerAction: {Values}", playerAction);
    }

    public void Dispose()
    {
        gameService.DeleteGame(Game.GameId);
    }
}
