using System.Net.Sockets;

namespace onlineHraClient;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MUD Client ===");
        Console.Write("Server address (default: localhost): ");
        var serverAddress = Console.ReadLine()?.Trim() ?? "localhost";
        
        Console.Write("Server port (default: 65525): ");
        var portInput = Console.ReadLine()?.Trim() ?? "65525";
        
        if (!int.TryParse(portInput, out int port))
        {
            port = 65525;
        }

        try
        {
            using var client = new TcpClient();
            Console.WriteLine($"Connecting to {serverAddress}:{port}...");
            
            await client.ConnectAsync(serverAddress, port);
            Console.WriteLine("Connected!");
            
            using var reader = new StreamReader(client.GetStream());
            using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            
            // Read and display server messages in background
            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;
                        // Clear the current input line and re-display prompt after message
                        Console.WriteLine();
                        Console.WriteLine(line);
                        Console.Write(">>> ");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nConnection lost: {ex.Message}");
                }
            });
            
            // Send user input
            while (true)
            {
                Console.Write(">>> ");
                var input = Console.ReadLine();
                if (input == null) break;
                
                if (input.ToLower() == "quit" || input.ToLower() == "exit")
                {
                    await writer.WriteLineAsync(input);
                    break;
                }
                
                await writer.WriteLineAsync(input);
            }
            
            client.Close();
            Console.WriteLine("Disconnected from server.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
