using onlineHra.Networking;

namespace onlineHra;

class Program
{
    static void Main(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(baseDir);
        
        Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"Base directory: {baseDir}");
        
        new Server(65525).Start();
        Console.Read();
    }
}