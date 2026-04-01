using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class DropCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;

    public DropCommand(WorldService worldService, PlayerService playerService, LoggingService logger)
    {
        _worldService = worldService;
        _playerService = playerService;
        _logger = logger;
    }

    public DropCommand() : this(new WorldService(), new PlayerService(), new LoggingService()) { }

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
            return "Usage: drop <item name>";
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

        var itemName = args.ToLower().Trim();
        
        string? foundItemId = null;
        Item? foundItem = null;
        
        foreach (var itemId in player.State.Inventory.ToList())
        {
            var item = ws.GetItem(itemId);
            if (item != null && item.Name.ToLower().Contains(itemName))
            {
                foundItemId = itemId;
                foundItem = item;
                break;
            }
        }

        if (foundItemId == null || foundItem == null)
        {
            return $"You don't have '{args}' in your inventory.";
        }

        player.State.Inventory.Remove(foundItemId);
        currentRoom.Items.Add(foundItemId);
        ps.SavePlayer(player.State);

        _logger.LogCommand(player.State.Username, $"drop {foundItemId}");

        return $"You dropped the {foundItem.Name}.";
    }
}
