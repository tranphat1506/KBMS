using KBMS.Models;

namespace KBMS.Parser;

/// <summary>
/// Exception thrown during parsing
/// </summary>
public class ParserException : Exception
{
    /// <summary>
    /// Structured error response
    /// </summary>
    public ErrorResponse Response { get; }

    /// <summary>
    /// Line number where error occurred
    /// </summary>
    public int Line => Response.Line ?? 0;

    /// <summary>
    /// Column number where error occurred
    /// </summary>
    public int Column => Response.Column ?? 0;

    public ParserException(ErrorResponse response) : base(response.Message)
    {
        Response = response;
    }

    public ParserException(string message, int line = 0, int column = 0) 
        : this(new ErrorResponse { Type = "ParserError", Message = message, Line = line, Column = column })
    {
    }

    public override string ToString()
    {
        return Column > 0
            ? $"Parse error at line {Line}, column {Column}: {Message}"
            : $"Parse error: {Message}";
    }
}
