using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using CardGame.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using QRCoder;

namespace CardGame.Pages;

public partial class GameHost(ILogger<GameHost> logger, NavigationManager navigationManager, IJSRuntime jsRuntime, GameService gameService)
    : ComponentBase, IDisposable
{
    private readonly Vector2 AimScale = new(64.0f, 64.0f);

    private Game Game { get; } = gameService.CreateGame();

    private Dictionary<Guid, Player> Players { get; } = [];

    private bool[] Ninjas { get; } = [true, true, true, true, true, true, true];

    private MarkupString? PlayerUrlQrCode;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Game.PlayerAction += OnPlayerAction;

            var playerUrl = navigationManager.ToAbsoluteUri($"/game/{Game.GameId}");
            using var qrCodeGenerator = new QRCodeGenerator();
            var qrCode = qrCodeGenerator.CreateQrCode(playerUrl.ToString(), QRCodeGenerator.ECCLevel.M);
            var svgQrCode = new SvgQRCode(qrCode);
            PlayerUrlQrCode = new(svgQrCode.GetGraphic(
                1, darkColorHex: "#eab8b2", lightColorHex: "#000735", sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute));
            StateHasChanged();
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
            case PlayerAction.Throw(var id):
                _ = OnPlayerThrow(id);
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
        Players[playerId] = new(GetRandomColor(playerId), Vector2.Zero, 0);
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

    private async Task OnPlayerThrow(Guid playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
        {
            return;
        }

        var hitId = await jsRuntime.InvokeAsync<int?>("throwCard", playerId.ToString());
        if (hitId is not int hit || !Ninjas[hit])
        {
            return;
        }

        Ninjas[hit] = false;
        Players[playerId] = player with { Score = player.Score + 1 };
        await InvokeAsync(StateHasChanged);
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

    private record Player(int Hue, Vector2 Angle, int Score);
}
