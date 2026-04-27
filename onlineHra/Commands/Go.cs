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
            return "Pouziti: jdi <smer> (sever, jih, vychod, zapad, nahoru, dolu)";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var oldRoomId = player.CurrentRoomId;
        var currentRoom = ws.GetRoom(oldRoomId);
        if (currentRoom == null) return "Chyba mistnosti.";

        var direction = args.ToLower().Trim();

        direction = direction switch
        {
            "s" => "sever",
            "j" => "jih",
            "v" => "vychod",
            "z" => "zapad",
            "n" => "nahoru",
            "d" => "dolu",
            _ => direction
        };

        if (!currentRoom.Exits.TryGetValue(direction, out var targetRoomId))
        {
            return $"Tudy nemuzes jit na {direction}.";
        }

        foreach (var npcId in currentRoom.Npcs)
        {
            var npc = ws.GetNpc(npcId);
            if (npc != null && !npc.IsDead && npc.BlocksExit == direction)
            {
                return $"{npc.Name} ti blokuje cestu na {direction}! Musis ho porazit.";
            }
        }

        if (currentRoom.RequiredItems.TryGetValue(direction, out var requiredItemId))
        {
            if (!player.State.Inventory.Contains(requiredItemId))
            {
                var item = ws.GetItem(requiredItemId);
                var itemName = item?.Name ?? requiredItemId;
                return $"Cesta na {direction} je zamcena. Potrebujes: {itemName}.";
            }

            player.State.Inventory.Remove(requiredItemId);
            currentRoom.RequiredItems.Remove(direction);
            ps.SavePlayer(player.State);
        }

        var targetRoom = ws.GetRoom(targetRoomId);
        if (targetRoom == null) return "Cilova mistnost neexistuje.";

        if (_server != null)
        {
            var playersInOldRoom = _server.GetPlayersInRoom(oldRoomId);
            foreach (var p in playersInOldRoom)
            {
                if (p != player)
                {
                    await p.SendMessageAsync($"\n{player.State.Username} odchazi smerem {direction}.");
                }
            }
        }

        player.CurrentRoomId = targetRoomId;
        player.State.CurrentRoomId = targetRoomId;
        ps.SavePlayer(player.State);
        _logger.LogCommand(player.State.Username, $"jdi {direction}");

        if (_server != null)
        {
            var playersInNewRoom = _server.GetPlayersInRoom(targetRoomId);
            foreach (var p in playersInNewRoom)
            {
                if (p != player)
                {
                    await p.SendMessageAsync($"\n{player.State.Username} prichazi.");
                }
            }
        }

        var exploreCmd = new ExploreCommand(ws, _logger, _server);
        var roomInfo = await exploreCmd.Execute(client, player, ws);

        return $"Jdes na {direction}...\n\n" + roomInfo;
    }
}