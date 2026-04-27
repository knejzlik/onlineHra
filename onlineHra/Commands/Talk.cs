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
            return "Pouziti: mluv <jmeno postavy> [tema]";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null) return "Chyba mistnosti.";

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
            return $"Zadny '{parts[0]}' tu neni.";
        }

        if (targetNpc.IsDead)
        {
            return $"{targetNpc.Name} je po smrti. S mrtvolami se povida tezko.";
        }

        var sb = new StringBuilder();
        var response = targetNpc.Dialogs.FirstOrDefault(d => d.Trigger.ToLower() == topic)?.Response
            ?? targetNpc.Dialogs.FirstOrDefault(d => d.Trigger == "default")?.Response
            ?? $"{targetNpc.Name} k tomu nema co rict.";

        if (targetNpc.Id == "veleknez")
        {
            bool hasEgg = player.State.Inventory.Contains("zlate_vejce");
            bool hasScale = player.State.Inventory.Contains("draci_supina");

            if (hasEgg && hasScale)
            {
                player.State.Inventory.Remove("zlate_vejce");
                player.State.Inventory.Remove("draci_supina");
                player.State.GameCompleted = true;

                sb.AppendLine($"{targetNpc.Name} rika: \"Dokazal jsi to! Ziskal jsi Zlate vejce i Draci supinu! Ritual muze zacit...\"");
                sb.AppendLine();
                sb.AppendLine("=========================================================");
                sb.AppendLine("                    GRATULUJEME!                         ");
                sb.AppendLine("          Dokoncil jsi hru a splnil svuj ukol!           ");
                sb.AppendLine("=========================================================");
                return sb.ToString();
            }
            else if (hasEgg)
            {
                response = "Mas Zlate vejce, to je skvele! Ale k dokonceni ritualu stale potrebuji Draci supinu z bosse.";
            }
            else if (hasScale)
            {
                response = "Mas Draci supinu, to je skvele! Ale k dokonceni ritualu stale potrebuji Zlate vejce od draka.";
            }
        }

        sb.AppendLine($"{targetNpc.Name} rika: \"{response}\"");
        sb.AppendLine();

        var availableTopics = targetNpc.Dialogs.Where(d => d.Trigger != "default").Select(d => d.Trigger).ToList();
        if (availableTopics.Any())
        {
            sb.AppendLine($"Muze ti rict vice o: {string.Join(", ", availableTopics)}");
            sb.AppendLine($"(Napis: mluv {targetName} <tema>)");
        }

        return sb.ToString();
    }
}