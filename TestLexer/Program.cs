using System;
using KBMS.Parser;

class Program
{
    static void Main()
    {
        string text = "-- 9. Complex Queries";
        try {
            var lexer = new Lexer(text);
            var tokens = lexer.Tokenize();
            Console.WriteLine("Tokenized successfully: " + tokens.Count + " tokens.");
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            Console.WriteLine("Parsed successfully: " + (ast == null ? "null" : ast.Type));
        } catch (Exception ex) {
            Console.WriteLine("EXCEPTION: " + ex);
        }
    }
}
