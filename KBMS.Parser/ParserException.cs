namespace KBMS.Parser;

/// <summary>
/// Exception thrown during parsing
/// </summary>
public class ParserException : Exception
{
    /// <summary>
    /// Line number where error occurred
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Column number where error occurred
    /// </summary>
    public int Column { get; }

    public ParserException(string message, int line = 0, int column = 0) : base(message)
    {
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return Column > 0
            ? $"Parse error at line {Line}, column {Column}: {Message}"
            : $"Parse error: {Message}";
    }
}
