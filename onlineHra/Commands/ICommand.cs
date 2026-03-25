using System.Net.Sockets;

namespace onlineHra;

public interface ICommand
{
    public Task<string> Execute(TcpClient client);

}