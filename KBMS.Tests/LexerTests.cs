using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;

using System;
using KBMS.Parser;
using Xunit;

namespace KBMS.Tests;

/// <summary>
/// Unit tests for the KBQL Lexer
/// Tests tokenization of all KBQL keywords, operators, and literals
/// </summary>
public class LexerTests
{
    private List<Token> Tokenize(string input)
    {
        var lexer = new Lexer(input);
        return lexer.Tokenize();
    }

    // ==================== BASIC TOKEN TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeEOF()
    {
        var tokens = Tokenize("");
        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeWhitespace()
    {
        var tokens = Tokenize("   \t\n  ");
        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
    }

    // ==================== LITERAL TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeIdentifier()
    {
        var tokens = Tokenize("myIdentifier");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(TokenType.IDENTIFIER, tokens[0].Type);
        Assert.Equal("myIdentifier", tokens[0].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeIdentifierWithUnderscore()
    {
        var tokens = Tokenize("my_identifier");
        Assert.Equal(TokenType.IDENTIFIER, tokens[0].Type);
        Assert.Equal("my_identifier", tokens[0].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeNumber()
    {
        var tokens = Tokenize("123");
        Assert.Equal(TokenType.NUMBER, tokens[0].Type);
        Assert.Equal("123", tokens[0].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeNegativeNumber()
    {
        var tokens = Tokenize("-456");
        Assert.Equal(TokenType.NUMBER, tokens[0].Type);
        Assert.Equal("-456", tokens[0].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeFloat()
    {
        var tokens = Tokenize("3.14159");
        Assert.Equal(TokenType.NUMBER, tokens[0].Type);
        Assert.Equal("3.14159", tokens[0].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeString_SingleQuotes()
    {
        var tokens = Tokenize("'hello world'");
        Assert.Equal(TokenType.STRING, tokens[0].Type);
        Assert.Equal("hello world", tokens[0].Literal);
    }

    [Fact]
    public void Lexer_ShouldTokenizeString_DoubleQuotes()
    {
        var tokens = Tokenize("\"hello world\"");
        Assert.Equal(TokenType.STRING, tokens[0].Type);
        Assert.Equal("hello world", tokens[0].Literal);
    }

    [Fact]
    public void Lexer_ShouldTokenizeBoolean_True()
    {
        var tokens = Tokenize("true");
        Assert.Equal(TokenType.BOOLEAN, tokens[0].Type);
        Assert.Equal("true", tokens[0].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeBoolean_False()
    {
        var tokens = Tokenize("false");
        Assert.Equal(TokenType.BOOLEAN, tokens[0].Type);
        Assert.Equal("false", tokens[0].Lexeme);
    }

    // ==================== DDL KEYWORD TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeCREATE()
    {
        var tokens = Tokenize("CREATE");
        Assert.Equal(TokenType.CREATE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeDROP()
    {
        var tokens = Tokenize("DROP");
        Assert.Equal(TokenType.DROP, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeUSE()
    {
        var tokens = Tokenize("USE");
        Assert.Equal(TokenType.USE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeADD()
    {
        var tokens = Tokenize("ADD");
        Assert.Equal(TokenType.ADD, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeREMOVE()
    {
        var tokens = Tokenize("REMOVE");
        Assert.Equal(TokenType.REMOVE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeKNOWLEDGE()
    {
        var tokens = Tokenize("KNOWLEDGE");
        Assert.Equal(TokenType.KNOWLEDGE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeBASE()
    {
        var tokens = Tokenize("BASE");
        Assert.Equal(TokenType.BASE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeCONCEPT()
    {
        var tokens = Tokenize("CONCEPT");
        Assert.Equal(TokenType.CONCEPT, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeRULE()
    {
        var tokens = Tokenize("RULE");
        Assert.Equal(TokenType.RULE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeRELATION()
    {
        var tokens = Tokenize("RELATION");
        Assert.Equal(TokenType.RELATION, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeOPERATOR()
    {
        var tokens = Tokenize("OPERATOR");
        Assert.Equal(TokenType.OPERATOR, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeFUNCTION()
    {
        var tokens = Tokenize("FUNCTION");
        Assert.Equal(TokenType.FUNCTION, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeUSER()
    {
        var tokens = Tokenize("USER");
        Assert.Equal(TokenType.USER, tokens[0].Type);
    }

    // ==================== DML KEYWORD TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeSELECT()
    {
        var tokens = Tokenize("SELECT");
        Assert.Equal(TokenType.SELECT, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeINSERT()
    {
        var tokens = Tokenize("INSERT");
        Assert.Equal(TokenType.INSERT, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeUPDATE()
    {
        var tokens = Tokenize("UPDATE");
        Assert.Equal(TokenType.UPDATE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeDELETE()
    {
        var tokens = Tokenize("DELETE");
        Assert.Equal(TokenType.DELETE, tokens[0].Type);
    }


    [Fact]
    public void Lexer_ShouldTokenizeSHOW()
    {
        var tokens = Tokenize("SHOW");
        Assert.Equal(TokenType.SHOW, tokens[0].Type);
    }

    // ==================== CLAUSE KEYWORD TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeWHERE()
    {
        var tokens = Tokenize("WHERE");
        Assert.Equal(TokenType.WHERE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeFROM()
    {
        var tokens = Tokenize("FROM");
        Assert.Equal(TokenType.FROM, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeINTO()
    {
        var tokens = Tokenize("INTO");
        Assert.Equal(TokenType.INTO, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeVALUES()
    {
        var tokens = Tokenize("VALUES");
        Assert.Equal(TokenType.VALUES, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeSET()
    {
        var tokens = Tokenize("SET");
        Assert.Equal(TokenType.SET, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeJOIN()
    {
        var tokens = Tokenize("JOIN");
        Assert.Equal(TokenType.JOIN, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeON()
    {
        var tokens = Tokenize("ON");
        Assert.Equal(TokenType.ON, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeORDER()
    {
        var tokens = Tokenize("ORDER");
        Assert.Equal(TokenType.ORDER, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeBY()
    {
        var tokens = Tokenize("BY");
        Assert.Equal(TokenType.BY, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeLIMIT()
    {
        var tokens = Tokenize("LIMIT");
        Assert.Equal(TokenType.LIMIT, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeOFFSET()
    {
        var tokens = Tokenize("OFFSET");
        Assert.Equal(TokenType.OFFSET, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeAS()
    {
        var tokens = Tokenize("AS");
        Assert.Equal(TokenType.AS, tokens[0].Type);
    }

    // ==================== DATA TYPE TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeINT()
    {
        var tokens = Tokenize("INT");
        Assert.Equal(TokenType.INT, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeDOUBLE()
    {
        var tokens = Tokenize("DOUBLE");
        Assert.Equal(TokenType.DOUBLE, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeVARCHAR()
    {
        var tokens = Tokenize("VARCHAR");
        Assert.Equal(TokenType.VARCHAR, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeTEXT()
    {
        var tokens = Tokenize("TEXT");
        Assert.Equal(TokenType.TEXT, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeBOOLEAN_TYPE()
    {
        var tokens = Tokenize("BOOLEAN");
        Assert.Equal(TokenType.BOOLEAN_TYPE, tokens[0].Type);
    }

    // ==================== OPERATOR TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizePlus()
    {
        var tokens = Tokenize("+");
        Assert.Equal(TokenType.PLUS, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeMinus()
    {
        var tokens = Tokenize("-");
        Assert.Equal(TokenType.MINUS, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeStar()
    {
        var tokens = Tokenize("*");
        Assert.Equal(TokenType.STAR, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeSlash()
    {
        var tokens = Tokenize("/");
        Assert.Equal(TokenType.SLASH, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeEquals()
    {
        var tokens = Tokenize("=");
        Assert.Equal(TokenType.EQUALS, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeNotEquals()
    {
        var tokens = Tokenize("<>");
        Assert.Equal(TokenType.NOT_EQUALS, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeNotEqualsAlt()
    {
        var tokens = Tokenize("!=");
        Assert.Equal(TokenType.NOT_EQUALS, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeGreater()
    {
        var tokens = Tokenize(">");
        Assert.Equal(TokenType.GREATER, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeLess()
    {
        var tokens = Tokenize("<");
        Assert.Equal(TokenType.LESS, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeGreaterEqual()
    {
        var tokens = Tokenize(">=");
        Assert.Equal(TokenType.GREATER_EQUAL, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeLessEqual()
    {
        var tokens = Tokenize("<=");
        Assert.Equal(TokenType.LESS_EQUAL, tokens[0].Type);
    }

    // ==================== PUNCTUATION TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeLParen()
    {
        var tokens = Tokenize("(");
        Assert.Equal(TokenType.LPAREN, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeRParen()
    {
        var tokens = Tokenize(")");
        Assert.Equal(TokenType.RPAREN, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeComma()
    {
        var tokens = Tokenize(",");
        Assert.Equal(TokenType.COMMA, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeSemicolon()
    {
        var tokens = Tokenize(";");
        Assert.Equal(TokenType.SEMICOLON, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeColon()
    {
        var tokens = Tokenize(":");
        Assert.Equal(TokenType.COLON, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeDot()
    {
        var tokens = Tokenize(".");
        Assert.Equal(TokenType.DOT, tokens[0].Type);
    }

    // ==================== COMPLEX EXPRESSION TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeSimpleQuery()
    {
        var tokens = Tokenize("SELECT * FROM Person");

        Assert.Equal(5, tokens.Count);
        Assert.Equal(TokenType.SELECT, tokens[0].Type);
        Assert.Equal(TokenType.STAR, tokens[1].Type);
        Assert.Equal(TokenType.FROM, tokens[2].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[3].Type);
        Assert.Equal("Person", tokens[3].Lexeme);
    }

    [Fact]
    public void Lexer_ShouldTokenizeCreateKbQuery()
    {
        var tokens = Tokenize("CREATE KNOWLEDGE BASE myKb DESCRIPTION 'Test KB'");

        Assert.Equal(TokenType.CREATE, tokens[0].Type);
        Assert.Equal(TokenType.KNOWLEDGE, tokens[1].Type);
        Assert.Equal(TokenType.BASE, tokens[2].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[3].Type);
        Assert.Equal("myKb", tokens[3].Lexeme);
        Assert.Equal(TokenType.DESCRIPTION, tokens[4].Type);
        Assert.Equal(TokenType.STRING, tokens[5].Type);
        Assert.Equal("Test KB", tokens[5].Literal);
    }

    [Fact]
    public void Lexer_ShouldTokenizeInsertQuery()
    {
        var tokens = Tokenize("INSERT INTO Person VALUES ('John', 30)");

        Assert.Equal(TokenType.INSERT, tokens[0].Type);
        Assert.Equal(TokenType.INTO, tokens[1].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[2].Type);
        Assert.Equal(TokenType.VALUES, tokens[3].Type);
        Assert.Equal(TokenType.LPAREN, tokens[4].Type);
        Assert.Equal(TokenType.STRING, tokens[5].Type);
        Assert.Equal("John", tokens[5].Literal);
        Assert.Equal(TokenType.COMMA, tokens[6].Type);
        Assert.Equal(TokenType.NUMBER, tokens[7].Type);
        Assert.Equal("30", tokens[7].Lexeme);
        Assert.Equal(TokenType.RPAREN, tokens[8].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeWhereClause()
    {
        var tokens = Tokenize("WHERE age >= 18 AND name = 'John'");

        Assert.Equal(TokenType.WHERE, tokens[0].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[1].Type);
        Assert.Equal("age", tokens[1].Lexeme);
        Assert.Equal(TokenType.GREATER_EQUAL, tokens[2].Type);
        Assert.Equal(TokenType.NUMBER, tokens[3].Type);
        Assert.Equal(TokenType.AND, tokens[4].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[5].Type);
        Assert.Equal(TokenType.EQUALS, tokens[6].Type);
        Assert.Equal(TokenType.STRING, tokens[7].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeUpdateQuery()
    {
        var tokens = Tokenize("UPDATE Person SET age = 31 WHERE name = 'John'");

        Assert.Equal(TokenType.UPDATE, tokens[0].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[1].Type);
        Assert.Equal(TokenType.SET, tokens[2].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[3].Type);
        Assert.Equal(TokenType.EQUALS, tokens[4].Type);
        Assert.Equal(TokenType.NUMBER, tokens[5].Type);
        Assert.Equal(TokenType.WHERE, tokens[6].Type);
    }

    [Fact]
    public void Lexer_ShouldTokenizeDeleteQuery()
    {
        var tokens = Tokenize("DELETE FROM Person WHERE id = 1");

        Assert.Equal(TokenType.DELETE, tokens[0].Type);
        Assert.Equal(TokenType.FROM, tokens[1].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[2].Type);
        Assert.Equal(TokenType.WHERE, tokens[3].Type);
    }

    // ==================== CASE INSENSITIVITY TESTS ====================

    [Fact]
    public void Lexer_ShouldBeCaseInsensitiveForKeywords()
    {
        var tokens1 = Tokenize("SELECT");
        var tokens2 = Tokenize("select");
        var tokens3 = Tokenize("Select");

        Assert.Equal(TokenType.SELECT, tokens1[0].Type);
        Assert.Equal(TokenType.SELECT, tokens2[0].Type);
        Assert.Equal(TokenType.SELECT, tokens3[0].Type);
    }

    // ==================== COMMENT TESTS ====================

    [Fact]
    public void Lexer_ShouldHandleSingleLineComment()
    {
        var tokens = Tokenize("SELECT * FROM Person -- this is a comment");

        // Should have tokens up to Person, then EOF
        Assert.Equal(TokenType.SELECT, tokens[0].Type);
        Assert.Equal(TokenType.STAR, tokens[1].Type);
        Assert.Equal(TokenType.FROM, tokens[2].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[3].Type);
        Assert.Equal(TokenType.EOF, tokens[4].Type);
    }

    // ==================== ERROR HANDLING TESTS ====================

    [Fact]
    public void Lexer_ShouldTokenizeUnknownChar()
    {
        var tokens = Tokenize("@");
        Assert.Equal(TokenType.UNKNOWN, tokens[0].Type);
    }

    [Fact]
    public void Lexer_ShouldHandleMultipleTokens()
    {
        var tokens = Tokenize("a, b, c");

        Assert.Equal(TokenType.IDENTIFIER, tokens[0].Type);
        Assert.Equal("a", tokens[0].Lexeme);
        Assert.Equal(TokenType.COMMA, tokens[1].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[2].Type);
        Assert.Equal("b", tokens[2].Lexeme);
        Assert.Equal(TokenType.COMMA, tokens[3].Type);
        Assert.Equal(TokenType.IDENTIFIER, tokens[4].Type);
        Assert.Equal("c", tokens[4].Lexeme);
    }
}
