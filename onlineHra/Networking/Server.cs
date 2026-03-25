using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks.Dataflow;
using onlineHra.Commands;

namespace onlineHra.Networking;

public class Server
{
    private TcpListener server;
    private HashSet<TcpClient> connections;
    private Dictionary<string, ICommand> commands;

    public Server(int port)
    {
        commands = new Dictionary<string, ICommand>();
        connections = new HashSet<TcpClient>();
        server = new TcpListener(System.Net.IPAddress.Any, port);
        server.Start();
        Loop();
        commands.Add("help",new HelpCommand());
        CheckForDisconnects();
    }

    private async void Loop()
    {
        Console.WriteLine("Server spusten");
        while (true)
        { 
             TcpClient tcpc;
             Task<TcpClient> t = server.AcceptTcpClientAsync();
             await t;
             tcpc = t.Result;
            connections.Add(tcpc);
            ClientLoop(tcpc);
        }
    }

    private async void ClientLoop(TcpClient client)
    {
        StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8);
        StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.UTF8);
        writer.AutoFlush = true;
        while (true)
        {
            await writer.WriteAsync(">>> ");
            string? inp;
            var t = reader.ReadLineAsync();
            await t;
            inp = t.Result;
            Console.WriteLine(inp);
            if (inp=="help")
            {
                await writer.WriteAsync(commands["help"].Execute(client)+"\n");
            }
        }
    }

    private void Disconnect(TcpClient client)
    {
        connections.Remove(client);
    }

    private async void CheckForDisconnects()
    {
        foreach (var client in connections)
        {
            if (!client.Connected)
            {
                Disconnect(client);
            }
        }

        await Task.Delay(10000);
    }
}