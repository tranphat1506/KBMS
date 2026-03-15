namespace KBMS.Models;

public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }

    public ParseException(string message, Exception innerException) : base(message, innerException) { }
}

public class LexerException : Exception
{
    public LexerException(string message) : base(message) { }

    public LexerException(string message, Exception innerException) : base(message, innerException) { }
}
