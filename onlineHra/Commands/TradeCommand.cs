using System.Net.Sockets;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class TradeCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public TradeCommand(WorldService worldService, PlayerService playerService, LoggingService logger, Server? server = null)
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
            return "Pouziti: vymen <predmet>";
        }

        if (string.IsNullOrEmpty(player.CurrentRoomId))
        {
            player.CurrentRoomId = player.State.CurrentRoomId;
        }

        var currentRoom = ws.GetRoom(player.CurrentRoomId);
        if (currentRoom == null) return "Chyba mistnosti.";

        if (!currentRoom.Npcs.Contains("drak"))
        {
            return "Neni tu s kym vymenovat. Potrebujes najit draka.";
        }

        var itemName = args.ToLower().Trim();
        string? foundItemId = null;

        foreach (var itemId in player.State.Inventory)
        {
            var item = ws.GetItem(itemId);
            if (item != null && item.Name.ToLower().Contains(itemName))
            {
                foundItemId = itemId;
                break;
            }
        }

        if (foundItemId == null)
        {
            return $"Nemas v inventari nic jako '{itemName}'.";
        }

        if (foundItemId != "falesne_vejce")
        {
            return "Drak o tento predmet nema vubec zajem.";
        }

        player.State.Inventory.Remove("falesne_vejce");
        player.State.Inventory.Add("zlate_vejce");
        ps.SavePlayer(player.State);

        _logger.LogCommand(player.State.Username, "vymen falesne_vejce");

        if (_server != null)
        {
            var playersInRoom = _server.GetPlayersInRoom(player.CurrentRoomId);
            foreach (var p in playersInRoom)
            {
                if (p != player)
                {
                    await p.SendMessageAsync($"\n{player.State.Username} uspesne vymenil vejce s drakem.");
                }
            }
        }

        return "Podal jsi drakovi Tezke vejce. Drak z nej mel obrovskou radost a na oplatku ti prenechal sve prave Zlate vejce!";
    }
}