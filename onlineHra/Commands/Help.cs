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

        sb.AppendLine("=== NAPOVEDA ===");
        sb.AppendLine("jdi <smer> - pohyb (sever/jih/vychod/zapad/nahoru/dolu nebo s/j/v/z/n/d)");
        sb.AppendLine("prozkoumej - zobrazi informace o tve aktualni mistnosti");
        sb.AppendLine("inventar - zobrazi tve veci a kapacitu");
        sb.AppendLine("vezmi <predmet> - sebere predmet z mistnosti");
        sb.AppendLine("zahod <predmet> - zahodi predmet na zem");
        sb.AppendLine("utoc <postava> - zautoci na monstrum (potrebujes zbran)");
        sb.AppendLine("vymen <predmet> - nabidne predmet k vymene NPC");
        sb.AppendLine("mluv <postava> [tema] - promluvi s NPC (ukaze ti dostupna temata)");
        sb.AppendLine("rekni <zprava> - posle zpravu hracum ve stejne mistnosti");
        sb.AppendLine("septej <hrac> <zprava> - soukroma zprava jinemu hraci");
        sb.AppendLine("rozhlas <zprava> - globalni zprava vsem hracum ve hre");
        sb.AppendLine("pomoc - zobrazi tuto napovedu");
        sb.AppendLine();

        if (player != null)
        {
            var exploreCmd = new ExploreCommand(_worldService, _logger, _server);
            var roomInfo = await exploreCmd.Execute(client, player, _worldService);
            sb.AppendLine(roomInfo);
        }

        return sb.ToString();
    }
}