using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class ExploreCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly LoggingService _logger;

    public ExploreCommand(WorldService worldService, LoggingService logger)
    {
        _worldService = worldService;
        _logger = logger;
    }

    public ExploreCommand() : this(new WorldService(), new LoggingService()) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService)
    {
        var ws = worldService ?? _worldService;
        
        if (player == null)
        {
            return "Error: Player not found.";
        }

        // Ensure player's CurrentRoomId is synced with State
        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }
        
        // Validate room exists, reset to start if not
        var room = ws.GetRoom(player.CurrentRoomId);
        if (room == null)
        {
            player.CurrentRoomId = "start";
            player.State.CurrentRoomId = "start";
            room = ws.GetRoom("start");
            
            if (room == null)
            {
                return "Error: Room system not initialized. Check rooms.json file.";
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"=== {room.Name} ===");
        sb.AppendLine(room.Description);
        sb.AppendLine();

        // Exits
        sb.AppendLine("Exits:");
        if (room.Exits.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var exit in room.Exits)
            {
                var directionName = GetDirectionName(exit.Key);
                sb.AppendLine($"  {directionName} -> {exit.Value}");
            }
        }
        sb.AppendLine();

        // Items
        sb.AppendLine("Items here:");
        if (room.Items.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var itemId in room.Items)
            {
                var item = ws.GetItem(itemId);
                if (item != null)
                {
                    sb.AppendLine($"  - {item.Name} ({item.Description})");
                }
            }
        }
        sb.AppendLine();

        // NPCs
        sb.AppendLine("Characters here:");
        if (room.Npcs.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var npcId in room.Npcs)
            {
                var npc = ws.GetNpc(npcId);
                if (npc != null)
                {
                    sb.AppendLine($"  - {npc.Name} ({npc.Description})");
                }
            }
        }
        sb.AppendLine();

        // Other players
        sb.AppendLine("Other players here:");
        sb.AppendLine("  (no other players visible)");

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
