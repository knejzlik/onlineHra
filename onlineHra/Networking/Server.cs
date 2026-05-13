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
    private int _port;

    public bool IsGameWon { get; set; } = false;
    public HashSet<string> SubmittedItems { get; set; } = new();

    public Server(int port)
    {
        _port = port;
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
        Console.WriteLine($"Server started on port {_port}");
        _logger.LogInfo($"Server started on port {_port}");

        commands.Add("help", new HelpCommand(_worldService, _logger, this));
        commands.Add("explore", new ExploreCommand(_worldService, _logger, this));
        commands.Add("go", new GoCommand(_worldService, _playerService, _logger, this));
        commands.Add("inventory", new InventoryCommand(_worldService, _playerService));
        commands.Add("take", new TakeCommand(_worldService, _playerService, _logger));
        commands.Add("drop", new DropCommand(_worldService, _playerService, _logger));
        commands.Add("talk", new TalkCommand(_worldService, this));
        commands.Add("say", new SayCommand(_logger, this));
        commands.Add("whisper", new WhisperCommand(this));
        commands.Add("broadcast", new BroadcastCommand(this));
        commands.Add("attack", new AttackCommand(_worldService, _playerService, _logger, this));
        commands.Add("trade", new TradeCommand(_worldService, _playerService, _logger, this));
        commands.Add("give", new GiveCommand(_worldService, _playerService, _logger, this));

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

            await p.SendMessageAsync("Welcome to the MUD game!");
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

                if (IsGameWon)
                {
                    if (inp.ToLower() == "restart" || string.IsNullOrEmpty(inp) || inp.ToLower() == "hrat znovu")
                    {
                        await RestartGame();
                        continue;
                    }
                    else
                    {
                        await writer.WriteAsync("Game is over! Press Enter or type 'restart' to play again.\n");
                        continue;
                    }
                }

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
                    response = $"Unknown command '{commandName}'. Type 'help' for a list of commands.";
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
            "help" => await ((HelpCommand)cmd).Execute(player.Client, player),
            "explore" => await ((ExploreCommand)cmd).Execute(player.Client, player, _worldService),
            "go" => await ((GoCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "inventory" => await ((InventoryCommand)cmd).Execute(player.Client, player, _worldService, _playerService),
            "take" => await ((TakeCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "drop" => await ((DropCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "talk" => await ((TalkCommand)cmd).Execute(player.Client, player, _worldService, args),
            "say" => await ((SayCommand)cmd).Execute(player.Client, player, _worldService, args),
            "whisper" => await ((WhisperCommand)cmd).Execute(player.Client, player, _worldService, args),
            "broadcast" => await ((BroadcastCommand)cmd).Execute(player.Client, player, _worldService, args),
            "attack" => await ((AttackCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "trade" => await ((TradeCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "give" => await ((GiveCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            _ => await cmd.Execute(player.Client)
        };
    }

    public void TriggerWin()
    {
        IsGameWon = true;
        var msg = "\n=========================================================\n" +
                  "                    CONGRATULATIONS!                     \n" +
                  "       The ritual is complete! Everyone wins!            \n" +
                  "=========================================================\n" +
                  "Press Enter or type 'restart' to play again.";

        foreach (var p in GetAllPlayers())
        {
            p.SendMessageAsync(msg).Wait();
        }
    }

    public async Task RestartGame()
    {
        IsGameWon = false;
        SubmittedItems.Clear();
        _worldService = new WorldService("Data");

        var players = GetAllPlayers();

        foreach (var p in players)
        {
            p.State.Inventory.Clear();
            p.State.CurrentRoomId = "start";
            p.CurrentRoomId = "start";
            p.State.GameCompleted = false;
            _playerService.SavePlayer(p.State);
        }

        foreach (var p in players)
        {
            await p.SendMessageAsync("\n--- THE WORLD HAS BEEN RESET ---");
            var exploreCmd = new ExploreCommand(_worldService, _logger, this);
            var roomInfo = await exploreCmd.Execute(p.Client, p, _worldService);
            await p.SendMessageAsync(roomInfo);
        }
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