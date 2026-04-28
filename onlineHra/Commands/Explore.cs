using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class ExploreCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public ExploreCommand(WorldService worldService, LoggingService logger, Server? server = null)
    {
        _worldService = worldService;
        _logger = logger;
        _server = server;
    }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService)
    {
        var ws = worldService ?? _worldService;

        if (player == null) return "Player error.";
        if (string.IsNullOrEmpty(player.CurrentRoomId)) player.CurrentRoomId = player.State.CurrentRoomId;

        var room = ws.GetRoom(player.CurrentRoomId);
        if (room == null) return "Room error.";

        var sb = new StringBuilder();
        sb.AppendLine($"=== {room.Name} ===");
        sb.AppendLine(room.Description);
        sb.AppendLine();

        sb.AppendLine("Exits:");
        if (room.Exits.Count == 0) sb.AppendLine("  (none)");
        else
        {
            foreach (var exit in room.Exits)
            {
                var lockedInfo = room.RequiredItems.ContainsKey(exit.Key) ? " [LOCKED]" : "";
                sb.AppendLine($"  {exit.Key.ToUpper()}{lockedInfo} -> {ws.GetRoom(exit.Value)?.Name ?? exit.Value}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("Items here:");
        if (room.Items.Count == 0) sb.AppendLine("  (none)");
        else
        {
            foreach (var itemId in room.Items)
            {
                var item = ws.GetItem(itemId);
                if (item != null) sb.AppendLine($"  - {item.Name} ({item.Description})");
            }
        }
        sb.AppendLine();

        sb.AppendLine("Characters here:");
        if (room.Npcs.Count == 0) sb.AppendLine("  (none)");
        else
        {
            foreach (var npcId in room.Npcs)
            {
                var npc = ws.GetNpc(npcId);
                if (npc != null) sb.AppendLine($"  - {npc.Name} ({npc.Description})");
            }
        }
        sb.AppendLine();

        if (_server != null)
        {
            var playersInRoom = _server.GetPlayersInRoom(player.CurrentRoomId);
            sb.AppendLine("Players here:");
            if (playersInRoom.Count <= 1) sb.AppendLine("  (no one else)");
            else
            {
                foreach (var p in playersInRoom)
                {
                    if (p != player) sb.AppendLine($"  - {p.State.Username}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}