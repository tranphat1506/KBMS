using System;
using System.Threading.Tasks;
using KBMS.CLI;

namespace KBMS.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        var cli = new Cli();
        await cli.StartInteractiveAsync();
    }
}
