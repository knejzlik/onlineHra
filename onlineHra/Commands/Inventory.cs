using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class InventoryCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly PlayerService _playerService;

    public InventoryCommand(WorldService worldService, PlayerService playerService)
    {
        _worldService = worldService;
        _playerService = playerService;
    }

    public InventoryCommand() : this(new WorldService(), new PlayerService()) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, PlayerService? playerService)
    {
        var ws = worldService ?? _worldService;
        var ps = playerService ?? _playerService;

        if (player == null)
        {
            return "Error: Player not found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== INVENTORY ===");
        sb.AppendLine($"Capacity: {player.State.Inventory.Count} / {player.State.MaxInventoryCapacity}");
        sb.AppendLine();

        if (player.State.Inventory.Count == 0)
        {
            sb.AppendLine("Your inventory is empty.");
        }
        else
        {
            sb.AppendLine("Items:");
            foreach (var itemId in player.State.Inventory)
            {
                var item = ws.GetItem(itemId);
                if (item != null)
                {
                    sb.AppendLine($"  - {item.Name}: {item.Description} (weight: {item.Weight})");
                }
                else
                {
                    sb.AppendLine($"  - {itemId} (unknown item)");
                }
            }
        }

        return sb.ToString();
    }
}
