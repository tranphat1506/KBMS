using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KBMS.Server;

class Program
{
    static async Task Main(string[] args)
    {
        // Check for flags
        if (args.Length > 0 && args[0] == "--update")
        {
            Console.WriteLine(">>> Update Mode Detected.");
            string exeDir = AppContext.BaseDirectory;
            string configPath = Path.Combine(exeDir, "kbms.ini");
            var config = ConfigManager.Load(configPath);
            var server = new KbmsServer(config);
            server.RunUpdate();
            return;
        }

        using IHost host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "KBMSServer";
            })
            .ConfigureServices(services =>
            {
                services.AddHostedService<KbmsHostedService>();
            })
            .Build();

        await host.RunAsync();
    }
}
