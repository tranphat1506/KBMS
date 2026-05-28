using System;
using KBMS.Parser;
var parser = new KBMS.Parser.Parser("CREATE CONCEPT S ( VARIABLES ( p: DECIMAL );");
var result = parser.ParseAll();
Console.WriteLine("Parsed!");
