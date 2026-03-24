using System;
using System.Threading.Tasks;
using KBMS.Server;

namespace KBMS.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var server = new KbmsServer();
        
        // Check for flags
        if (args.Length > 0 && args[0] == "--update")
        {
            Console.WriteLine(">>> Update Mode Detected.");
            server.RunUpdate();
            return;
        }

        if (args.Length >= 3 && args[0] == "--migrate-v2")
        {
            Console.WriteLine(">>> Migration Mode Detected (V2 -> V3).");
            string path = args[1];
            string key = args[2];
            server.MigrateV2(path, key);
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
