using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class TalkCommand : ICommand
{
    private readonly WorldService _worldService;

    public TalkCommand(WorldService worldService)
    {
        _worldService = worldService;
    }

    public TalkCommand() : this(new WorldService()) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        var ws = worldService ?? _worldService;

        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: talk <character name> [topic]";
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

        var parts = args.Split(' ', 2);
        var targetName = parts[0].ToLower().Trim();
        var topic = parts.Length > 1 ? parts[1].ToLower().Trim() : "default";
        
        // Find NPC by name
        Npc? targetNpc = null;
        foreach (var npcId in currentRoom.Npcs)
        {
            var npc = ws.GetNpc(npcId);
            if (npc != null && npc.Name.ToLower().Contains(targetName))
            {
                targetNpc = npc;
                break;
            }
        }

        if (targetNpc == null)
        {
            return $"There is no '{parts[0]}' here to talk to.";
        }

        // Try to find dialog response by topic, fallback to default
        var response = targetNpc.Dialogs.FirstOrDefault(d => d.Trigger.ToLower() == topic)?.Response 
            ?? targetNpc.Dialogs.FirstOrDefault(d => d.Trigger == "default")?.Response 
            ?? $"{targetNpc.Name} doesn't have anything to say about that.";

        return $"{targetNpc.Name} says: \"{response}\"";
    }
}
