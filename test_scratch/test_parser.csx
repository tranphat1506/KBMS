using System;
using System.IO;
using System.Reflection;
using KBMS.Parser;

var text = "INSERT INTO SpaceBody VARIABLES (name: 'StarX', mass: 1000000.0);";
Console.WriteLine($"Parsing: {text}");

var lexer = new Lexer(text);
var tokens = lexer.Tokenize();
Console.WriteLine("Tokens:");
foreach (var t in tokens) {
    Console.WriteLine($"[{t.Type}] {t.Lexeme} (L:{t.Line} C:{t.Column})");
}

var parser = new Parser(tokens);
try {
    var ast = parser.Parse();
    Console.WriteLine($"Parsed successfully: {ast.Count} statements");
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
