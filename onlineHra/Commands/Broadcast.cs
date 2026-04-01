using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class BroadcastCommand : ICommand
{
    private readonly Server? _server;

    public BroadcastCommand(Server? server = null)
    {
        _server = server;
    }

    public BroadcastCommand() : this(null) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: broadcast <message> - sends a message to ALL players in the dungeon";
        }

        if (_server == null)
        {
            return "Server not available for broadcast.";
        }

        var allPlayers = _server.GetAllPlayers();
        var sentCount = 0;

        foreach (var p in allPlayers)
        {
            if (p != player)
            {
                await p.SendMessageAsync($"\n[BROADCAST from {player.State.Username}]: {args}");
                sentCount++;
            }
        }

        return $"You broadcast to everyone in the dungeon: \"{args}\"\nDelivered to {sentCount} player(s).";
    }
}
