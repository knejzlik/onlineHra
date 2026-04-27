using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class GoCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public GoCommand(WorldService worldService, PlayerService playerService, LoggingService logger, Server? server = null)
    {
        _worldService = worldService;
        _playerService = playerService;
        _logger = logger;
        _server = server;
    }

    public GoCommand() : this(new WorldService(), new PlayerService(), new LoggingService(), null) { }

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

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

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

        var direction = args.ToLower().Trim();
        
        // Support shortcut directions
        direction = direction switch
        {
            "n" => "north",
            "s" => "south",
            "e" => "east",
            "w" => "west",
            "u" => "up",
            "d" => "down",
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
        sb.AppendLine();

        // Show items in the room
        sb.AppendLine("Items here:");
        if (targetRoom.Items.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var itemId in targetRoom.Items)
            {
                var item = ws.GetItem(itemId);
                if (item != null)
                {
                    sb.AppendLine($"  - {item.Name} ({item.Description})");
                }
            }
        }
        sb.AppendLine();

        // Show NPCs in the room
        sb.AppendLine("Characters here:");
        if (targetRoom.Npcs.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var npcId in targetRoom.Npcs)
            {
                var npc = ws.GetNpc(npcId);
                if (npc != null)
                {
                    sb.AppendLine($"  - {npc.Name} ({npc.Description})");
                }
            }
        }
        sb.AppendLine();

        if (_server != null)
        {
            var playersInRoom = _server.GetPlayersInRoom(targetRoomId);
            if (playersInRoom.Count > 0)
            {
                sb.AppendLine("Players here:");
                foreach (var p in playersInRoom)
                {
                    if (p != player)
                    {
                        sb.AppendLine($"  - {p.State.Username}");
                    }
                }
            }
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
