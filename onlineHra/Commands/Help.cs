using System.Net.Sockets;
using System.Text;
using onlineHra.Services;
using onlineHra.Networking;

namespace onlineHra.Commands;

public class HelpCommand : ICommand
{
    private readonly WorldService _worldService;
    private readonly LoggingService _logger;
    private readonly Server? _server;

    public HelpCommand(WorldService worldService, LoggingService logger, Server? server = null)
    {
        _worldService = worldService;
        _logger = logger;
        _server = server;
    }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null);
    }

    public async Task<string> Execute(TcpClient client, Player? player)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== HELP ===");
        sb.AppendLine("go <direction> - movement (north/south/east/west/up/down or n/s/e/w/u/d)");
        sb.AppendLine("explore - display information about your current room");
        sb.AppendLine("inventory - view your items and capacity");
        sb.AppendLine("take <item> - pick up an item from the room");
        sb.AppendLine("drop <item> - drop an item on the ground");
        sb.AppendLine("attack <character> - attack a monster (requires a weapon)");
        sb.AppendLine("trade <item> - offer an item for trade to an NPC");
        sb.AppendLine("give <item> [to <player>] - hand over a quest artifact to the High Priest or give an item to a player");
        sb.AppendLine("talk <character> [topic] - talk to an NPC");
        sb.AppendLine("say <message> - send a message to players in the same room");
        sb.AppendLine("whisper <player> <message> - private message to another player");
        sb.AppendLine("broadcast <message> - global message to all players");
        sb.AppendLine("help - display this help");

        return await Task.FromResult(sb.ToString());
    }
}