using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class TakeCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;
    private readonly LoggingService _logger;

    public TakeCommand(WorldService worldService, PlayerService playerService, LoggingService logger)
    {
        _worldService = worldService;
        _playerService = playerService;
        _logger = logger;
    }

    public TakeCommand() : this(new WorldService(), new PlayerService(), new LoggingService()) { }

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
            return "Usage: take <item name>";
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
        
        foreach (var itemId in currentRoom.Items.ToList())
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
            return $"There is no '{args}' here to take.";
        }

        var currentWeight = player.State.Inventory.Sum(id => ws.GetItem(id)?.Weight ?? 0);
        if (currentWeight + foundItem.Weight > player.State.MaxInventoryCapacity)
        {
            return $"Your inventory is too full to carry the {foundItem.Name}.";
        }

        currentRoom.Items.Remove(foundItemId);
        player.State.Inventory.Add(foundItemId);
        ps.SavePlayer(player.State);

        _logger.LogCommand(player.State.Username, $"take {foundItemId}");

        return $"You picked up the {foundItem.Name}. ({foundItem.Description})";
    }
}
