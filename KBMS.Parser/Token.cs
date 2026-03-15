namespace KBMS.Parser;

/// <summary>
/// Represents a token in the source code
/// </summary>
public class Token
{
    /// <summary>
    /// Type of the token
    /// </summary>
    public TokenType Type { get; set; }

    /// <summary>
    /// Lexeme (the actual text)
    /// </summary>
    public string Lexeme { get; set; } = string.Empty;

    /// <summary>
    /// Literal value (for numbers, strings)
    /// </summary>
    public object? Literal { get; set; }

    /// <summary>
    /// Line number (1-based)
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Column number (1-based)
    /// </summary>
    public int Column { get; set; }

    public Token(TokenType type, string lexeme, object? literal, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"Token({Type}, '{Lexeme}', {Literal}, Line:{Line}, Col:{Column})";
    }
}
