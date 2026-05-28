using System;
using KBMS.Parser;
using KBMS.Server.Core;
using Xunit;

namespace KBMS.Tests;

public class DebugTests
{
    [Fact]
    public void DebugLsp()
    {
        var engine = new LspEngine(null!); 
        try
        {
            var parser = new KBMS.Parser.Parser("CREATE CONCEPT S ( VARIABLES ( p: DECIMAL );");
            parser.ParseAll();
            Console.WriteLine("NO EXCEPTION!");
        }
        catch(Exception e)
        {
            Console.WriteLine("EXCEPTION: " + e.Message);
        }
    }
}
