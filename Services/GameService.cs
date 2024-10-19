using System;
using System.Collections.Concurrent;
using System.Numerics;

namespace CardGame.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, Game> games = [];

    public Game CreateGame()
    {
        Game game;
        do
        {
            game = new Game(CreateGameId());
        } while (!games.TryAdd(game.GameId, game));
        return game;
    }

    public (Game, Guid)? JoinGame(string gameId)
    {
        if (!games.TryGetValue(gameId, out var game))
        {
            return null;
        }

        var playerId = game.Join();
        return (game, playerId);
    }

    public void DeleteGame(string gameId)
    {
        if (games.TryRemove(gameId, out var game))
        {
            game.End();
        }
    }

    private static string CreateGameId()
    {
        Span<byte> gameIdBytes = stackalloc byte[4];
        Random.Shared.NextBytes(gameIdBytes);
        return Convert.ToHexString(gameIdBytes);
    }
}

public class Game(string gameId)
{
    public string GameId { get; } = gameId;

    public event Action<PlayerAction>? PlayerAction;

    public event Action<HostAction>? HostAction;

    public Guid Join()
    {
        var playerId = Guid.NewGuid();
        PlayerAction?.Invoke(new PlayerAction.Join(playerId));
        return playerId;
    }

    public void Aim(Guid playerId, Vector2 angle) => PlayerAction?.Invoke(new PlayerAction.Aim(playerId, angle));

    public void Throw(Guid playerId) => PlayerAction?.Invoke(new PlayerAction.Throw(playerId));

    public void Leave(Guid playerId) => PlayerAction?.Invoke(new PlayerAction.Leave(playerId));

    public void End() => HostAction?.Invoke(new HostAction.End());
}

public abstract record PlayerAction(Guid PlayerId)
{
    public record Join(Guid PlayerId) : PlayerAction(PlayerId);

    public record Aim(Guid PlayerId, Vector2 Angle) : PlayerAction(PlayerId);

    public record Throw(Guid PlayerId) : PlayerAction(PlayerId);

    public record Leave(Guid PlayerId) : PlayerAction(PlayerId);
}

public abstract record HostAction
{
    public record End : HostAction;
}
