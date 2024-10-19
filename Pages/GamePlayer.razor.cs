using System;
using System.Numerics;
using System.Threading.Tasks;
using CardGame.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace CardGame.Pages;

public partial class GamePlayer(ILogger<GamePlayer> logger, GameService gameService, IJSRuntime jsRuntime)
    : ComponentBase,
        IDisposable
{
    [Parameter]
    public required string GameId { get; init; }
    private (Game, Guid)? Game { get; set; }

    private Quaternion? aimReference;
    private Quaternion? aim;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Game = gameService.JoinGame(GameId);
            if (Game is var (game, _))
            {
                game.HostAction += OnHostAction;
                StateHasChanged();

                var thisReference = DotNetObjectReference.Create(this);
                await jsRuntime.InvokeVoidAsync("addOrientationListener", thisReference);
                await jsRuntime.InvokeVoidAsync("addCardListener", thisReference);
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public void Throw()
    {
        if (Game is not var (game, playerId))
        {
            return;
        }

        game.Throw(playerId);
    }

    private void ResetAim()
    {
        aimReference = aim;
        SendAim();
    }

    [JSInvokable]
    public void OnOrientationReading(float[] quaternion)
    {
        if (quaternion is not [var x, var y, var z, var w])
        {
            logger.LogWarning("Invalid quaternion from JavaScript client: {Values}", quaternion);
            return;
        }

        aim = new Quaternion(x, y, z, w);
        aimReference ??= aim;
        SendAim();
    }

    private void SendAim()
    {
        if (Game is not var (game, playerId) || aim is null || aimReference is null)
        {
            return;
        }

        var angle = Quaternion.Inverse(aimReference.Value) * aim.Value;

        var angleX_0 = 2.0f * (angle.X * angle.Y + angle.W * angle.Z);
        var angleX_1 = angle.W * angle.W + angle.X * angle.X - angle.Y * angle.Y - angle.Z * angle.Z;
        var angleX = 0.0f;
        if (angleX_0 != 0.0f && angleX_1 != 0.0f)
        {
            angleX = float.Atan2(angleX_0, angleX_1);
        }

        var angleY_0 = 2.0f * (angle.Y * angle.Z + angle.W * angle.X);
        var angleY_1 = angle.W * angle.W - angle.X * angle.X - angle.Y * angle.Y + angle.Z * angle.Z;
        var angleY = 0.0f;
        if (angleY_0 != 0.0f && angleY_1 != 0.0f)
        {
            angleY = float.Atan2(angleY_0, angleY_1);
        }

        game.Aim(playerId, new Vector2(-angleX, -angleY));
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
