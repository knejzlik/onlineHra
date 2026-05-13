using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace onlineHraClient;

class Program
{
    static string currentInput = "";
    static int cursorPosition = 0;
    static bool isPasswordMode = false;
    static readonly object lockObj = new object();

    static async Task Main(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string defaultAddress = configuration.GetValue<string>("Client:DefaultServerAddress", "localhost") ?? "localhost";
        int defaultPort = configuration.GetValue<int>("Client:DefaultServerPort", 65525);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== MUD Client ===");
        Console.ResetColor();

        Console.Write($"Server address (default: {defaultAddress}): ");
        var serverAddressInput = Console.ReadLine();
        var serverAddress = string.IsNullOrWhiteSpace(serverAddressInput) ? defaultAddress : serverAddressInput.Trim();

        Console.Write($"Server port (default: {defaultPort}): ");
        var portInput = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(portInput))
        {
            portInput = defaultPort.ToString();
        }

        if (!int.TryParse(portInput, out int port))
        {
            port = defaultPort;
        }

        try
        {
            using var client = new TcpClient();
            Console.WriteLine($"Connecting to {serverAddress}:{port}...");

            await client.ConnectAsync(serverAddress, port);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connected!");
            Console.ResetColor();

            using var reader = new StreamReader(client.GetStream());
            using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };

            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;

                        string capturedInput;
                        int capturedCursor;
                        bool capturedPasswordMode;

                        lock (lockObj)
                        {
                            if (line.ToLower().Contains("password:"))
                            {
                                isPasswordMode = true;
                            }

                            capturedInput = currentInput;
                            capturedCursor = cursorPosition;
                            capturedPasswordMode = isPasswordMode;
                        }

                        Console.WriteLine();
                        PrintColorized(line);

                        if (!capturedPasswordMode)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write(">>> ");
                            Console.ResetColor();
                            Console.Write(capturedInput);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write(">>> ");
                            Console.ResetColor();
                            Console.Write(new string('*', capturedInput.Length));
                        }

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
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nConnection lost: {ex.Message}");
                    Console.ResetColor();
                }
            });

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(">>> ");
            Console.ResetColor();

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

                        isPasswordMode = false;

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

                            int currentLine = Console.CursorTop;
                            Console.SetCursorPosition(0, currentLine);
                            string displayInput = isPasswordMode ? new string('*', currentInput.Length) : currentInput;

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write(">>> ");
                            Console.ResetColor();

                            string currentLineContent = ">>> " + displayInput;
                            int spacesNeeded = Math.Max(0, Console.WindowWidth - currentLineContent.Length);
                            Console.Write(displayInput + new string(' ', spacesNeeded));
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

                        int currentLine = Console.CursorTop;
                        Console.SetCursorPosition(4 + cursorPosition - 1, currentLine);
                        char displayChar = isPasswordMode ? '*' : key.KeyChar;
                        Console.Write(displayChar);
                        if (cursorPosition < currentInput.Length)
                        {
                            if (isPasswordMode)
                            {
                                Console.Write(new string('*', currentInput.Length - cursorPosition));
                            }
                            else
                            {
                                Console.Write(currentInput.Substring(cursorPosition));
                            }
                            Console.SetCursorPosition(4 + cursorPosition, currentLine);
                        }
                    }
                }

                if (inputToSend != null)
                {
                    await writer.WriteLineAsync(inputToSend);
                    if (shouldExit) break;
                    inputToSend = null;

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(">>> ");
                    Console.ResetColor();
                }
            }

            client.Close();
            Console.WriteLine("Disconnected from server.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static void PrintColorized(string line)
    {
        if (line.StartsWith("===") || line.StartsWith("---"))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }
        else if (line.Contains("CONGRATULATIONS") || line.Contains("wins!") || line.StartsWith("You handed over") || line.Contains("successfully traded"))
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }
        else if (line.Contains("dead") || line.Contains("strikes down") || line.Contains("roared and fell") || line.StartsWith("You attacked") || line.Contains("error") || line.Contains("Cannot go") || line.Contains("locked") || line.Contains("blocks your path"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }
        else if (line.StartsWith("Exits:") || line.StartsWith("Items here:") || line.StartsWith("Characters here:") || line.StartsWith("Players here:"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else if (line.StartsWith("["))
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
        }
        else if (line.Contains("says: \""))
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        Console.WriteLine(line);
        Console.ResetColor();
    }
}