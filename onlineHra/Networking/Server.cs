using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using onlineHra.Commands;
using onlineHra.Services;
using System.Collections.Generic;
using System.Linq;

namespace onlineHra.Networking;

public class Server
{
    private TcpListener server;
    private HashSet<Player> connections;
    private WorldService _worldService;
    private PlayerService _playerService;
    private LoggingService _logger;
    
    // We need an object to lock our connections list so multiple threads don't crash it
    private readonly object _connectionsLock = new object();

    public Server(int port)
    {
        // Set working directory to ensure Data folder is found
        var baseDir = AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(baseDir);
        
        _worldService = new WorldService("Data");
        _playerService = new PlayerService("Data/players.json");
        _logger = new LoggingService("Data/server.log");
        
        commands = new Dictionary<string, ICommand>();
        connections = new HashSet<Player>();
        server = new TcpListener(System.Net.IPAddress.Any, port);
    }

    private Dictionary<string, ICommand> commands;

    // Move the starting logic out of the constructor
    public void Start()
    {
        server.Start();
        Console.WriteLine("Server started on port 65525");
        _logger.LogInfo("Server started on port 65525");
        
        // Initialize commands with services
        commands.Add("help", new HelpCommand());
        commands.Add("explore", new ExploreCommand(_worldService, _logger, this));
        commands.Add("go", new GoCommand(_worldService, _playerService, _logger, this));
        commands.Add("inventory", new InventoryCommand(_worldService, _playerService));
        commands.Add("take", new TakeCommand(_worldService, _playerService, _logger));
        commands.Add("drop", new DropCommand(_worldService, _playerService, _logger));
        commands.Add("talk", new TalkCommand(_worldService, this));
        commands.Add("say", new SayCommand(_logger, this));
        commands.Add("whisper", new WhisperCommand(this));
        commands.Add("broadcast", new BroadcastCommand(this));
        
        // Use discards (_) to fire-and-forget these loops safely
        _ = AcceptLoopAsync();
        _ = CheckForDisconnectsAsync();
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        { 
            // Await the new client connection. 
            TcpClient tcpc = await server.AcceptTcpClientAsync();
            
            // Pass the client off to a separate method so this loop isn't blocked!
            _ = HandleNewPlayerAsync(tcpc);
        }
    }

    private async Task HandleNewPlayerAsync(TcpClient tcpc)
    {
        try
        {
            // Fully await the connection/registration process asynchronously
            Player? p = await Player.Connect(tcpc, _playerService);
            
            if (p == null)
            {
                tcpc.Close();
                return;
            }

            // HashSets are not thread-safe. We must lock it when modifying.
            lock (_connectionsLock)
            {
                connections.Add(p);
            }

            _logger.LogPlayerConnect(p.State.Username);
            
            // Send welcome message and initial room description
            await p.SendMessageAsync("Welcome to the MUD game!");
            var exploreCmd = new ExploreCommand(_worldService, _logger);
            var welcomeMsg = await exploreCmd.Execute(tcpc, p, _worldService);
            await p.SendMessageAsync(welcomeMsg);

            // Start the infinite command loop for this specific player
            await ClientLoopAsync(p);
        }
        catch (Exception ex)
        {
            _logger.LogError($"A player failed to connect or register: {ex.Message}");
            Console.WriteLine($"A player failed to connect or register: {ex.Message}");
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
                
                if (inp == null)
                {
                    break; // Client disconnected
                }

                inp = inp.Trim();
                if (string.IsNullOrEmpty(inp))
                {
                    continue;
                }

                _logger.LogCommand(client.State.Username, inp);
                Console.WriteLine($"[{client.State.Username}] Received: {inp}");
                
                // Parse command and arguments
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
                    response = $"Unknown command '{commandName}'. Type 'help' for available commands.";
                }
                
                await writer.WriteAsync(response + "\n");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Client loop error for {client.State.Username}: {ex.Message}");
        }
        finally
        {
            // Save player state on disconnect
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
            "help" => await cmd.Execute(player.Client),
            "explore" => await ((ExploreCommand)cmd).Execute(player.Client, player, _worldService),
            "go" => await ((GoCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "inventory" => await ((InventoryCommand)cmd).Execute(player.Client, player, _worldService, _playerService),
            "take" => await ((TakeCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "drop" => await ((DropCommand)cmd).Execute(player.Client, player, _worldService, _playerService, args),
            "talk" => await ((TalkCommand)cmd).Execute(player.Client, player, _worldService, args),
            "say" => await ((SayCommand)cmd).Execute(player.Client, player, _worldService, args),
            "whisper" => await ((WhisperCommand)cmd).Execute(player.Client, player, _worldService, args),
            "broadcast" => await ((BroadcastCommand)cmd).Execute(player.Client, player, _worldService, args),
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
        // Added a while(true) loop. Otherwise, this would only run once and stop.
        while (true)
        {
            await Task.Delay(10000);

            lock (_connectionsLock)
            {
                // Create a temporary list of disconnected players to avoid 
                // "Collection was modified" exceptions while iterating.
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