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
        /*Restart serveru- hrac napise hrat znovu nebo enter a spawne te na zacatku a itemy se vrati, nova instance tridy
         * kdyz vyhraje napise to vsem ze vyhrali
         * Anglictina
         * Na vyhru globalne ty itemy
         * command odevzdej
         * 
         * 
          */
}