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

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        var ws = worldService ?? _worldService;

        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: talk <character> [topic]";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null) return "Room error.";

        var parts = args.Split(' ', 2);
        var targetName = parts[0].ToLower().Trim();
        var topic = parts.Length > 1 ? parts[1].ToLower().Trim() : "default";

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
            return $"There is no '{parts[0]}' here.";
        }

        if (targetNpc.IsDead)
        {
            return $"{targetNpc.Name} is dead. It's hard to talk to corpses.";
        }

        var sb = new StringBuilder();
        var response = targetNpc.Dialogs.FirstOrDefault(d => d.Trigger.ToLower() == topic)?.Response
            ?? targetNpc.Dialogs.FirstOrDefault(d => d.Trigger == "default")?.Response
            ?? $"{targetNpc.Name} doesn't have anything to say about that.";

        sb.AppendLine($"{targetNpc.Name} says: \"{response}\"");
        sb.AppendLine();

        var availableTopics = targetNpc.Dialogs.Where(d => d.Trigger != "default").Select(d => d.Trigger).ToList();
        if (availableTopics.Any())
        {
            sb.AppendLine($"Can tell you more about: {string.Join(", ", availableTopics)}");
            sb.AppendLine($"(Type: talk {targetName} <topic>)");
        }

        return sb.ToString();
    }
}