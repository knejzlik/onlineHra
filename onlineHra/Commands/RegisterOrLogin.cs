using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace onlineHra.Commands;

public class RegisterOrLogin : ICommand 
{
    public async Task<string> Execute(TcpClient client)
    {
        var tempReader = new StreamReader(client.GetStream());
        var tempWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };
        
        string filePath = "PayerSave.csv";
        bool nextStep = true;
        int state = 0;

        // Ensure the file exists
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }

        while (nextStep)
        {
            await tempWriter.WriteLineAsync("Would you like to register or login?");
            await tempWriter.WriteAsync(">>> ");
            string? input = await tempReader.ReadLineAsync();
            
            if (input == null) return string.Empty; 

            if (input.ToLower() == "register")
            {
                state = 1;
                break;
            }

            if (input.ToLower() == "login")
            {
                state = 2;
                break;
            }
            await tempWriter.WriteLineAsync("Unable to parse command. Please try again.");
        }

        switch (state)
        {
            case 1: // Registration
            {
                while (true)
                {
                    await tempWriter.WriteLineAsync("Enter username:");
                    await tempWriter.WriteAsync(">>> ");
                    var username = await tempReader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(username)) continue;

                    bool usableUsername = true;
                    
                    using (var fileReader = new StreamReader(filePath))
                    {
                        string? line;
                        while ((line = await fileReader.ReadLineAsync()) != null)
                        {
                            var split = line.Split(';');
                            if (split.Length > 0 && split[0].ToLower() == username.ToLower())
                            {
                                usableUsername = false;
                                break;
                            }
                        }
                    }

                    if (!usableUsername)
                    {
                        await tempWriter.WriteLineAsync("Username already in use.");
                        continue;
                    }

                    await tempWriter.WriteLineAsync("Enter password:");
                    await tempWriter.WriteAsync(">>> ");
                    var password = await tempReader.ReadLineAsync();
                    
                    await tempWriter.WriteLineAsync("Repeat password:");
                    await tempWriter.WriteAsync(">>> ");
                    var password2 = await tempReader.ReadLineAsync();
                    
                    if (password != password2) 
                    {
                        await tempWriter.WriteLineAsync("Passwords do not match.");
                        continue;
                    }

                    // Format the full player line. If you add default stats later (e.g., Level 1, 100 HP), add them here!
                    string newPlayerLine = $"{username};{password}";

                    using (var fileWriter = new StreamWriter(filePath, append: true))
                    {
                        await fileWriter.WriteLineAsync(newPlayerLine);
                    }

                    await tempWriter.WriteLineAsync("Registration successful!");
                    
                    // Return the newly created line
                    return newPlayerLine; 
                }
            }
            case 2: // Login
            {
                while (true)
                {
                    await tempWriter.WriteLineAsync("Enter username:");
                    await tempWriter.WriteAsync(">>> ");
                    var username = await tempReader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(username)) return string.Empty;
                    
                    await tempWriter.WriteLineAsync("Enter password:");
                    await tempWriter.WriteAsync(">>> ");
                    var password = await tempReader.ReadLineAsync();

                    string? matchedPlayerLine = null;
                    
                    using (var fileReader = new StreamReader(filePath))
                    {
                        string? line;
                        while ((line = await fileReader.ReadLineAsync()) != null)
                        {
                            var split = line.Split(';');
                            if (split.Length >= 2 && 
                                split[0].ToLower() == username.ToLower() && 
                                split[1] == password)
                            {
                                // Instead of just setting a boolean, we grab the whole string
                                matchedPlayerLine = line; 
                                break;
                            }
                        }
                    }

                    // If we found a match, matchedPlayerLine won't be null
                    if (matchedPlayerLine != null)
                    {
                        await tempWriter.WriteLineAsync("Login successful!");
                        return matchedPlayerLine;
                    }
                    
                    await tempWriter.WriteLineAsync("Invalid username or password. Try again.");
                }
            }
        }

        return string.Empty;
    }
}