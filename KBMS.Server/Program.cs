using System;
using System.Threading.Tasks;
using KBMS.Server;

namespace KBMS.Server;

class Program
{
    static async Task Main(string[] args)
    {
        string configPath = "kbms.ini";
        var config = ConfigManager.Load(configPath);
        var server = new KbmsServer(config);
        
        // Check for flags
        if (args.Length > 0 && args[0] == "--update")
        {
            Console.WriteLine(">>> Update Mode Detected.");
            server.RunUpdate();
            return;
        }


        Console.WriteLine("Starting KBMS Server...");
        Console.WriteLine("Press Ctrl+C to stop the server.");

        var serverTask = server.StartAsync();

        // Handle graceful shutdown
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nShutting down server...");
            server.Stop();
        };

        await serverTask;
    }
}
