using Microsoft.Extensions.Configuration;
﻿using onlineHra.Networking;

namespace onlineHra;

class Program
{
    static void Main(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(baseDir);
        
        Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"Base directory: {baseDir}");
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        int port = configuration.GetValue<int>("Server:Port", 65525);

        new Server(port).Start();
        Console.Read();
    }

}