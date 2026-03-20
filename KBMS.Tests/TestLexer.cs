using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;

using System;
using KBMS.Parser;

var lexer = new Lexer("CREATE CONCEPT Employee VARIABLES (id: INT, name: VARCHAR(100), salary: DOUBLE, active: BOOLEAN)");
var tokens = lexer.Tokenize();

Console.WriteLine("Tokens:");
foreach (var t in tokens)
{
    Console.WriteLine($"  {t.Type}: '{t.Lexeme}'");
}
