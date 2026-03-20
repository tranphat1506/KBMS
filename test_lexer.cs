using System;
using KBMS.Parser;
class Program {
    static void Main() {
        var lexer = new Lexer("ALTER CONCEPT Person");
        foreach(var t in lexer.Tokenize()) {
            Console.WriteLine($"Type: {t.Type}, Lexeme: {t.Lexeme}");
        }
    }
}
