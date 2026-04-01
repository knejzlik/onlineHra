using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class SayCommand : ICommand
{
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public SayCommand(LoggingService logger, Server? server = null)
    {
        _logger = logger;
        _server = server;
    }

    public SayCommand() : this(new LoggingService(), null) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: say <message> - sends a message to all players in the same room";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var ws = worldService ?? new WorldService();
        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null)
        {
            player.CurrentRoomId = "start";
            player.State.CurrentRoomId = "start";
            currentRoom = ws.GetRoom("start");
            
            if (currentRoom == null)
            {
                return "Error: Room system not initialized. Check rooms.json file.";
            }
        }

        var result = new StringBuilder();
        result.AppendLine($"You say: \"{args}\"");
        
        if (_server != null)
        {
            var playersInRoom = _server.GetPlayersInRoom(player.CurrentRoomId);
            foreach (var p in playersInRoom)
            {
                if (p != player)
                {
                    await p.SendMessageAsync($"\n[{player.State.Username}] says: \"{args}\"");
                    result.AppendLine($"Sent to: {p.State.Username}");
                }
            }
        }

        return result.ToString();
    }
}
