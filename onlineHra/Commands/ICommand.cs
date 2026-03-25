using System.Net.Sockets;

namespace onlineHra;

public interface ICommand
{
    public string Execute(TcpClient client);

}