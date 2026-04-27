using System.Net.Sockets;
using onlineHra.Commands;
using onlineHra.Services;

namespace onlineHra.Networking;

public class Server
{
    private TcpListener server;
    private HashSet<Player> connections;
    private WorldService _worldService;
    private PlayerService _playerService;
    private LoggingService _logger;
    private readonly object _connectionsLock = new object();
    private Dictionary<string, ICommand> commands;

    public Server(int port)
    {
        var baseDir = AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(baseDir);

        _worldService = new WorldService("Data");
        _playerService = new PlayerService("Data/players.json");
        _logger = new LoggingService("Data/server.log");

        commands = new Dictionary<string, ICommand>();
        connections = new HashSet<Player>();
        server = new TcpListener(System.Net.IPAddress.Any, port);
    }

    public void Start()
    {
        server.Start();
        Console.WriteLine("Server started on port 65525");
        _logger.LogInfo("Server started on port 65525");

        commands.Add("pomoc", new HelpCommand(_worldService, _logger, this));
        commands.Add("prozkoumej", new ExploreCommand(_worldService, _logger, this));
        commands.Add("jdi", new GoCommand(_worldService, _playerService, _logger, this));
        commands.Add("inventar", new InventoryCommand(_worldService, _playerService));
        commands.Add("vezmi", new TakeCommand(_worldService, _playerService, _logger));
        commands.Add("zahod", new DropCommand(_worldService, _playerService, _logger));
        commands.Add("mluv", new TalkCommand(_worldService, this));
        commands.Add("rekni", new SayCommand(_logger, this));
        commands.Add("septej", new WhisperCommand(this));
        commands.Add("rozhlas", new BroadcastCommand(this));
        commands.Add("utoc", new AttackCommand(_worldService, _playerService, _logger, this));
        commands.Add("vymen", new TradeCommand(_worldService, _playerService, _logger, this));

        _ = AcceptLoopAsync();
        _ = CheckForDisconnectsAsync();
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient tcpc = await server.AcceptTcpClientAsync();
            _ = HandleNewPlayerAsync(tcpc);
        }
    }

    private async Task HandleNewPlayerAsync(TcpClient tcpc)
    {
        try
        {
            Player? p = await Player.Connect(tcpc, _playerService);

            if (p == null)
            {
                tcpc.Close();
                return;
            }

            lock (_connectionsLock)
            {
                connections.Add(p);
            }

            _logger.LogPlayerConnect(p.State.Username);

            await p.SendMessageAsync("Vitej v textovem MUDu!");
            var exploreCmd = new ExploreCommand(_worldService, _logger, this);
            var welcomeMsg = await exploreCmd.Execute(tcpc, p, _worldService);
            await p.SendMessageAsync(welcomeMsg);

            await ClientLoopAsync(p);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Connection error: {ex.Message}");
        }
    }

    private async Task ClientLoopAsync(Player client)
    {
        StreamReader reader = client.Reader;
        StreamWriter writer = client.Writer;
        writer.AutoFlush = true;

        try
        {
            while (true)
            {
                await writer.WriteAsync(">>> ");
                string? inp = await reader.ReadLineAsync();

                if (inp == null) break;

                inp = inp.Trim();
                if (string.IsNullOrEmpty(inp)) continue;

                _logger.LogCommand(client.State.Username, inp);
                Console.WriteLine($"[{client.State.Username}]: {inp}");

                var parts = inp.Split(' ', 2);
                var commandName = parts[0].ToLower();
                var args = parts.Length > 1 ? parts[1] : null;

                string response;
                if (commands.TryGetValue(commandName, out var cmd))
                {
                    response = await ExecuteCommand(cmd, client, commandName, args);
                }
                else
                {
                    response = $"Neznamy prikaz '{commandName}'. Napis 'pomoc' pro seznam prikazu.";
                }

                await writer.WriteAsync(response + "\n");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Client loop error: {ex.Message}");
        }
        finally
        {
            _playerService.SavePlayer(client.State);
            _logger.LogPlayerDisconnect(client.State.Username);

            lock (_connectionsLock)
            {
                connections.Remove(client);
            }
            client.Disconnect();
        }
    }

    private async Task<string> ExecuteCommand(ICommand cmd, Player player, string commandName, string? args)
    {
        return commandName switch
        {
            "pomoc" => await ((HelpCommand)cmd).Execute(player.Client, player),
            "prozkoumej" => await ((ExploreCommand)cmd).Execute(player.Client, player, _worldService),
            "jdi" => await ((GoCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "inventar" => await ((InventoryCommand)cmd).Execute(player.Client, player, _worldService, _playerService),
            "vezmi" => await ((TakeCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "zahod" => await ((DropCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "mluv" => await ((TalkCommand)cmd).Execute(player.Client, player, _worldService, args),
            "rekni" => await ((SayCommand)cmd).Execute(player.Client, player, _worldService, args),
            "septej" => await ((WhisperCommand)cmd).Execute(player.Client, player, _worldService, args),
            "rozhlas" => await ((BroadcastCommand)cmd).Execute(player.Client, player, _worldService, args),
            "utoc" => await ((AttackCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "vymen" => await ((TradeCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            _ => await cmd.Execute(player.Client)
        };
    }

    private void Disconnect(Player client)
    {
        client.Disconnect();
        lock (_connectionsLock)
        {
            connections.Remove(client);
        }
    }

    public List<Player> GetPlayersInRoom(string roomId)
    {
        lock (_connectionsLock)
        {
            return connections.Where(p => p.CurrentRoomId == roomId).ToList();
        }
    }

    public List<Player> GetAllPlayers()
    {
        lock (_connectionsLock)
        {
            return connections.ToList();
        }
    }

    private async Task CheckForDisconnectsAsync()
    {
        while (true)
        {
            await Task.Delay(10000);

            lock (_connectionsLock)
            {
                var disconnectedPlayers = connections.Where(c => !c.Client.Connected).ToList();

                foreach (var client in disconnectedPlayers)
                {
                    _logger.LogPlayerDisconnect(client.State.Username);
                    _playerService.SavePlayer(client.State);
                    Disconnect(client);
                }
            }
        }
    }
}