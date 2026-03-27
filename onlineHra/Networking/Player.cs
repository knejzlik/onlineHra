using System.Net.Sockets;
using onlineHra.Models;

namespace onlineHra.Networking;

public class Player
{
    public TcpClient Client { get; set; }
    public StreamReader Reader { get; set; }
    public StreamWriter Writer { get; set; }
    public PlayerState State { get; set; }
    public string CurrentRoomId { get; set; } = "start";

    public Player(TcpClient client, PlayerState state)
    {
        Client = client;
        State = state;
        CurrentRoomId = state.CurrentRoomId;
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

    public static async Task<Player?> Connect(TcpClient client, Services.PlayerService playerService)
    {
        var save = await new Commands.RegisterOrLogin().Execute(client, playerService);
        if (string.IsNullOrEmpty(save)) return null;
        
        var splited = save.Split(';');
        if (splited.Length < 2) return null;
        
        var username = splited[0];
        var playerState = playerService.GetPlayer(username);
        if (playerState == null) return null;
        
        // Ensure player has a valid current room
        if (string.IsNullOrEmpty(playerState.CurrentRoomId))
        {
            playerState.CurrentRoomId = "start";
            playerService.SavePlayer(playerState);
        }
        
        return new Player(client, playerState);
    }

    public void Disconnect()
    {
        Client.Close();
    }
}