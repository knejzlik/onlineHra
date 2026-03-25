using System.Net.Sockets;
using onlineHra.Commands;

namespace onlineHra.Networking;

public class Player
{
    public TcpClient Client { get; set; }
    public StreamReader Reader { get; set; }
    public StreamWriter Writer { get; set; }
    public String Username { get; set; }
    public String Password { get; set; }

    public Player(TcpClient client, String username, String password)
    {
        Client = client;
        Username = username;
        Password = password;
        Reader = new StreamReader(Client.GetStream());
        Writer = new StreamWriter(Client.GetStream());
    }

    public void SendMessage(String message)
    {
        Writer.AutoFlush = true;
        Writer.WriteLine(message);
    }
    public async Task SendMessageAsync(String message)
    {
        Writer.AutoFlush = true;
        await Writer.WriteLineAsync(message);
    }

    public static async Task<Player> Connect(TcpClient client)
    {
        var save = await new RegisterOrLogin().Execute(client);
        var splited = save.Split(';');
        return new Player(client, splited[0], splited[1]);
    }

    public  void Disconnect()
    {
        Client.Close();
    }
}