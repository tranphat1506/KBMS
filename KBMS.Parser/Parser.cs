using System;
using System.Collections.Generic;
using System.Text;
using KBMS.Parser.Ast;

namespace KBMS.Parser;

/// <summary>
/// Recursive descent parser for KBQL (KBDDL + KBDML)
/// </summary>
public class Parser
{
    private readonly List<Token> _tokens;
    private int _current = 0;
    private string _originalQuery = string.Empty;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Parser(string source)
    {
        var lexer = new Lexer(source);
        _tokens = lexer.Tokenize();
        _originalQuery = source;
    }

    /// <summary>
    /// Parse the tokens into an AST node
    /// </summary>
    public AstNode? Parse()
    {
        if (IsAtEnd()) return null;

        var token = Peek();
        _originalQuery = string.Join(" ", _tokens.ConvertAll(t => t.Lexeme));

        return ParseStatement();
    }

    /// <summary>
    /// Parse multiple statements (semicolon-separated)
    /// </summary>
    public List<AstNode> ParseAll()
    {
        var statements = new List<AstNode>();

        while (!IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                stmt.OriginalQuery = _originalQuery;
                statements.Add(stmt);
            }

            // Consume optional semicolon
            if (Check(TokenType.SEMICOLON)) Advance();
        }

        return statements;
    }

    private AstNode? ParseStatement()
    {
        var token = Peek();

        if (token == null) return null;

        return token.Type switch
        {
            TokenType.CREATE => ParseCreate(),
            TokenType.DROP => ParseDrop(),
            TokenType.USE => ParseUse(),
            TokenType.ADD => ParseAdd(),
            TokenType.REMOVE => ParseRemove(),
            TokenType.GRANT => ParseGrant(),
            TokenType.REVOKE => ParseRevoke(),
            TokenType.SELECT => ParseSelect(),
            TokenType.INSERT => ParseInsert(),
            TokenType.UPDATE => ParseUpdate(),
            TokenType.DELETE => ParseDelete(),
            TokenType.SOLVE => ParseSolve(),
            TokenType.SHOW => ParseShow(),
            _ => throw new ParserException($"Unexpected token: {token.Lexeme}", token.Line, token.Column)
        };
    }

    // ==================== DDL Parsers ====================

    private AstNode ParseCreate()
    {
        Consume(TokenType.CREATE);
        var token = Peek();

        if (token == null)
            throw new ParserException("Expected token after CREATE");

        return token.Type switch
        {
            TokenType.KNOWLEDGE => ParseCreateKnowledgeBase(),
            TokenType.CONCEPT => ParseCreateConcept(),
            TokenType.RELATION => ParseCreateRelation(),
            TokenType.OPERATOR => ParseCreateOperator(),
            TokenType.FUNCTION => ParseCreateFunction(),
            TokenType.RULE => ParseCreateRule(),
            TokenType.USER => ParseCreateUser(),
            _ => throw new ParserException($"Unexpected token after CREATE: {token.Lexeme}", token.Line, token.Column)
        };
    }

    private CreateKbNode ParseCreateKnowledgeBase()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.KNOWLEDGE);
        Consume(TokenType.BASE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
        var node = new CreateKbNode
        {
            Type = "CREATE_KNOWLEDGE_BASE",
            KbName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Optional DESCRIPTION
        if (Check(TokenType.DESCRIPTION))
        {
            Consume(TokenType.DESCRIPTION);
            var descToken = Consume(TokenType.STRING) ?? throw new ParserException("Expected description string");
            node.Description = descToken.Literal?.ToString();
        }

        return node;
    }

    private CreateConceptNode ParseCreateConcept()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.CONCEPT);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        var node = new CreateConceptNode
        {
            Type = "CREATE_CONCEPT",
            ConceptName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse VARIABLES clause
        if (Check(TokenType.VARIABLES))
        {
            Consume(TokenType.VARIABLES);
            if (!Check(TokenType.LPAREN))
                throw new ParserException("Expected '(' after VARIABLES");
            Consume(TokenType.LPAREN);
            while (!Check(TokenType.RPAREN))
            {
                var varNode = ParseVariableDefinition();
                node.Variables.Add(varNode);
                if (!Check(TokenType.RPAREN))
                    Consume(TokenType.COMMA);
            }
            Consume(TokenType.RPAREN);
        }

        // Parse ALIASES clause
        if (Check(TokenType.ALIASES))
        {
            Consume(TokenType.ALIASES);
            node.Aliases = ParseIdentifierList();
        }

        // Parse BASE_OBJECTS clause
        if (Check(TokenType.BASE_OBJECTS))
        {
            Consume(TokenType.BASE_OBJECTS);
            node.BaseObjects = ParseIdentifierList();
        }

        // Parse CONSTRAINTS clause
        if (Check(TokenType.CONSTRAINTS))
        {
            Consume(TokenType.CONSTRAINTS);
            node.Constraints = ParseConstraintList();
        }

        // Parse SAME_VARIABLES clause
        if (Check(TokenType.SAME_VARIABLES))
        {
            Consume(TokenType.SAME_VARIABLES);
            node.SameVariables = ParseSameVariablesList();
        }

        return node;
    }

    private VariableDefinition ParseVariableDefinition()
    {
        // Accept any token as variable name (including keywords)
        var token = Peek();
        if (token == null || token.Type == TokenType.COLON || token.Type == TokenType.COMMA || token.Type == TokenType.RPAREN)
            throw new ParserException("Expected variable name");
        var nameToken = Advance();
        Consume(TokenType.COLON);
        var typeToken = Peek() ?? throw new ParserException("Expected variable type");

        var varDef = new VariableDefinition
        {
            Name = nameToken.Lexeme,
            Type = typeToken.Lexeme.ToUpper()
        };

        // Parse type
        switch (typeToken.Type)
        {
            case TokenType.TINYINT:
            case TokenType.SMALLINT:
            case TokenType.INT:
            case TokenType.BIGINT:
            case TokenType.FLOAT:
            case TokenType.DOUBLE:
            case TokenType.DECIMAL:
            case TokenType.NUMBER:
            case TokenType.VARCHAR:
            case TokenType.CHAR:
            case TokenType.TEXT:
            case TokenType.STRING:
            case TokenType.BOOLEAN_TYPE:
            case TokenType.DATE:
            case TokenType.DATETIME:
            case TokenType.TIMESTAMP:
            case TokenType.OBJECT_TYPE:
            case TokenType.IDENTIFIER:
                Advance();
                break;
            default:
                throw new ParserException($"Expected type, got {typeToken.Lexeme}", typeToken.Line, typeToken.Column);
        }

        // Parse length for VARCHAR, CHAR, DECIMAL
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            var lengthToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected length");
            varDef.Length = (int?)ConvertToDouble(lengthToken.Literal);

            // Parse scale for DECIMAL
            if (Check(TokenType.COMMA))
            {
                Consume(TokenType.COMMA);
                var scaleToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected scale");
                varDef.Scale = (int?)ConvertToDouble(scaleToken.Literal);
            }
            Consume(TokenType.RPAREN);
        }

        return varDef;
    }

    private CreateRelationNode ParseCreateRelation()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.RELATION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected relation name");
        var node = new CreateRelationNode
        {
            Type = "CREATE_RELATION",
            RelationName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.FROM);
        var domainToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected domain concept");
        node.DomainConcept = domainToken.Lexeme;

        Consume(TokenType.TO);
        var rangeToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected range concept");
        node.RangeConcept = rangeToken.Lexeme;

        // Parse PROPERTIES clause
        if (Check(TokenType.PROPERTIES))
        {
            node.Properties = ParseIdentifierList();
        }

        return node;
    }

    private CreateOperatorNode ParseCreateOperator()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.OPERATOR);

        // Operator symbol can be identifier, operator token, or any keyword
        var symbolToken = Peek() ?? throw new ParserException("Expected operator symbol");
        string symbol;

        if (symbolToken.Type == TokenType.IDENTIFIER)
        {
            symbol = symbolToken.Lexeme;
            Advance();
        }
        else if (IsOperatorToken(symbolToken.Type))
        {
            symbol = symbolToken.Lexeme;
            Advance();
        }
        else if (IsKeywordToken(symbolToken.Type))
        {
            // Accept any keyword as an operator symbol
            symbol = symbolToken.Lexeme;
            Advance();
        }
        else
        {
            throw new ParserException($"Expected operator symbol, got {symbolToken.Lexeme}", symbolToken.Line, symbolToken.Column);
        }

        var node = new CreateOperatorNode
        {
            Type = "CREATE_OPERATOR",
            Symbol = symbol,
            Line = token.Line,
            Column = token.Column
        };

        // Parse PARAMS clause
        if (Check(TokenType.PARAMS))
        {
            Consume(TokenType.PARAMS);
            Consume(TokenType.LPAREN);
            while (!Check(TokenType.RPAREN))
            {
                var typeToken = Peek() ?? throw new ParserException("Expected parameter type");
                node.ParamTypes.Add(typeToken.Lexeme.ToUpper());
                Advance();

                if (!Check(TokenType.RPAREN))
                    Consume(TokenType.COMMA);
            }
            Consume(TokenType.RPAREN);
        }

        // Parse RETURNS clause
        if (Check(TokenType.RETURNS))
        {
            Consume(TokenType.RETURNS);
            var returnToken = Peek() ?? throw new ParserException("Expected return type");
            node.ReturnType = returnToken.Lexeme.ToUpper();
            Advance();
        }

        // Parse PROPERTIES clause
        if (Check(TokenType.PROPERTIES))
        {
            node.Properties = ParseIdentifierList();
        }

        return node;
    }

    private CreateFunctionNode ParseCreateFunction()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.FUNCTION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected function name");
        var node = new CreateFunctionNode
        {
            Type = "CREATE_FUNCTION",
            FunctionName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse PARAMS clause
        if (Check(TokenType.PARAMS))
        {
            Consume(TokenType.PARAMS);
            Consume(TokenType.LPAREN);
            while (!Check(TokenType.RPAREN))
            {
                var paramDef = ParseParamDefinition();
                node.Parameters.Add(paramDef);
                if (!Check(TokenType.RPAREN))
                    Consume(TokenType.COMMA);
            }
            Consume(TokenType.RPAREN);
        }

        // Parse RETURNS clause
        if (Check(TokenType.RETURNS))
        {
            Consume(TokenType.RETURNS);
            var returnToken = Peek() ?? throw new ParserException("Expected return type");
            node.ReturnType = returnToken.Lexeme.ToUpper();
            Advance();
        }

        // Parse BODY clause
        if (Check(TokenType.BODY))
        {
            Consume(TokenType.BODY);
            var bodyToken = Consume(TokenType.STRING) ?? throw new ParserException("Expected body string");
            node.Body = bodyToken.Literal?.ToString() ?? "";
        }

        // Parse PROPERTIES clause
        if (Check(TokenType.PROPERTIES))
        {
            node.Properties = ParseIdentifierList();
        }

        return node;
    }

    private ParamDefinition ParseParamDefinition()
    {
        var typeToken = Peek() ?? throw new ParserException("Expected parameter type");
        var paramDef = new ParamDefinition
        {
            Type = typeToken.Lexeme.ToUpper()
        };
        Advance();

        // Parse length for VARCHAR, CHAR, DECIMAL
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            var lengthToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected length");
            paramDef.Length = (int?)ConvertToDouble(lengthToken.Literal);

            if (Check(TokenType.COMMA))
            {
                Consume(TokenType.COMMA);
                var scaleToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected scale");
                paramDef.Scale = (int?)ConvertToDouble(scaleToken.Literal);
            }
            Consume(TokenType.RPAREN);
        }

        // Parse parameter name
        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected parameter name");
        paramDef.Name = nameToken.Lexeme;

        return paramDef;
    }

    private CreateRuleNode ParseCreateRule()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.RULE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected rule name");
        var node = new CreateRuleNode
        {
            Type = "CREATE_RULE",
            RuleName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse TYPE clause
        if (Check(TokenType.TYPE))
        {
            var typeToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected rule type");
            node.RuleType = Enum.Parse<RuleType>(typeToken.Lexeme, true);
        }

        // Parse SCOPE clause
        if (Check(TokenType.SCOPE))
        {
            var scopeToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected scope concept");
            node.ConceptName = scopeToken.Lexeme;
        }

        // Parse IF clause (hypothesis)
        if (Check(TokenType.IF))
        {
            node.Hypothesis = ParseExpressionList();
        }

        // Parse THEN clause (conclusions)
        if (Check(TokenType.THEN))
        {
            node.Conclusions = ParseExpressionList();
        }

        // Parse COST clause
        if (Check(TokenType.COST))
        {
            var costToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected cost number");
            node.Cost = (int?)ConvertToDouble(costToken.Literal);
        }

        return node;
    }

    private CreateUserNode ParseCreateUser()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.USER);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected username");
        var node = new CreateUserNode
        {
            Type = "CREATE_USER",
            Username = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse PASSWORD clause
        if (Check(TokenType.PASSWORD))
        {
            Consume(TokenType.PASSWORD);
            var passToken = Peek();
            if (passToken == null)
                throw new ParserException("Expected password");

            // Accept both STRING (quoted) and IDENTIFIER (unquoted) for password
            if (passToken.Type == TokenType.STRING)
            {
                Advance();
                node.Password = passToken.Literal?.ToString() ?? "";
            }
            else if (passToken.Type == TokenType.IDENTIFIER)
            {
                Advance();
                node.Password = passToken.Lexeme;
            }
            else
            {
                throw new ParserException("Expected password string or identifier");
            }
        }

        // Parse ROLE clause
        if (Check(TokenType.ROLE))
        {
            Consume(TokenType.ROLE);
            // Accept any identifier or keyword as role name
            var roleToken = Peek() ?? throw new ParserException("Expected role");
            Advance();
            node.Role = roleToken.Lexeme.ToUpper();
        }

        // Parse SYSTEM_ADMIN clause
        if (Check(TokenType.SYSTEM_ADMIN))
        {
            var adminToken = Peek() ?? throw new ParserException("Expected boolean");
            node.SystemAdmin = adminToken.Type == TokenType.BOOLEAN && (bool)(adminToken.Literal ?? false);
            Advance();
        }

        return node;
    }

    private AstNode ParseDrop()
    {
        Consume(TokenType.DROP);
        var token = Peek();

        if (token == null)
            throw new ParserException("Expected token after DROP");

        return token.Type switch
        {
            TokenType.KNOWLEDGE => ParseDropKnowledgeBase(),
            TokenType.CONCEPT => ParseDropConcept(),
            TokenType.RELATION => ParseDropRelation(),
            TokenType.OPERATOR => ParseDropOperator(),
            TokenType.FUNCTION => ParseDropFunction(),
            TokenType.RULE => ParseDropRule(),
            TokenType.USER => ParseDropUser(),
            _ => throw new ParserException($"Unexpected token after DROP: {token.Lexeme}", token.Line, token.Column)
        };
    }

    private DropKbNode ParseDropKnowledgeBase()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.KNOWLEDGE);
        Consume(TokenType.BASE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
        return new DropKbNode
        {
            Type = "DROP_KNOWLEDGE_BASE",
            KbName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private DropConceptNode ParseDropConcept()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.CONCEPT);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        return new DropConceptNode
        {
            Type = "DROP_CONCEPT",
            ConceptName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private DropRelationNode ParseDropRelation()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.RELATION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected relation name");
        return new DropRelationNode
        {
            Type = "DROP_RELATION",
            RelationName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private DropOperatorNode ParseDropOperator()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.OPERATOR);

        var symbolToken = Peek() ?? throw new ParserException("Expected operator symbol");
        string symbol;

        if (symbolToken.Type == TokenType.IDENTIFIER)
        {
            symbol = symbolToken.Lexeme;
            Advance();
        }
        else if (IsOperatorToken(symbolToken.Type))
        {
            symbol = symbolToken.Lexeme;
            Advance();
        }
        else if (IsKeywordToken(symbolToken.Type))
        {
            // Accept any keyword as an operator symbol
            symbol = symbolToken.Lexeme;
            Advance();
        }
        else
        {
            throw new ParserException($"Expected operator symbol, got {symbolToken.Lexeme}", symbolToken.Line, symbolToken.Column);
        }

        return new DropOperatorNode
        {
            Type = "DROP_OPERATOR",
            Symbol = symbol,
            Line = token.Line,
            Column = token.Column
        };
    }

    private DropFunctionNode ParseDropFunction()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.FUNCTION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected function name");
        return new DropFunctionNode
        {
            Type = "DROP_FUNCTION",
            FunctionName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private DropRuleNode ParseDropRule()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.RULE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected rule name");
        return new DropRuleNode
        {
            Type = "DROP_RULE",
            RuleName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private DropUserNode ParseDropUser()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.USER);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected username");
        return new DropUserNode
        {
            Type = "DROP_USER",
            Username = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private UseKbNode ParseUse()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.USE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
        return new UseKbNode
        {
            Type = "USE",
            KbName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private AstNode ParseAdd()
    {
        Consume(TokenType.ADD);
        var token = Peek();

        if (token == null)
            throw new ParserException("Expected token after ADD");

        return token.Type switch
        {
            TokenType.VARIABLE => ParseAddVariable(),
            TokenType.HIERARCHY => ParseAddHierarchy(),
            TokenType.COMPUTATION => ParseAddComputation(),
            _ => throw new ParserException($"Unexpected token after ADD: {token.Lexeme}", token.Line, token.Column)
        };
    }

    private AddVariableNode ParseAddVariable()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.VARIABLE);

        // Accept any token as variable name (including keywords)
        var nameToken = Peek();
        if (nameToken == null || nameToken.Type == TokenType.COLON)
            throw new ParserException("Expected variable name");
        Advance();
        Consume(TokenType.COLON);
        var typeToken = Peek() ?? throw new ParserException("Expected variable type");

        var node = new AddVariableNode
        {
            Type = "ADD_VARIABLE",
            VariableName = nameToken.Lexeme,
            VariableType = typeToken.Lexeme.ToUpper(),
            Line = token.Line,
            Column = token.Column
        };
        Advance();

        // Parse length for VARCHAR, CHAR, DECIMAL
        if (Check(TokenType.LPAREN))
        {
            var lengthToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected length");
            node.Length = (int?)ConvertToDouble(lengthToken.Literal);

            if (Check(TokenType.COMMA))
            {
                var scaleToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected scale");
                node.Scale = (int?)ConvertToDouble(scaleToken.Literal);
            }
            Consume(TokenType.RPAREN);
        }

        Consume(TokenType.TO);
        Consume(TokenType.CONCEPT);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        node.ConceptName = conceptToken.Lexeme;

        return node;
    }

    private AddHierarchyNode ParseAddHierarchy()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.HIERARCHY);

        var firstConceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");

        var typeToken = Peek() ?? throw new ParserException("Expected hierarchy type");
        HierarchyType hierarchyType;

        if (typeToken.Type == TokenType.IS_A)
        {
            hierarchyType = HierarchyType.IS_A;
        }
        else if (typeToken.Type == TokenType.PART_OF)
        {
            hierarchyType = HierarchyType.PART_OF;
        }
        else
        {
            throw new ParserException($"Expected IS_A or PART_OF, got {typeToken.Lexeme}", typeToken.Line, typeToken.Column);
        }
        Advance();

        var secondConceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");

        // In both IS_A and PART_OF:
        // "A IS_A B" or "A PART_OF B" means A is the child, B is the parent
        return new AddHierarchyNode
        {
            Type = "ADD_HIERARCHY",
            ChildConcept = firstConceptToken.Lexeme,
            ParentConcept = secondConceptToken.Lexeme,
            HierarchyType = hierarchyType,
            Line = token.Line,
            Column = token.Column
        };
    }

    private AddComputationNode ParseAddComputation()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.COMPUTATION);
        Consume(TokenType.TO);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        var node = new AddComputationNode
        {
            Type = "ADD_COMPUTATION",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse VARIABLES clause
        Consume(TokenType.VARIABLES);
        while (!Check(TokenType.FORMULA))
        {
            var varToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable name");
            node.InputVariables.Add(varToken.Lexeme);
            if (Check(TokenType.COMMA))
            {
                Consume(TokenType.COMMA);
            }
            else if (!Check(TokenType.FORMULA))
            {
                throw new ParserException("Expected comma or FORMULA", Peek()!.Line, Peek()!.Column);
            }
        }
        // Last variable is the result variable
        if (node.InputVariables.Count > 0)
        {
            node.ResultVariable = node.InputVariables[^1];
            node.InputVariables.RemoveAt(node.InputVariables.Count - 1);
        }

        // Parse FORMULA clause
        Consume(TokenType.FORMULA);
        var formulaToken = Consume(TokenType.STRING) ?? throw new ParserException("Expected formula string");
        node.Formula = formulaToken.Literal?.ToString() ?? "";

        // Parse COST clause
        if (Check(TokenType.COST))
        {
            var costToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected cost number");
            node.Cost = (int?)ConvertToDouble(costToken.Literal);
        }

        return node;
    }

    private AstNode ParseRemove()
    {
        Consume(TokenType.REMOVE);
        var token = Peek();

        if (token == null)
            throw new ParserException("Expected token after REMOVE");

        return token.Type switch
        {
            TokenType.HIERARCHY => ParseRemoveHierarchy(),
            TokenType.COMPUTATION => ParseRemoveComputation(),
            _ => throw new ParserException($"Unexpected token after REMOVE: {token.Lexeme}", token.Line, token.Column)
        };
    }

    private RemoveHierarchyNode ParseRemoveHierarchy()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.HIERARCHY);

        var parentToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected parent concept");

        var typeToken = Peek() ?? throw new ParserException("Expected hierarchy type");
        HierarchyType hierarchyType;

        if (typeToken.Type == TokenType.IS_A)
        {
            hierarchyType = HierarchyType.IS_A;
        }
        else if (typeToken.Type == TokenType.PART_OF)
        {
            hierarchyType = HierarchyType.PART_OF;
        }
        else
        {
            throw new ParserException($"Expected IS_A or PART_OF, got {typeToken.Lexeme}", typeToken.Line, typeToken.Column);
        }
        Advance();

        var childToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected child concept");

        return new RemoveHierarchyNode
        {
            Type = "REMOVE_HIERARCHY",
            ParentConcept = parentToken.Lexeme,
            ChildConcept = childToken.Lexeme,
            HierarchyType = hierarchyType,
            Line = token.Line,
            Column = token.Column
        };
    }

    private RemoveComputationNode ParseRemoveComputation()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.COMPUTATION);

        var varToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable name");
        Consume(TokenType.FROM);
        Consume(TokenType.CONCEPT);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");

        return new RemoveComputationNode
        {
            Type = "REMOVE_COMPUTATION",
            VariableName = varToken.Lexeme,
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private GrantNode ParseGrant()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.GRANT);

        // Accept any identifier or keyword as privilege name
        var privToken = Peek() ?? throw new ParserException("Expected privilege");
        Advance();
        Consume(TokenType.ON);
        var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
        Consume(TokenType.TO);
        var userToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected username");

        return new GrantNode
        {
            Type = "GRANT",
            Privilege = privToken.Lexeme.ToUpper(),
            KbName = kbToken.Lexeme,
            Username = userToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    private RevokeNode ParseRevoke()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.REVOKE);

        // Accept any identifier or keyword as privilege name
        var privToken = Peek() ?? throw new ParserException("Expected privilege");
        Advance();
        Consume(TokenType.ON);
        var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
        Consume(TokenType.FROM);
        var userToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected username");

        return new RevokeNode
        {
            Type = "REVOKE",
            Privilege = privToken.Lexeme.ToUpper(),
            KbName = kbToken.Lexeme,
            Username = userToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };
    }

    // ==================== DML Parsers ====================

    private SelectNode ParseSelect()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.SELECT);

        var node = new SelectNode
        {
            Type = "SELECT",
            Line = token.Line,
            Column = token.Column
        };

        // Check for * (select all) or aggregate functions
        if (Check(TokenType.STAR))
        {
            Consume(TokenType.STAR);
            // SELECT * FROM concept
        }
        else if (Check(TokenType.COUNT) || Check(TokenType.SUM) || Check(TokenType.AVG) || Check(TokenType.MIN) || Check(TokenType.MAX))
        {
            // Aggregate function: SELECT COUNT(*) FROM concept
            var agg = new AggregateClause();
            var funcToken = Advance()!;
            agg.AggregateType = funcToken.Type.ToString();

            if (Check(TokenType.LPAREN))
            {
                Consume(TokenType.LPAREN);
                if (!Check(TokenType.STAR))
                {
                    var varToken = Consume(TokenType.IDENTIFIER);
                    agg.Variable = varToken?.Lexeme;
                }
                else
                {
                    Consume(TokenType.STAR);
                }
                Consume(TokenType.RPAREN);
            }

            if (Check(TokenType.AS))
            {
                Consume(TokenType.AS);
                var aliasToken = Consume(TokenType.IDENTIFIER);
                agg.Alias = aliasToken?.Lexeme;
            }

            node.Aggregates.Add(agg);
        }
        else
        {
            // SELECT concept (shorthand for SELECT * FROM concept)
            var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
            node.ConceptName = conceptToken.Lexeme;
        }

        // FROM clause (for SELECT * FROM concept or SELECT COUNT(*) FROM concept)
        if (Check(TokenType.FROM))
        {
            Consume(TokenType.FROM);
            var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
            node.ConceptName = conceptToken.Lexeme;
        }

        // Parse optional AS alias
        if (Check(TokenType.AS))
        {
            var aliasToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected alias");
            node.Alias = aliasToken.Lexeme;
        }

        // Parse JOIN clauses
        while (Check(TokenType.JOIN))
        {
            Consume(TokenType.JOIN);
            node.Joins.Add(ParseJoinClause());
        }

        // Parse WHERE clause
        if (Check(TokenType.WHERE))
        {
            Consume(TokenType.WHERE);
            node.Conditions = ParseConditionList();
        }

        // Parse GROUP BY clause
        if (Check(TokenType.GROUP))
        {
            Consume(TokenType.GROUP);
            Consume(TokenType.BY);
            node.GroupBy = ParseGroupByList();
        }

        // Parse HAVING clause
        if (Check(TokenType.HAVING))
        {
            node.Having = ParseCondition();
        }

        // Parse ORDER BY clause
        if (Check(TokenType.ORDER))
        {
            Consume(TokenType.ORDER);
            Consume(TokenType.BY);
            node.OrderBy = ParseOrderByList();
        }

        // Parse LIMIT clause
        if (Check(TokenType.LIMIT))
        {
            Consume(TokenType.LIMIT);
            var limitToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected limit number");
            node.Limit = new LimitClause { Limit = (int)ConvertToDouble(limitToken.Literal)! };

            // Parse OFFSET clause
            if (Check(TokenType.OFFSET))
            {
                Consume(TokenType.OFFSET);
                var offsetToken = Consume(TokenType.NUMBER) ?? throw new ParserException("Expected offset number");
                node.Limit.Offset = (int)ConvertToDouble(offsetToken.Literal)!;
            }
        }

        return node;
    }

    private JoinClause ParseJoinClause()
    {
        var join = new JoinClause();

        var targetToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept or relation name");
        join.Target = targetToken.Lexeme;

        // Parse optional AS alias
        if (Check(TokenType.AS))
        {
            var aliasToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected alias");
            join.Alias = aliasToken.Lexeme;
        }

        // Parse ON clause
        if (Check(TokenType.ON))
        {
            Consume(TokenType.ON);
            join.OnCondition = ParseCondition();
        }

        return join;
    }

    private InsertNode ParseInsert()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.INSERT);
        Consume(TokenType.INTO);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        var node = new InsertNode
        {
            Type = "INSERT",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.VALUES);
        Consume(TokenType.LPAREN);

        // Parse values - support both positional and named syntax
        // Positional: VALUES (value1, value2, value3)
        // Named: VALUES (field1 = value1, field2 = value2)
        int positionIndex = 0;
        while (!Check(TokenType.RPAREN))
        {
            var firstToken = Peek();
            if (firstToken == null)
                throw new ParserException("Expected value");

            // Check if this is named syntax: IDENTIFIER = value
            if (firstToken.Type == TokenType.IDENTIFIER)
            {
                var nextToken = PeekNext();
                if (nextToken?.Type == TokenType.EQUALS)
                {
                    // Named syntax
                    var fieldToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected field name");
                    Consume(TokenType.EQUALS);
                    var valueNode = ParseValueNode();
                    node.Values[fieldToken.Lexeme] = valueNode;
                }
                else
                {
                    // Positional syntax - identifier as value
                    var valueNode = ParseValueNode();
                    node.Values[$"_{positionIndex}"] = valueNode;
                    positionIndex++;
                }
            }
            else
            {
                // Positional syntax - literal value
                var valueNode = ParseValueNode();
                node.Values[$"_{positionIndex}"] = valueNode;
                positionIndex++;
            }

            if (!Check(TokenType.RPAREN))
                Consume(TokenType.COMMA);
        }
        Consume(TokenType.RPAREN);

        return node;
    }

    private ValueNode ParseValueNode()
    {
        var token = Peek() ?? throw new ParserException("Expected value");

        var valueNode = new ValueNode();

        switch (token.Type)
        {
            case TokenType.NUMBER:
                valueNode.ValueType = "number";
                valueNode.Value = ConvertToDouble(token.Literal);
                Advance();
                break;
            case TokenType.STRING:
                valueNode.ValueType = "string";
                var stringValue = token.Literal?.ToString() ?? "";
                // Remove surrounding quotes from string literals
                if (stringValue.Length >= 2 && (stringValue[0] == '\'' || stringValue[0] == '"'))
                {
                    valueNode.Value = stringValue.Substring(1, stringValue.Length - 1);
                }
                else
                {
                    valueNode.Value = stringValue;
                }
                Advance();
                break;
            case TokenType.BOOLEAN:
                valueNode.ValueType = "boolean";
                valueNode.Value = token.Literal;
                Advance();
                break;
            case TokenType.IDENTIFIER:
                valueNode.ValueType = "identifier";
                valueNode.Value = token.Lexeme;
                Advance();
                break;
            default:
                throw new ParserException($"Unexpected value type: {token.Lexeme}", token.Line, token.Column);
        }

        return valueNode;
    }

    private UpdateNode ParseUpdate()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.UPDATE);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        var node = new UpdateNode
        {
            Type = "UPDATE",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.SET);

        // Parse SET values
        while (!Check(TokenType.WHERE) && !Check(TokenType.SEMICOLON) && !IsAtEnd())
        {
            var fieldToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected field name");
            Consume(TokenType.EQUALS);
            var expr = ParseExpression();
            node.SetValues[fieldToken.Lexeme] = expr;

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        // Parse WHERE clause
        if (Check(TokenType.WHERE))
        {
            Consume(TokenType.WHERE);
            node.Conditions = ParseConditionList();
        }

        return node;
    }

    private DeleteNode ParseDelete()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.DELETE);
        Consume(TokenType.FROM);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        var node = new DeleteNode
        {
            Type = "DELETE",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse WHERE clause
        if (Check(TokenType.WHERE))
        {
            Consume(TokenType.WHERE);
            node.Conditions = ParseConditionList();
        }

        return node;
    }

    private SolveNode ParseSolve()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.SOLVE);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
        var node = new SolveNode
        {
            Type = "SOLVE",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.FOR);
        var findToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable to find");
        node.FindVariable = findToken.Lexeme;

        Consume(TokenType.GIVEN);

        // Parse known values
        while (!Check(TokenType.USING) && !Check(TokenType.SEMICOLON) && !IsAtEnd())
        {
            var keyToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable name");
            Consume(TokenType.EQUALS);
            var valueToken = Peek() ?? throw new ParserException("Expected value");
            object value;

            switch (valueToken.Type)
            {
                case TokenType.NUMBER:
                    value = ConvertToDouble(valueToken.Literal)!;
                    break;
                case TokenType.STRING:
                    value = valueToken.Literal?.ToString() ?? "";
                    break;
                case TokenType.BOOLEAN:
                    value = valueToken.Literal ?? false;
                    break;
                case TokenType.IDENTIFIER:
                    value = valueToken.Lexeme;
                    break;
                default:
                    throw new ParserException($"Unexpected value type: {valueToken.Lexeme}", valueToken.Line, valueToken.Column);
            }
            Advance();
            node.Known[keyToken.Lexeme] = value;

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        // Parse USING clause
        if (Check(TokenType.USING))
        {
            var usingToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected rule type");
            node.RuleType = usingToken.Lexeme.ToLower();
        }

        return node;
    }

    private ShowNode ParseShow()
    {
        var token = Peek() ?? throw new ParserException("Unexpected end of input");
        Consume(TokenType.SHOW);

        var node = new ShowNode
        {
            Line = token.Line,
            Column = token.Column
        };

        // Determine show type
        if (Peek()?.Type == TokenType.KNOWLEDGE)
        {
            Consume(TokenType.BASE);
            node.ShowType = ShowType.KnowledgeBases;
            node.Type = "SHOW_KNOWLEDGE_BASES";
        }
        else if (Peek()?.Type == TokenType.CONCEPT && PeekNext()?.Type == TokenType.IDENTIFIER)
        {
            // SHOW CONCEPT <name> - show concept detail
            Consume(TokenType.CONCEPT);
            var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected concept name");
            node.ShowType = ShowType.ConceptDetail;
            node.Type = "SHOW_CONCEPT";
            node.ConceptName = conceptToken.Lexeme;

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
        }
        else if (Peek()?.Type == TokenType.CONCEPTS)
        {
            // SHOW CONCEPTS - show all concepts
            Consume(TokenType.CONCEPTS);
            node.ShowType = ShowType.Concepts;
            node.Type = "SHOW_CONCEPTS";

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
        }
        else if (Peek()?.Type == TokenType.RULES)
        {
            // SHOW RULES - show all rules
            Consume(TokenType.RULES);
            node.ShowType = ShowType.Rules;
            node.Type = "SHOW_RULES";

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }

            if (Check(TokenType.TYPE))
            {
                var ruleTypeToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected rule type");
                node.RuleType = ruleTypeToken.Lexeme.ToLower();
            }
        }
        else if (Peek()?.Type == TokenType.RELATIONS)
        {
            Consume(TokenType.RELATIONS);
            node.ShowType = ShowType.Relations;
            node.Type = "SHOW_RELATIONS";

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
        }
        else if (Peek()?.Type == TokenType.OPERATORS)
        {
            Consume(TokenType.OPERATORS);
            node.ShowType = ShowType.Operators;
            node.Type = "SHOW_OPERATORS";

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
        }
        else if (Peek()?.Type == TokenType.FUNCTIONS)
        {
            Consume(TokenType.FUNCTIONS);
            node.ShowType = ShowType.Functions;
            node.Type = "SHOW_FUNCTIONS";

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
        }
        else if (Peek()?.Type == TokenType.USERS)
        {
            Consume(TokenType.USERS);
            node.ShowType = ShowType.Users;
            node.Type = "SHOW_USERS";
        }
        else if (Peek()?.Type == TokenType.PRIVILEGES)
        {
            Consume(TokenType.PRIVILEGES);
            if (Check(TokenType.ON))
            {
                node.ShowType = ShowType.PrivilegesOnKb;
                node.Type = "SHOW_PRIVILEGES_ON";
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
            else if (Check(TokenType.OF))
            {
                node.ShowType = ShowType.PrivilegesOfUser;
                node.Type = "SHOW_PRIVILEGES_OF";
                var userToken = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected username");
                node.Username = userToken.Lexeme;
            }
            else
            {
                throw new ParserException("Expected ON or OF after PRIVILEGES");
            }
        }
        else
        {
            var errorToken = Peek();
            throw new ParserException($"Unexpected show type: {errorToken?.Lexeme}", errorToken?.Line ?? 0, errorToken?.Column ?? 0);
        }

        return node;
    }

    // ==================== Helper Parsers ====================

    private ExpressionNode ParseExpression()
    {
        return ParseLogicalOr();
    }

    private ExpressionNode ParseLogicalOr()
    {
        var left = ParseLogicalAnd();

        while (Check(TokenType.OR))
        {
            var op = Advance()!;
            var right = ParseLogicalAnd();
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = "OR",
                Right = right,
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    private ExpressionNode ParseLogicalAnd()
    {
        var left = ParseComparison();

        while (Check(TokenType.AND))
        {
            var op = Advance()!;
            var right = ParseComparison();
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = "AND",
                Right = right,
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    private ExpressionNode ParseComparison()
    {
        var left = ParseAdditive();

        while (IsComparisonOperator(Peek()?.Type))
        {
            var op = Advance()!;
            var right = ParseAdditive();
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op.Lexeme,
                Right = right,
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    private ExpressionNode ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Check(TokenType.PLUS) || Check(TokenType.MINUS))
        {
            var op = Advance()!;
            var right = ParseMultiplicative();
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op.Lexeme,
                Right = right,
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    private ExpressionNode ParseMultiplicative()
    {
        var left = ParseUnary();

        while (Check(TokenType.STAR) || Check(TokenType.SLASH) || Check(TokenType.PERCENT))
        {
            var op = Advance()!;
            var right = ParseUnary();
            left = new BinaryExpressionNode
            {
                Left = left,
                Operator = op.Lexeme,
                Right = right,
                Line = op.Line,
                Column = op.Column
            };
        }

        return left;
    }

    private ExpressionNode ParseUnary()
    {
        if (Check(TokenType.NOT) || Check(TokenType.MINUS))
        {
            var op = Advance()!;
            var right = ParseUnary();
            return new UnaryExpressionNode
            {
                Operator = op.Lexeme,
                Operand = right,
                Line = op.Line,
                Column = op.Column
            };
        }

        return ParsePrimary();
    }

    private ExpressionNode ParsePrimary()
    {
        var token = Peek();

        if (token == null)
            throw new ParserException("Unexpected end of expression");

        switch (token.Type)
        {
            case TokenType.NUMBER:
                Advance();
                return new LiteralNode
                {
                    Value = ConvertToDouble(token.Literal),
                    ValueType = "number",
                    Line = token.Line,
                    Column = token.Column
                };

            case TokenType.STRING:
                Advance();
                return new LiteralNode
                {
                    Value = token.Literal?.ToString(),
                    ValueType = "string",
                    Line = token.Line,
                    Column = token.Column
                };

            case TokenType.BOOLEAN:
                Advance();
                return new LiteralNode
                {
                    Value = token.Literal,
                    ValueType = "boolean",
                    Line = token.Line,
                    Column = token.Column
                };

            case TokenType.IDENTIFIER:
                Advance();

                // Check for function call
                if (Check(TokenType.LPAREN))
                {
                    var funcCall = new FunctionCallNode
                    {
                        FunctionName = token.Lexeme,
                        Line = token.Line,
                        Column = token.Column
                    };
                    Consume(TokenType.LPAREN);

                    while (!Check(TokenType.RPAREN))
                    {
                        funcCall.Arguments.Add(ParseExpression());
                        if (!Check(TokenType.RPAREN))
                            Consume(TokenType.COMMA);
                    }
                    Consume(TokenType.RPAREN);

                    return funcCall;
                }

                return new VariableNode
                {
                    Name = token.Lexeme,
                    Line = token.Line,
                    Column = token.Column
                };

            case TokenType.LPAREN:
                Advance();
                var expr = ParseExpression();
                Consume(TokenType.RPAREN);
                return expr;

            default:
                throw new ParserException($"Unexpected token in expression: {token.Lexeme}", token.Line, token.Column);
        }
    }

    private Condition ParseCondition()
    {
        var left = ParseExpression();

        if (left is VariableNode varNode)
        {
            var opToken = Peek() ?? throw new ParserException("Expected operator");

            if (IsComparisonOperator(opToken.Type))
            {
                Advance();
                var right = ParseExpression();
                object? value = right switch
                {
                    LiteralNode lit => lit.Value,
                    VariableNode v => v.Name,
                    _ => right.ToString()
                };

                return new Condition
                {
                    Field = varNode.Name,
                    Operator = opToken.Lexeme,
                    Value = value
                };
            }
        }
        else if (left is BinaryExpressionNode binary)
        {
            // Handle expression-based conditions
            return new Condition
            {
                Field = binary.Left?.ToString() ?? "",
                Operator = binary.Operator,
                Value = binary.Right?.ToString()
            };
        }

        throw new ParserException("Invalid condition", Peek()?.Line ?? 0, Peek()?.Column ?? 0);
    }

    private List<Condition> ParseConditionList()
    {
        var conditions = new List<Condition>();

        var cond = ParseCondition();
        conditions.Add(cond);

        while (Check(TokenType.AND) || Check(TokenType.OR))
        {
            var logicalOp = Advance()!;
            cond.LogicalOperator = logicalOp.Lexeme.ToUpper();
            cond = ParseCondition();
            conditions.Add(cond);
        }

        return conditions;
    }

    private List<string> ParseIdentifierList()
    {
        var list = new List<string>();

        while (!IsAtEnd() && !IsClauseKeyword(Peek()?.Type))
        {
            var token = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected identifier");
            list.Add(token.Lexeme);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        return list;
    }

    private List<string> ParseConstraintList()
    {
        var list = new List<string>();

        while (!IsAtEnd() && !IsClauseKeyword(Peek()?.Type))
        {
            var constraint = ParseExpressionString();
            list.Add(constraint);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        return list;
    }

    private string ParseExpressionString()
    {
        var sb = new StringBuilder();
        var parenCount = 0;

        while (!IsAtEnd())
        {
            var token = Peek();
            if (token == null) break;

            // Stop at comma or clause keyword (unless inside parentheses)
            if (parenCount == 0 && (token.Type == TokenType.COMMA || IsClauseKeyword(token.Type)))
                break;

            sb.Append(token.Lexeme);
            Advance();

            if (token.Type == TokenType.LPAREN) parenCount++;
            if (token.Type == TokenType.RPAREN) parenCount--;
        }

        return sb.ToString().Trim();
    }

    private List<SameVariableGroup> ParseSameVariablesList()
    {
        var list = new List<SameVariableGroup>();

        while (!IsAtEnd() && !IsClauseKeyword(Peek()?.Type))
        {
            var var1Token = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable name");
            Consume(TokenType.EQUALS);
            var var2Token = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable name");

            list.Add(new SameVariableGroup
            {
                Var1 = var1Token.Lexeme,
                Var2 = var2Token.Lexeme
            });

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        return list;
    }

    private List<string> ParseExpressionList()
    {
        var list = new List<string>();

        while (!IsAtEnd() && !IsClauseKeyword(Peek()?.Type))
        {
            var expr = ParseExpressionString();
            list.Add(expr);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        return list;
    }

    private List<string> ParseGroupByList()
    {
        var list = new List<string>();

        while (!IsAtEnd() && !IsClauseKeyword(Peek()?.Type))
        {
            var token = Consume(TokenType.IDENTIFIER) ?? throw new ParserException("Expected variable name");
            list.Add(token.Lexeme);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        return list;
    }

    private List<OrderByItem> ParseOrderByList()
    {
        var list = new List<OrderByItem>();

        while (!IsAtEnd())
        {
            var nextTokenType = Peek()?.Type;
            if (nextTokenType == null || IsClauseKeyword(nextTokenType))
                break;

            // Accept any token as variable name (including keywords like DESC)
            var token = Peek();
            if (token == null || token.Type == TokenType.COMMA)
                throw new ParserException("Expected variable name");
            var varToken = Advance();
            var item = new OrderByItem { Variable = varToken.Lexeme };

            if (Check(TokenType.ASC))
            {
                item.Direction = "ASC";
                Advance();
            }
            else if (Check(TokenType.DESC))
            {
                item.Direction = "DESC";
                Advance();
            }
            // If neither ASC nor DESC, assume ASC
            else
            {
                item.Direction = "ASC";
            }

            list.Add(item);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        return list;
    }

    // ==================== Token Helpers ====================

    private Token? Peek()
    {
        if (IsAtEnd()) return null;
        return _tokens[_current];
    }

    private Token? PeekNext()
    {
        if (_current + 1 >= _tokens.Count) return null;
        return _tokens[_current + 1];
    }

    private Token? Advance()
    {
        if (!IsAtEnd()) _current++;
        return _current > 0 ? _tokens[_current - 1] : null;
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek()?.Type == type;
    }

    private Token? Consume(TokenType type)
    {
        if (Check(type)) return Advance();
        return null;
    }

    private bool IsAtEnd()
    {
        return _current >= _tokens.Count || _tokens[_current].Type == TokenType.EOF;
    }

    private static bool IsComparisonOperator(TokenType? type)
    {
        return type == TokenType.EQUALS ||
               type == TokenType.NOT_EQUALS ||
               type == TokenType.GREATER ||
               type == TokenType.LESS ||
               type == TokenType.GREATER_EQUAL ||
               type == TokenType.LESS_EQUAL;
    }

    private static bool IsOperatorToken(TokenType type)
    {
        return type == TokenType.PLUS ||
               type == TokenType.MINUS ||
               type == TokenType.STAR ||
               type == TokenType.SLASH ||
               type == TokenType.CARET ||
               type == TokenType.PERCENT;
    }

    private static bool IsKeywordToken(TokenType type)
    {
        // Check if the token type is a keyword (not a literal, identifier, operator, or punctuation)
        return type != TokenType.IDENTIFIER &&
               type != TokenType.NUMBER &&
               type != TokenType.STRING &&
               type != TokenType.BOOLEAN &&
               !IsOperatorToken(type) &&
               type != TokenType.LPAREN &&
               type != TokenType.RPAREN &&
               type != TokenType.LBRACKET &&
               type != TokenType.RBRACKET &&
               type != TokenType.LBRACE &&
               type != TokenType.RBRACE &&
               type != TokenType.COMMA &&
               type != TokenType.SEMICOLON &&
               type != TokenType.COLON &&
               type != TokenType.DOT &&
               type != TokenType.EOF;
    }

    private static bool IsClauseKeyword(TokenType? type)
    {
        return type == TokenType.VARIABLES ||
               type == TokenType.ALIASES ||
               type == TokenType.BASE_OBJECTS ||
               type == TokenType.CONSTRAINTS ||
               type == TokenType.SAME_VARIABLES ||
               type == TokenType.RETURNS ||
               type == TokenType.BODY ||
               type == TokenType.PROPERTIES ||
               type == TokenType.FORMULA ||
               type == TokenType.COST ||
               type == TokenType.TYPE ||
               type == TokenType.SCOPE ||
               type == TokenType.IF ||
               type == TokenType.THEN ||
               type == TokenType.WHERE ||
               type == TokenType.ORDER ||
               type == TokenType.GROUP ||
               type == TokenType.HAVING ||
               type == TokenType.LIMIT ||
               type == TokenType.JOIN ||
               type == TokenType.ON ||
               type == TokenType.FROM ||
               type == TokenType.FOR ||
               type == TokenType.GIVEN ||
               type == TokenType.USING ||
               type == TokenType.PASSWORD ||
               type == TokenType.ROLE ||
               type == TokenType.SYSTEM_ADMIN ||
               type == TokenType.SEMICOLON;
    }

    private static double? ConvertToDouble(object? value)
    {
        if (value == null) return null;
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is float f) return f;
        if (value is decimal dec) return (double)dec;
        if (double.TryParse(value.ToString(), out var result)) return result;
        return null;
    }
}
