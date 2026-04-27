using System.Net.Sockets;

namespace onlineHraClient;

class Program
{
    static string currentInput = "";
    static int cursorPosition = 0;
    static readonly object lockObj = new object();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MUD Client ===");
        Console.Write("Server address (default: localhost): ");
        var serverAddressInput = Console.ReadLine();
        var serverAddress = string.IsNullOrWhiteSpace(serverAddressInput) ? "localhost" : serverAddressInput.Trim();
        
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
                        
                        // Capture input state under lock
                        string capturedInput;
                        int capturedCursor;
                        lock (lockObj)
                        {
                            capturedInput = currentInput;
                            capturedCursor = cursorPosition;
                        }
                        
                        // Simply write the message and redisplay prompt
                        // Avoid complex cursor manipulation that can fail
                        Console.WriteLine();
                        Console.WriteLine(line);
                        Console.Write(">>> ");
                        Console.Write(capturedInput);
                        
                        // Try to set cursor position, but don't crash if it fails
                        try
                        {
                            int targetLeft = 4 + capturedCursor;
                            int targetTop = Console.CursorTop;
                            if (targetLeft < Console.BufferWidth && targetTop < Console.BufferHeight)
                            {
                                Console.SetCursorPosition(targetLeft, targetTop);
                            }
                        }
                        catch
                        {
                            // If we can't set cursor position, just leave it at the end
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nConnection lost: {ex.Message}");
                }
            });
            
            // Send user input with custom character-by-character reading
            Console.Write(">>> ");
            string? inputToSend = null;
            bool shouldExit = false;
            
            while (true)
            {
                var key = Console.ReadKey(true);
                
                lock (lockObj)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        inputToSend = currentInput;
                        currentInput = "";
                        cursorPosition = 0;
                        
                        if (inputToSend.ToLower() == "quit" || inputToSend.ToLower() == "exit")
                        {
                            shouldExit = true;
                        }
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (cursorPosition > 0)
                        {
                            currentInput = currentInput.Remove(cursorPosition - 1, 1);
                            cursorPosition--;
                            
                            // Redraw the line
                            int currentLine = Console.CursorTop;
                            Console.SetCursorPosition(0, currentLine);
                            string currentLineContent = ">>> " + currentInput;
                            int spacesNeeded = Math.Max(0, Console.WindowWidth - currentLineContent.Length);
                            Console.Write(currentLineContent + new string(' ', spacesNeeded));
                            Console.SetCursorPosition(4 + cursorPosition, currentLine);
                        }
                    }
                    else if (key.Key == ConsoleKey.LeftArrow)
                    {
                        if (cursorPosition > 0)
                        {
                            cursorPosition--;
                            Console.SetCursorPosition(4 + cursorPosition, Console.CursorTop);
                        }
                    }
                    else if (key.Key == ConsoleKey.RightArrow)
                    {
                        if (cursorPosition < currentInput.Length)
                        {
                            cursorPosition++;
                            Console.SetCursorPosition(4 + cursorPosition, Console.CursorTop);
                        }
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        currentInput = currentInput.Insert(cursorPosition, key.KeyChar.ToString());
                        cursorPosition++;
                        
                        // Redraw from cursor position
                        int currentLine = Console.CursorTop;
                        Console.SetCursorPosition(4 + cursorPosition - 1, currentLine);
                        Console.Write(key.KeyChar);
                        if (cursorPosition < currentInput.Length)
                        {
                            Console.Write(currentInput.Substring(cursorPosition));
                            Console.SetCursorPosition(4 + cursorPosition, currentLine);
                        }
                    }
                }
                
                // Send message after releasing lock
                if (inputToSend != null)
                {
                    await writer.WriteLineAsync(inputToSend);
                    if (shouldExit) break;
                    inputToSend = null;
                    Console.Write(">>> ");
                }
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
