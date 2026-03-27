using System.Net.Sockets;
using System.Text;
using onlineHra.Models;
using onlineHra.Services;

namespace onlineHra.Commands;

public class SayCommand : ICommand
{
    private readonly LoggingService _logger;

    public SayCommand(LoggingService logger)
    {
        _logger = logger;
    }

    public SayCommand() : this(new LoggingService()) { }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, null, null, null);
    }

    public async Task<string> Execute(TcpClient client, Networking.Player? player, WorldService? worldService, string? args = null)
    {
        if (player == null || string.IsNullOrEmpty(args))
        {
            return "Usage: rekni <message> - sends a message to all players in the same room";
        }

        return $"You say: \"{args}\"";
    }
}
