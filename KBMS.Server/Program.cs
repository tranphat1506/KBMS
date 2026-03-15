using System;
using System.Threading.Tasks;
using KBMS.Server;

namespace KBMS.Server;

class Program
{
    static async Task Main(string[] args)
    {
        var server = new KbmsServer();

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
