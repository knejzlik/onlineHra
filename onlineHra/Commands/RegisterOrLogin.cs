using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using onlineHra.Services;

namespace onlineHra.Commands;

public class RegisterOrLogin : ICommand
{
    public async Task<string> Execute(TcpClient client, PlayerService playerService)
    {
        var tempReader = new StreamReader(client.GetStream());
        var tempWriter = new StreamWriter(client.GetStream()) { AutoFlush = true };

        bool nextStep = true;
        int state = 0;

        while (nextStep)
        {
            await tempWriter.WriteLineAsync("Login nebo register?");
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
            await tempWriter.WriteLineAsync("Neznamy prikaz. Zkus to znovu.");
        }

        switch (state)
        {
            case 1:
                {
                    while (true)
                    {
                        await tempWriter.WriteLineAsync("Zadej uzivatelske jmeno:");
                        await tempWriter.WriteAsync(">>> ");
                        var username = await tempReader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(username)) continue;

                        await tempWriter.WriteLineAsync("Zadej heslo:");
                        await tempWriter.WriteAsync(">>> ");
                        var password = await tempReader.ReadLineAsync();

                        await tempWriter.WriteLineAsync("Zopakuj heslo:");
                        await tempWriter.WriteAsync(">>> ");
                        var password2 = await tempReader.ReadLineAsync();

                        if (password != password2)
                        {
                            await tempWriter.WriteLineAsync("Hesla se neshoduji.");
                            continue;
                        }

                        if (!playerService.Register(username, password))
                        {
                            await tempWriter.WriteLineAsync("Uzivatelske jmeno uz existuje.");
                            continue;
                        }

                        await tempWriter.WriteLineAsync("Registrace uspesna!");

                        return $"{username};{password}";
                    }
                }
            case 2:
                {
                    while (true)
                    {
                        await tempWriter.WriteLineAsync("Zadej uzivatelske jmeno:");
                        await tempWriter.WriteAsync(">>> ");
                        var username = await tempReader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(username)) return string.Empty;

                        await tempWriter.WriteLineAsync("Zadej heslo:");
                        await tempWriter.WriteAsync(">>> ");
                        var password = await tempReader.ReadLineAsync();

                        var playerState = playerService.Login(username, password);

                        if (playerState != null)
                        {
                            await tempWriter.WriteLineAsync("Prihlaseni uspesne!");
                            return $"{playerState.Username};{playerState.PasswordHash}";
                        }

                        await tempWriter.WriteLineAsync("Spatne jmeno nebo heslo. Zkus to znovu.");
                    }
                }
        }

        return string.Empty;
    }

    public async Task<string> Execute(TcpClient client)
    {
        return await Execute(client, new PlayerService());
    }
}