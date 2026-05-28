using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace KBMS.Server;

public class KbmsHostedService : BackgroundService
{
    private readonly KbmsServer _server;

    public KbmsHostedService()
    {
        // Get absolute path of the directory containing the exe
        string exeDir = AppContext.BaseDirectory;
        string configPath = Path.Combine(exeDir, "kbms.ini");
        
        var config = ConfigManager.Load(configPath);
        _server = new KbmsServer(config);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register the stop action when the service receives a stop signal
        stoppingToken.Register(() => 
        {
            Console.WriteLine("\nShutting down KBMS Server...");
            _server.Stop();
        });

        Console.WriteLine("Starting KBMS Server (Service/Console)...");
        
        // Start the server and await it
        await _server.StartAsync();
    }
}
