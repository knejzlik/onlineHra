using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using onlineHra.Commands;
using System.Collections.Generic;
using System.Linq;
using System;

namespace onlineHra.Networking;

public class Server
{
    private TcpListener server;
    private HashSet<Player> connections;
    private Dictionary<string, ICommand> commands;
    
    // We need an object to lock our connections list so multiple threads don't crash it
    private readonly object _connectionsLock = new object();

    public Server(int port)
    {
        commands = new Dictionary<string, ICommand>();
        connections = new HashSet<Player>();
        server = new TcpListener(System.Net.IPAddress.Any, port);
        
        // Add commands BEFORE starting loops, avoiding race conditions
        commands.Add("help", new HelpCommand()); 
    }

    // Move the starting logic out of the constructor
    public void Start()
    {
        server.Start();
        Console.WriteLine("Server spusten");
        
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
            Player p = await Player.Connect(tcpc);

            // HashSets are not thread-safe. We must lock it when modifying.
            lock (_connectionsLock)
            {
                connections.Add(p);
            }

            // Start the infinite command loop for this specific player
            await ClientLoopAsync(p);
        }
        catch (Exception ex)
        {
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
                

                Console.WriteLine($"Received: {inp}");
                
                if (inp.ToLower() == "help")
                {
                    // Ensure the command is executed and written properly
                    string response = await commands["help"].Execute(client.Client);
                    await writer.WriteAsync(response + "\n");
                }
            }
        }
        catch (Exception)
        {
            // Handle unexpected socket drops
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
                    Disconnect(client);
                }
            }
        }
    }
}