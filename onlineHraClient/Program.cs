using System.Net.Sockets;
using System.Text;

namespace onlineHraClient;

class Program
{
    static string currentInput = "";
    static int cursorPosition = 0;
    static bool isPasswordMode = false;
    static readonly object lockObj = new object();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MUD Klient ===");
        Console.Write("Adresa serveru (vychozi: localhost): ");
        var serverAddressInput = Console.ReadLine();
        var serverAddress = string.IsNullOrWhiteSpace(serverAddressInput) ? "localhost" : serverAddressInput.Trim();

        Console.Write("Port serveru (vychozi: 65525): ");
        var portInput = Console.ReadLine()?.Trim() ?? "65525";

        if (!int.TryParse(portInput, out int port))
        {
            port = 65525;
        }

        try
        {
            using var client = new TcpClient();
            Console.WriteLine($"Pripojovani k {serverAddress}:{port}...");

            await client.ConnectAsync(serverAddress, port);
            Console.WriteLine("Pripojeno!");

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
                            if (line.ToLower().Contains("heslo:") || line.ToLower().Contains("password:"))
                            {
                                isPasswordMode = true;
                            }

                            capturedInput = currentInput;
                            capturedCursor = cursorPosition;
                            capturedPasswordMode = isPasswordMode;
                        }

                        Console.WriteLine();
                        Console.WriteLine(line);

                        if (!capturedPasswordMode)
                        {
                            Console.Write(">>> ");
                            Console.Write(capturedInput);
                        }
                        else
                        {
                            Console.Write(">>> ");
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
                    Console.WriteLine($"\nSpojeni ztraceno: {ex.Message}");
                }
            });

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
                            string currentLineContent = ">>> " + displayInput;
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
                    Console.Write(">>> ");
                }
            }

            client.Close();
            Console.WriteLine("Odpojeno od serveru.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}