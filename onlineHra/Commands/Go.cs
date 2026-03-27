using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class GoCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;

    public GoCommand(WorldService worldService, PlayerService playerService, LoggingService logger)
    {
        _worldService = worldService;
        _playerService = playerService;
        _logger = logger;
    }

    public GoCommand() : this(new WorldService(), new PlayerService(), new LoggingService()) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, PlayerService? playerService, string? args = null)
    {
        var ws = worldService ?? _worldService;
        var ps = playerService ?? _playerService;

        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: go <direction> (north, south, east, west, up, down)";
        }

        // Ensure player's CurrentRoomId is synced with State
        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null)
        {
            // Reset to start room
            player.CurrentRoomId = "start";
            player.State.CurrentRoomId = "start";
            currentRoom = ws.GetRoom("start");
            
            if (currentRoom == null)
            {
                return "Error: Room system not initialized. Check rooms.json file.";
            }
        }

        var direction = args.ToLower().Trim();
        
        // Map Czech directions to English
        direction = direction switch
        {
            "sever" => "north",
            "jih" => "south",
            "vychod" => "east",
            "zapad" => "west",
            "nahoru" => "up",
            "dolů" => "down",
            "s" => "north",
            "j" => "south",
            "v" => "east",
            "z" => "west",
            _ => direction
        };

        if (!currentRoom.Exits.TryGetValue(direction, out var targetRoomId))
        {
            return $"Cannot go {direction} from here.";
        }

        var targetRoom = ws.GetRoom(targetRoomId);
        if (targetRoom == null)
        {
            return "Error: Target room not found.";
        }

        // Update player position
        player.CurrentRoomId = targetRoomId;
        player.State.CurrentRoomId = targetRoomId;
        ps.SavePlayer(player.State);

        _logger.LogCommand(player.State.Username, $"go {direction}");

        var sb = new StringBuilder();
        sb.AppendLine($"You go {direction}...");
        sb.AppendLine();
        sb.AppendLine($"=== {targetRoom.Name} ===");
        sb.AppendLine(targetRoom.Description);
        sb.AppendLine();
        sb.AppendLine("Exits:");
        foreach (var exit in targetRoom.Exits)
        {
            sb.AppendLine($"  {GetDirectionName(exit.Key)} -> {exit.Value}");
        }

        return sb.ToString();
    }

    private string GetDirectionName(string dir)
    {
        return dir.ToLower() switch
        {
            "north" => "North",
            "south" => "South",
            "east" => "East",
            "west" => "West",
            "up" => "Up",
            "down" => "Down",
            _ => dir
        };
    }
}
