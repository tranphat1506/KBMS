using System;
using System.Collections.Generic;
using System.Text;

namespace KBMS.Parser;

/// <summary>
/// Lexer for KBQL (KBDDL + KBDML)
/// Tokenizes input text into tokens for the parser
/// </summary>
public class Lexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = new();

    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // DDL Keywords
        { "CREATE", TokenType.CREATE },
        { "DROP", TokenType.DROP },
        { "USE", TokenType.USE },
        { "ADD", TokenType.ADD },
        { "REMOVE", TokenType.REMOVE },

        // Object Keywords
        { "KNOWLEDGE", TokenType.KNOWLEDGE },
        { "BASE", TokenType.BASE },
        { "CONCEPT", TokenType.CONCEPT },
        { "CONCEPTS", TokenType.CONCEPTS },
        { "RULE", TokenType.RULE },
        { "RULES", TokenType.RULES },
        { "RELATION", TokenType.RELATION },
        { "RELATIONS", TokenType.RELATIONS },
        { "OPERATOR", TokenType.OPERATOR },
        { "OPERATORS", TokenType.OPERATORS },
        { "FUNCTION", TokenType.FUNCTION },
        { "FUNCTIONS", TokenType.FUNCTIONS },
        { "USER", TokenType.USER },
        { "USERS", TokenType.USERS },

        // DML/KML Keywords
        { "SELECT", TokenType.SELECT },
        { "INSERT", TokenType.INSERT },
        { "UPDATE", TokenType.UPDATE },
        { "DELETE", TokenType.DELETE },
        { "SOLVE", TokenType.SOLVE },
        { "SHOW", TokenType.SHOW },
        { "ATTRIBUTE", TokenType.ATTRIBUTE },

        // TCL Keywords
        { "BEGIN", TokenType.BEGIN },
        { "TRANSACTION", TokenType.TRANSACTION },
        { "COMMIT", TokenType.COMMIT },
        { "ROLLBACK", TokenType.ROLLBACK },

        // Clause Keywords
        { "WHERE", TokenType.WHERE },
        { "FROM", TokenType.FROM },
        { "TO", TokenType.TO },
        { "INTO", TokenType.INTO },
        { "VALUES", TokenType.VALUES },
        { "SET", TokenType.SET },
        { "JOIN", TokenType.JOIN },
        { "ON", TokenType.ON },
        { "ORDER", TokenType.ORDER },
        { "BY", TokenType.BY },
        { "GROUP", TokenType.GROUP },
        { "HAVING", TokenType.HAVING },
        { "LIMIT", TokenType.LIMIT },
        { "OFFSET", TokenType.OFFSET },
        { "AS", TokenType.AS },

        // Concept Definition Keywords
        { "VARIABLE", TokenType.VARIABLE },
        { "VARIABLES", TokenType.VARIABLES },
        { "ALIASES", TokenType.ALIASES },
        { "BASE_OBJECTS", TokenType.BASE_OBJECTS },
        { "CONSTRAINTS", TokenType.CONSTRAINTS },
        { "SAME_VARIABLES", TokenType.SAME_VARIABLES },
        { "CONSTRUCT_RELATIONS", TokenType.CONSTRUCT_RELATIONS },

        // Hierarchy Keywords
        { "HIERARCHY", TokenType.HIERARCHY },
        { "IS_A", TokenType.IS_A },
        { "PART_OF", TokenType.PART_OF },

        // Relation/Function/Operator Keywords
        { "PARAMS", TokenType.PARAMS },
        { "RETURNS", TokenType.RETURNS },
        { "BODY", TokenType.BODY },
        { "PROPERTIES", TokenType.PROPERTIES },

        // Computation Keywords
        { "COMPUTATION", TokenType.COMPUTATION },
        { "FORMULA", TokenType.FORMULA },
        { "COST", TokenType.COST },
        { "EQUATION", TokenType.EQUATION },
        { "EQUATIONS", TokenType.EQUATIONS },

        // Rule Keywords
        { "TYPE", TokenType.TYPE },
        { "SCOPE", TokenType.SCOPE },
        { "IF", TokenType.IF },
        { "THEN", TokenType.THEN },

        // User/Privilege Keywords
        { "PASSWORD", TokenType.PASSWORD },
        { "ROLE", TokenType.ROLE },
        { "SYSTEM_ADMIN", TokenType.SYSTEM_ADMIN },
        { "GRANT", TokenType.GRANT },
        { "REVOKE", TokenType.REVOKE },
        { "PRIVILEGES", TokenType.PRIVILEGES },
        { "IN", TokenType.IN },

        // Solve Keywords
        { "FOR", TokenType.FOR },
        { "GIVEN", TokenType.GIVEN },
        { "USING", TokenType.USING },
        { "FIND", TokenType.FIND },
        { "SAVE", TokenType.SAVE },

        // Aggregation Keywords
        { "COUNT", TokenType.COUNT },
        { "SUM", TokenType.SUM },
        { "AVG", TokenType.AVG },
        { "MAX", TokenType.MAX },
        { "MIN", TokenType.MIN },

        // Logical Keywords
        { "AND", TokenType.AND },
        { "OR", TokenType.OR },
        { "NOT", TokenType.NOT },

        // Other Keywords
        { "DESCRIPTION", TokenType.DESCRIPTION },
        { "ASC", TokenType.ASC },
        { "DESC", TokenType.DESC },

        // Data Types - Numeric
        { "TINYINT", TokenType.TINYINT },
        { "SMALLINT", TokenType.SMALLINT },
        { "INT", TokenType.INT },
        { "BIGINT", TokenType.BIGINT },
        { "FLOAT", TokenType.FLOAT },
        { "DOUBLE", TokenType.DOUBLE },
        { "DECIMAL", TokenType.DECIMAL },

        // Data Types - String
        { "VARCHAR", TokenType.VARCHAR },
        { "CHAR", TokenType.CHAR },
        { "TEXT", TokenType.TEXT },
        { "STRING", TokenType.STRING },

        // Data Types - Boolean
        { "BOOLEAN", TokenType.BOOLEAN_TYPE },

        // Data Types - Date/Time
        { "DATE", TokenType.DATE },
        { "DATETIME", TokenType.DATETIME },
        { "TIMESTAMP", TokenType.TIMESTAMP },

        // Data Types - Reference
        { "OBJECT", TokenType.OBJECT_TYPE },

        // Special
        { "NULL", TokenType.NULL_TOKEN },
        { "TRUE", TokenType.BOOLEAN },
        { "FALSE", TokenType.BOOLEAN }
    };

    public Lexer(string source)
    {
        _source = source;
    }

    /// <summary>
    /// Tokenize the source and return list of tokens
    /// </summary>
    public List<Token> Tokenize()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.EOF, "", null, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Advance();

        switch (c)
        {
            // Single-character tokens
            case ' ':
            case '\t':
            case '\r':
                // Ignore whitespace
                break;

            case '\n':
                _line++;
                _column = 1;
                break;

            // Punctuation
            case '(': AddToken(TokenType.LPAREN); break;
            case ')': AddToken(TokenType.RPAREN); break;
            case '[': AddToken(TokenType.LBRACKET); break;
            case ']': AddToken(TokenType.RBRACKET); break;
            case '{': AddToken(TokenType.LBRACE); break;
            case '}': AddToken(TokenType.RBRACE); break;
            case ',': AddToken(TokenType.COMMA); break;
            case ';': AddToken(TokenType.SEMICOLON); break;
            case '.': AddToken(TokenType.DOT); break;

            // Operators
            case '+': AddToken(TokenType.PLUS); break;
            case '-':
                if (Match('-'))
                {
                    // Comment - skip until end of line
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else if (IsDigit(Peek()))
                {
                    // Negative number
                    NumberLiteral();
                }
                else
                {
                    AddToken(TokenType.MINUS);
                }
                break;
            case '*': AddToken(TokenType.STAR); break;
            case '/': AddToken(TokenType.SLASH); break;
            case '^': AddToken(TokenType.CARET); break;
            case '%': AddToken(TokenType.PERCENT); break;

            // Comparison operators
            case '=': AddToken(TokenType.EQUALS); break;
            case '!':
                if (Match('='))
                {
                    AddToken(TokenType.NOT_EQUALS);
                }
                else
                {
                    throw new ParseException($"Unexpected character '!'", _line, _column);
                }
                break;
            case '<':
                if (Match('>'))
                {
                    AddToken(TokenType.NOT_EQUALS);
                }
                else if (Match('='))
                {
                    AddToken(TokenType.LESS_EQUAL);
                }
                else
                {
                    AddToken(TokenType.LESS);
                }
                break;
            case '>':
                if (Match('='))
                {
                    AddToken(TokenType.GREATER_EQUAL);
                }
                else
                {
                    AddToken(TokenType.GREATER);
                }
                break;

            // String literals
            case '\'': StringLiteral('\''); break;
            case '"': StringLiteral('"'); break;
            case ':': AddToken(TokenType.COLON); break;

            default:
                if (IsDigit(c))
                {
                    NumberLiteral();
                }
                else if (IsAlpha(c) || c == '_')
                {
                    Identifier();
                }
                else
                {
                    // Unknown character - create UNKNOWN token
                    AddToken(TokenType.UNKNOWN, c.ToString());
                }
                break;
        }
    }

    private void StringLiteral(char quote)
    {
        var sb = new StringBuilder();

        while (Peek() != quote && !IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _line++;
                _column = 1;
            }

            // Handle escape sequences
            if (Peek() == quote && PeekNext() == quote)
            {
                Advance(); // Skip first quote
                sb.Append(Advance()); // Add second quote as literal
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
        {
            throw new ParseException("Unterminated string", _line, _column);
        }

        Advance(); // Closing quote

        AddToken(TokenType.STRING, sb.ToString());
    }

    private void NumberLiteral()
    {
        int startColumn = _column - 1;

        // Integer part
        while (IsDigit(Peek())) Advance();

        // Decimal part
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // Consume '.'
            while (IsDigit(Peek())) Advance();
        }

        // Exponent part
        if (Peek() == 'e' || Peek() == 'E')
        {
            Advance();
            if (Peek() == '+' || Peek() == '-') Advance();
            while (IsDigit(Peek())) Advance();
        }

        var lexeme = _source.Substring(_start, _current - _start);
        if (double.TryParse(lexeme, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            AddToken(TokenType.NUMBER, value);
        }
        else
        {
            throw new ParseException($"Invalid number format: {lexeme}", _line, startColumn);
        }
    }

    private void Identifier()
    {
        while (IsAlphaNumeric(Peek()) || (Peek() == '.' && IsAlphaNumeric(PeekNext())))
        {
            Advance();
        }

        var text = _source.Substring(_start, _current - _start);
        var type = TokenType.IDENTIFIER;

        if (Keywords.TryGetValue(text.ToUpper(), out var keywordType))
        {
            if (keywordType == TokenType.BOOLEAN)
            {
                // true/false
                AddToken(TokenType.BOOLEAN, bool.Parse(text));
            }
            else
            {
                AddToken(keywordType);
            }
        }
        else
        {
            AddToken(TokenType.IDENTIFIER, text);
        }
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;

        _current++;
        _column++;
        return true;
    }

    private char Peek()
    {
        if (IsAtEnd()) return '\0';
        return _source[_current];
    }

    private char PeekNext()
    {
        if (_current + 1 >= _source.Length) return '\0';
        return _source[_current + 1];
    }

    private char Advance()
    {
        var c = _source[_current++];
        _column++;
        return c;
    }

    private void AddToken(TokenType type, object? literal = null)
    {
        var text = _source.Substring(_start, _current - _start);
        _tokens.Add(new Token(type, text, literal, _line, _column - text.Length));
    }

    private bool IsAtEnd()
    {
        return _current >= _source.Length;
    }

    private static bool IsDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool IsAlpha(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }

    private static bool IsAlphaNumeric(char c)
    {
        return IsAlpha(c) || IsDigit(c) || c == '_';
    }
}

/// <summary>
/// Exception thrown during lexing
/// </summary>
public class ParseException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParseException(string message, int line = 0, int column = 0) : base(message)
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
