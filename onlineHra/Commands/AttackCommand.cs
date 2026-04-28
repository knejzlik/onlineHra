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
            return "Usage: attack <character>";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null) return "Room error.";

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
            return $"There is no '{args}' here.";
        }

        if (targetNpc.IsDead)
        {
            return $"{targetNpc.Name} is already dead.";
        }

        if (targetNpc.Hp <= 0)
        {
            return $"You have no reason to attack {targetNpc.Name}.";
        }

        if (!player.State.Inventory.Contains("sharp_sword"))
        {
            return $"You tried to attack with your bare hands, but {targetNpc.Name} easily repelled you. You need a weapon!";
        }

        targetNpc.Hp -= 10;
        targetNpc.IsDead = true;

        if (targetNpc.Id == "boss_guard")
        {
            currentRoom.Items.Add("dragon_scale");
        }

        currentRoom.Npcs.Remove(targetNpc.Id);

        _logger.LogCommand(player.State.Username, $"attack {targetNpc.Id}");

        if (_server != null)
        {
            var playersInRoom = _server.GetPlayersInRoom(player.CurrentRoomId);
            foreach (var p in playersInRoom)
            {
                if (p != player)
                {
                    await p.SendMessageAsync($"\n{player.State.Username} strikes down {targetNpc.Name}!");
                }
            }
        }

        return $"You attacked with your sword. {targetNpc.Name} roared and fell dead to the ground! A Dragon Scale dropped from the body.";
    }
}