using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class TalkCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly Server? _server;

    public TalkCommand(WorldService worldService, Server? server = null)
    {
        _worldService = worldService;
        _server = server;
    }

    public TalkCommand() : this(new WorldService(), null) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        var ws = worldService ?? _worldService;

        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: talk <character name|player name> [topic]\n       whisper <player name> <message>\n       say <message> (to everyone in room)\n       broadcast <message> (to everyone in dungeon)";
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

        var parts = args.Split(' ', 2);
        var targetName = parts[0].ToLower().Trim();
        var topic = parts.Length > 1 ? parts[1].ToLower().Trim() : "default";
        
        // First check if it's a player to talk to
        if (_server != null)
        {
            var allPlayers = _server.GetAllPlayers();
            var targetPlayer = allPlayers.FirstOrDefault(p => p.State.Username.ToLower().Contains(targetName) && p != player);
            
            if (targetPlayer != null)
            {
                return $"You talk to {targetPlayer.State.Username}: \"{topic}\"\n{targetPlayer.State.Username} listens attentively.";
            }
        }
        
        // Check NPCs in current room first
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

        // If not in current room, search all NPCs in dungeon
        if (targetNpc == null)
        {
            foreach (var room in ws.GetAllRooms())
            {
                foreach (var npcId in room.Npcs)
                {
                    var npc = ws.GetNpc(npcId);
                    if (npc != null && npc.Name.ToLower().Contains(targetName))
                    {
                        targetNpc = npc;
                        break;
                    }
                }
                if (targetNpc != null) break;
            }
        }

        if (targetNpc == null)
        {
            return $"There is no '{parts[0]}' here or anywhere in the dungeon to talk to.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{targetNpc.Name} says: \"{GetResponse(targetNpc, topic)}\"");
        sb.AppendLine();
        sb.AppendLine($"Available topics with {targetNpc.Name}:");
        foreach (var dialog in targetNpc.Dialogs.Where(d => d.Trigger != "default"))
        {
            sb.AppendLine($"  * {dialog.Trigger.ToUpper()} - {GetTopicHint(dialog.Trigger)}");
        }

        return sb.ToString();
    }

    private string GetResponse(Npc npc, string topic)
    {
        var response = npc.Dialogs.FirstOrDefault(d => d.Trigger.ToLower() == topic)?.Response 
            ?? npc.Dialogs.FirstOrDefault(d => d.Trigger == "default")?.Response 
            ?? $"{npc.Name} doesn't have anything to say about that.";
        return response;
    }

    private string GetTopicHint(string topic)
    {
        return topic switch
        {
            "treasure" => "Ask about hidden treasures",
            "key" => "Inquire about keys",
            "traps" => "Learn about dangers",
            "chalice" => "Ask about the golden chalice",
            "journey" => "Discuss your quest",
            "magic" => "Learn about magical powers",
            "cavern" => "Ask about the caverns",
            "lake" => "Inquire about the underground lake",
            "work" => "Ask about their work",
            "fish" => "Discuss fishing",
            "boat" => "Ask about the boat",
            "secret" => "Inquire about secrets",
            "passage" => "Ask about secret passages",
            "help" => "Request assistance",
            "altar" => "Learn about the ancient altar",
            "relic" => "Ask about holy relics",
            "blessing" => "Request a blessing",
            _ => "General topic"
        };
    }
}
