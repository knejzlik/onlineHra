using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class WhisperCommand : ICommand
{
    private readonly Server? _server;

    public WhisperCommand(Server? server = null)
    {
        _server = server;
    }

    public WhisperCommand() : this(null) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: whisper <player name> <message> - sends a private message to one player";
        }

        if (_server == null)
        {
            return "Server not available for whisper.";
        }

        var parts = args.Split(' ', 2);
        if (parts.Length < 2)
        {
            return "Usage: whisper <player name> <message>";
        }

        var targetName = parts[0].ToLower().Trim();
        var message = parts[1];

        var allPlayers = _server.GetAllPlayers();
        var targetPlayer = allPlayers.FirstOrDefault(p => p.State.Username.ToLower().Contains(targetName) && p != player);

        if (targetPlayer == null)
        {
            return $"Player '{parts[0]}' not found in the dungeon.";
        }

        await targetPlayer.SendMessageAsync($"\n[WHISPER from {player.State.Username}]: {message}");
        return $"You whisper to {targetPlayer.State.Username}: \"{message}\"";
    }
}
