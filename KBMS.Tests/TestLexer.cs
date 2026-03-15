using System;
using KBMS.Parser;

var lexer = new Lexer("CREATE CONCEPT Employee VARIABLES (id: INT, name: VARCHAR(100), salary: DOUBLE, active: BOOLEAN)");
var tokens = lexer.Tokenize();

Console.WriteLine("Tokens:");
foreach (var t in tokens)
{
    Console.WriteLine($"  {t.Type}: '{t.Lexeme}'");
}
