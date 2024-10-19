using System;
using CardGame.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace CardGame.Pages;

public partial class GamePlayer(ILogger<GamePlayer> logger, GameService gameService) : ComponentBase, IDisposable
{
    [Parameter]
    public required string GameId { get; init; }
    private (Game, Guid)? Game { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Game = gameService.JoinGame(GameId);
            if (Game is var (game, _))
            {
                game.HostAction += OnHostAction;
                StateHasChanged();
            }
        }
        base.OnAfterRender(firstRender);
    }

    private void Throw()
    {
        if (Game is not var (game, playerId))
        {
            return;
        }

        game.Throw(playerId);
    }

    private void OnHostAction(HostAction hostAction)
    {
        logger.LogInformation("HostAction: {Values}", hostAction);
    }

    public void Dispose()
    {
        if (Game is not var (game, playerId))
        {
            return;
        }

        game.HostAction -= OnHostAction;
        game.Leave(playerId);
    }
}
