using System;
using System.Linq;

var source = "INSERT INTO Student VARIABLES(name: 'Alice', grade: 95);";
var lexer = new KBMS.Parser.Lexer(source);
var tokens = lexer.Tokenize();
foreach(var t in tokens) Console.WriteLine($"{t.Type}: {t.Lexeme}");
