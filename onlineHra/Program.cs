using onlineHra.Networking;

namespace onlineHra;

class Program
{
    static void Main(string[] args)
    {
        new Server(65525);
        Console.Read();
    }
}