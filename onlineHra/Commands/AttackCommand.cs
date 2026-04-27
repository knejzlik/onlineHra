using System.Net.Sockets;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class AttackCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public AttackCommand(WorldService worldService, PlayerService playerService, LoggingService logger, Server? server = null)
    {
        _worldService = worldService;
        _playerService = playerService;
        _logger = logger;
        _server = server;
    }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, PlayerService? playerService, string? args = null)
    {
        var ws = worldService ?? _worldService;

        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Pouziti: utoc <postava>";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null) return "Chyba mistnosti.";

        var targetName = args.ToLower().Trim();
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
            return $"Zadny '{args}' tu neni.";
        }

        if (targetNpc.IsDead)
        {
            return $"{targetNpc.Name} uz je mrtvy.";
        }

        if (targetNpc.Hp <= 0)
        {
            return $"Na {targetNpc.Name} nemas duvod utocit.";
        }

        if (!player.State.Inventory.Contains("ostry_mec"))
        {
            return $"Zkusil jsi zautocit holyma rukama, ale {targetNpc.Name} te snadno odrazil. Potrebujes zbran!";
        }

        targetNpc.Hp -= 10;
        targetNpc.IsDead = true;

        if (targetNpc.Id == "boss_strazce")
        {
            currentRoom.Items.Add("draci_supina");
        }

        _logger.LogCommand(player.State.Username, $"utoc {targetNpc.Id}");

        if (_server != null)
        {
            var playersInRoom = _server.GetPlayersInRoom(player.CurrentRoomId);
            foreach (var p in playersInRoom)
            {
                if (p != player)
                {
                    await p.SendMessageAsync($"\n{player.State.Username} smrtelne zasahl {targetNpc.Name}!");
                }
            }
        }

        return $"Zautocil jsi mecem. {targetNpc.Name} s revum padl mrtvy k zemi! Vypadla z nej Draci supina.";
    }
}