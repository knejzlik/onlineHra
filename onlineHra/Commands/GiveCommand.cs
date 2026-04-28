using System.Net.Sockets;
using onlineHra.Models;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class GiveCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public GiveCommand(WorldService worldService, PlayerService playerService, LoggingService logger, Server? server = null)
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
            return "Usage: give <item>";
        }

        if (player.CurrentRoomId != "temple")
        {
            return "You can only give items to the High Priest in the temple.";
        }

        if (_server == null) return "Server error.";

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
            return $"You don't have '{itemName}' in your inventory.";
        }

        if (foundItemId != "golden_egg" && foundItemId != "dragon_scale")
        {
            return "The High Priest doesn't want this item.";
        }

        if (_server.SubmittedItems.Contains(foundItemId))
        {
            return "This item has already been given by someone else.";
        }

        player.State.Inventory.Remove(foundItemId);
        ps.SavePlayer(player.State);
        _server.SubmittedItems.Add(foundItemId);

        _logger.LogCommand(player.State.Username, $"give {foundItemId}");

        var broadcastMsg = $"\n{player.State.Username} has given the {ws.GetItem(foundItemId)?.Name}!";
        foreach (var p in _server.GetAllPlayers())
        {
            if (p != player) await p.SendMessageAsync(broadcastMsg);
        }

        if (_server.SubmittedItems.Contains("golden_egg") && _server.SubmittedItems.Contains("dragon_scale"))
        {
            _server.TriggerWin();
            return $"You handed over the {ws.GetItem(foundItemId)?.Name}.";
        }

        return $"You handed over the {ws.GetItem(foundItemId)?.Name}. The High Priest still needs the other artifact.";
    }
}