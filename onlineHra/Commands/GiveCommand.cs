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

        if (_server == null) return "Server error.";

        var input = args.ToLower().Trim();
        var parts = input.Split(new[] { " to " }, StringSplitOptions.None);
        var itemName = parts[0].Trim();
        var targetPlayerName = parts.Length > 1 ? parts[1].Trim() : null;

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

        var foundItem = ws.GetItem(foundItemId);

        if (!string.IsNullOrEmpty(targetPlayerName))
        {
            var playersInRoom = _server.GetPlayersInRoom(player.CurrentRoomId);
            var targetPlayer = playersInRoom.FirstOrDefault(p => p.State.Username.ToLower().Contains(targetPlayerName) && p != player);

            if (targetPlayer == null)
            {
                return $"There is no player named '{targetPlayerName}' here.";
            }

            var targetWeight = targetPlayer.State.Inventory.Sum(id => ws.GetItem(id)?.Weight ?? 0);
            if (foundItem != null && targetWeight + foundItem.Weight > targetPlayer.State.MaxInventoryCapacity)
            {
                return $"{targetPlayer.State.Username}'s inventory is too full to carry the {foundItem.Name}.";
            }

            player.State.Inventory.Remove(foundItemId);
            ps.SavePlayer(player.State);

            targetPlayer.State.Inventory.Add(foundItemId);
            ps.SavePlayer(targetPlayer.State);

            _logger.LogCommand(player.State.Username, $"give {foundItemId} to {targetPlayer.State.Username}");

            await targetPlayer.SendMessageAsync($"\n{player.State.Username} gave you {foundItem?.Name}.");

            return $"You gave {foundItem?.Name} to {targetPlayer.State.Username}.";
        }

        if (player.CurrentRoomId != "temple")
        {
            return "You can only give items to the High Priest in the temple. (Or use 'give <item> to <player>')";
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

        _logger.LogCommand(player.State.Username, $"give {foundItemId} to High Priest");

        var broadcastMsg = $"\n{player.State.Username} has given the {foundItem?.Name}!";
        foreach (var p in _server.GetAllPlayers())
        {
            if (p != player) await p.SendMessageAsync(broadcastMsg);
        }

        if (_server.SubmittedItems.Contains("golden_egg") && _server.SubmittedItems.Contains("dragon_scale"))
        {
            _server.TriggerWin();
            return $"You handed over the {foundItem?.Name}.";
        }

        return $"You handed over the {foundItem?.Name}. The High Priest still needs the other artifact.";
    }
}