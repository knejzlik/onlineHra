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
            return "Usage: talk <character name>";
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null)
        {
            return "Error: Current room not found.";
        }

        var targetName = args.ToLower().Trim();
        
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
            return $"There is no '{args}' here to talk to.";
        }

        // Get default dialog response
        var response = targetNpc.Dialogs.FirstOrDefault(d => d.Trigger == "default")?.Response 
            ?? $"{targetNpc.Name} doesn't have anything to say.";

        return $"{targetNpc.Name} says: \"{response}\"";
    }
}
