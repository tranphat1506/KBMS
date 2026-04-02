using System;
using System.Collections.Generic;
using System.Text;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;
using KBMS.Parser.Ast.Expressions;
using KBMS.Models;

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

        var stmt = ParseStatement();

        // Enforce semicolon for single Parse() call as well, but allow EOF
        if (Check(TokenType.SEMICOLON))
        {
            Consume(TokenType.SEMICOLON);
        }
        else if (!IsAtEnd())
        {
            var next = Peek();
            throw Error("Semicolon ';' expected at end of statement", next);
        }

        return stmt;
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

            // Enforce semicolon after each statement, but allow EOF for the last one
            if (Check(TokenType.SEMICOLON))
            {
                Advance();
            }
            else if (!IsAtEnd())
            {
                var next = Peek();
                throw Error("Semicolon ';' expected", next);
            }
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
            TokenType.INSERT => ParseInsertOrBulk(),
            TokenType.UPDATE => ParseUpdate(),
            TokenType.DELETE => ParseDelete(),
            TokenType.SHOW => ParseShow(),
            TokenType.ALTER => ParseAlter(),
            TokenType.DESCRIBE => ParseDescribe(),
            TokenType.EXPLAIN => ParseExplain(),
            TokenType.MAINTENANCE => ParseMaintenance(),
            TokenType.EXPORT => ParseExport(),
            TokenType.IMPORT => ParseImport(),
            // TCL
            TokenType.BEGIN => ParseBeginTransaction(),
            TokenType.COMMIT => ParseCommit(),
            TokenType.ROLLBACK => ParseRollback(),
            _ => throw Error($"Unexpected token: {token.Lexeme}", token)
        };
    }

    // ==================== DDL Parsers ====================

    private AstNode ParseCreate()
    {
        Consume(TokenType.CREATE);
        var token = Peek();

        if (token == null)
            throw Error("Expected token after CREATE");

        return token.Type switch
        {
            TokenType.KNOWLEDGE => ParseCreateKnowledgeBase(),
            TokenType.CONCEPT => ParseCreateConcept(),
            TokenType.RELATION => ParseCreateRelation(),
            TokenType.OPERATOR => ParseCreateOperator(),
            TokenType.FUNCTION => ParseCreateFunction(),
            TokenType.RULE => ParseCreateRule(),
            TokenType.USER => ParseCreateUser(),
            TokenType.INDEX => ParseCreateIndex(),
            TokenType.TRIGGER => ParseCreateTrigger(),
            TokenType.HIERARCHY => ParseCreateHierarchy(),
            _ => throw Error($"Unexpected token after CREATE: {token.Lexeme}", token)
        };
    }

    // CREATE HIERARCHY is an alias for ADD HIERARCHY
    private AddHierarchyNode ParseCreateHierarchy()
    {
        return ParseAddHierarchy();
    }

    private AstNode ParseAlter()
    {
        Consume(TokenType.ALTER);
        var token = Peek() ?? throw Error("Expected token after ALTER");

        return token.Type switch
        {
            TokenType.CONCEPT => ParseAlterConcept(),
            TokenType.KNOWLEDGE => ParseAlterKnowledgeBase(),
            TokenType.USER => ParseAlterUser(),
            _ => throw Error($"Unexpected token after ALTER: {token.Lexeme}", token)
        };
    }

    private CreateKbNode ParseCreateKnowledgeBase()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.KNOWLEDGE);
        Consume(TokenType.BASE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
            var descToken = Consume(TokenType.STRING) ?? throw Error("Expected description string");
            node.Description = descToken.Literal?.ToString();
        }

        return node;
    }

    private CreateConceptNode ParseCreateConcept()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.CONCEPT);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        var node = new CreateConceptNode
        {
            Type = "CREATE_CONCEPT",
            ConceptName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        if (!Check(TokenType.LPAREN))
            throw Error("Expected '(' after concept name");
        Consume(TokenType.LPAREN);

        bool hasVariables = false;

        while (!IsAtEnd() && !Check(TokenType.RPAREN))
        {
            var nextType = Peek()?.Type;
            if (nextType == TokenType.VARIABLES)
            {
                Consume(TokenType.VARIABLES);
                if (!Check(TokenType.LPAREN))
                    throw Error("Expected '(' after VARIABLES");
                Consume(TokenType.LPAREN);
                while (!IsAtEnd() && !Check(TokenType.RPAREN))
                {
                    var varNode = ParseVariableDefinition();
                    node.Variables.Add(varNode);
                    if (Check(TokenType.COMMA))
                        Consume(TokenType.COMMA);
                }
                Consume(TokenType.RPAREN);
                hasVariables = true;
            }
            else if (nextType == TokenType.ALIASES)
            {
                Consume(TokenType.ALIASES);
                node.Aliases = ParseIdentifierList();
            }
            else if (nextType == TokenType.BASE_OBJECTS)
            {
                Consume(TokenType.BASE_OBJECTS);
                node.BaseObjects = ParseIdentifierList();
            }
            else if (nextType == TokenType.CONSTRAINTS)
            {
                Consume(TokenType.CONSTRAINTS);
                node.Constraints = ParseConstraintList();
            }
            else if (nextType == TokenType.SAME_VARIABLES)
            {
                Consume(TokenType.SAME_VARIABLES);
                node.SameVariables = ParseSameVariablesList();
            }
            else if (nextType == TokenType.CONSTRUCT_RELATIONS)
            {
                Consume(TokenType.CONSTRUCT_RELATIONS);
                node.ConstructRelations = ParseConstructRelationList();
            }
            else if (nextType == TokenType.PROPERTIES)
            {
                Consume(TokenType.PROPERTIES);
                node.Properties = ParsePropertyList();
            }
            else if (nextType == TokenType.RULES)
            {
                Consume(TokenType.RULES);
                node.ConceptRules = ParseConceptRuleList();
            }
            else if (nextType == TokenType.EQUATIONS)
            {
                Consume(TokenType.EQUATIONS);
                node.Equations = ParseEquationList();
            }
            else
            {
                // Fallback for positional variables (e.g. CREATE CONCEPT Student(name STRING, ...))
                var currentToken = Peek();
                if (currentToken != null && currentToken.Type == TokenType.IDENTIFIER)
                {
                    var varNode = ParseVariableDefinition();
                    node.Variables.Add(varNode);
                    hasVariables = true;
                    if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                    continue;
                }

                // Consume comma or other noise between blocks
                if (Check(TokenType.COMMA))
                    Consume(TokenType.COMMA);
                else
                    break;
            }
            
            // Optional comma after any block
            if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
        }

        bool hasAnyBlock = hasVariables || node.Constraints.Count > 0 || node.ConceptRules.Count > 0 
            || node.Equations.Count > 0 || node.Aliases.Count > 0 || node.BaseObjects.Count > 0 
            || node.Properties.Count > 0 || node.ConstructRelations.Count > 0 || node.SameVariables.Count > 0;

        if (!hasAnyBlock)
            throw Error("Concept must have at least one block (VARIABLES, CONSTRAINTS, RULES, etc.)");

        Consume(TokenType.RPAREN);

        return node;
    }

    private VariableDefinition ParseVariableDefinition()
    {
        // Accept any token as variable name (including keywords)
        var token = Peek();
        if (token == null || token.Type == TokenType.COLON || token.Type == TokenType.COMMA || token.Type == TokenType.RPAREN)
            throw Error("Expected variable name");
        var nameToken = Advance();
        Consume(TokenType.COLON);
        var typeToken = Peek() ?? throw Error("Expected variable type");

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
                Advance();
                break;
            case TokenType.IDENTIFIER:
                // Keep original case for concept-type references (e.g., Point, Triangle)
                varDef.Type = typeToken.Lexeme;
                Advance();
                break;
            default:
                throw Error($"Expected type, got {typeToken.Lexeme}", typeToken);
        }

        // Parse length for VARCHAR, CHAR, DECIMAL
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            var lengthToken = Consume(TokenType.NUMBER) ?? throw Error("Expected length");
            varDef.Length = (int?)ConvertToDouble(lengthToken.Literal);

            // Parse scale for DECIMAL
            if (Check(TokenType.COMMA))
            {
                Consume(TokenType.COMMA);
                var scaleToken = Consume(TokenType.NUMBER) ?? throw Error("Expected scale");
                varDef.Scale = (int?)ConvertToDouble(scaleToken.Literal);
            }
            Consume(TokenType.RPAREN);
        }

        return varDef;
    }

    private CreateRelationNode ParseCreateRelation()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.RELATION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected relation name");
        var node = new CreateRelationNode
        {
            Type = "CREATE_RELATION",
            RelationName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse optional clauses in any order
        while (!IsAtEnd())
        {
            var nextType = Peek()?.Type;
            if (nextType == TokenType.FROM || nextType == TokenType.DOMAIN)
            {
                Consume(nextType!.Value);
                var domainToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected domain concept");
                node.DomainConcept = domainToken.Lexeme;
            }
            else if (nextType == TokenType.TO || nextType == TokenType.RANGE)
            {
                Consume(nextType!.Value);
                var rangeToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected range concept");
                node.RangeConcept = rangeToken.Lexeme;
            }
            else if (nextType == TokenType.PARAMS)
            {
                Consume(TokenType.PARAMS);
                if (Check(TokenType.LPAREN))
                {
                    Consume(TokenType.LPAREN);
                    while (!Check(TokenType.RPAREN) && !IsAtEnd())
                    {
                        var paramToken = ConsumeIdentifier() ?? throw Error("Expected param name");
                        node.ParamNames.Add(paramToken.Lexeme);
                        if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                    }
                    Consume(TokenType.RPAREN);
                }
                else
                {
                    node.ParamNames = ParseIdentifierList();
                }
            }
            else if (nextType == TokenType.PROPERTIES)
            {
                Consume(TokenType.PROPERTIES);
                node.Properties = ParseIdentifierList();
            }
            else if (nextType == TokenType.EQUATIONS)
            {
                Consume(TokenType.EQUATIONS);
                node.Equations = ParseEquationList();
            }
            else if (nextType == TokenType.RULES)
            {
                Consume(TokenType.RULES);
                node.ConceptRules = ParseConceptRuleList();
            }
            else if (nextType == TokenType.LPAREN)
            {
                var concepts = ParseIdentifierList();
                if (concepts.Count >= 1) node.DomainConcept = concepts[0];
                if (concepts.Count >= 2) node.RangeConcept = concepts[1];
            }
            else
            {
                break;
            }
        }

        return node;
    }

    private CreateOperatorNode ParseCreateOperator()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.OPERATOR);

        // Operator symbol can be identifier, operator token, or any keyword
        var symbolToken = Peek() ?? throw Error("Expected operator symbol");
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
            throw Error($"Expected operator symbol, got {symbolToken.Lexeme}", symbolToken);
        }

        var node = new CreateOperatorNode
        {
            Type = "CREATE_OPERATOR",
            Symbol = symbol,
            Line = token.Line,
            Column = token.Column
        };

        // Parse PARAMS clause
        if (Check(TokenType.PARAMS) || Check(TokenType.LPAREN))
        {
            if (Check(TokenType.PARAMS)) Consume(TokenType.PARAMS);
            Consume(TokenType.LPAREN);
            while (!Check(TokenType.RPAREN))
            {
                var typeToken = Peek() ?? throw Error("Expected parameter type");
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
            var returnToken = Peek() ?? throw Error("Expected return type");
            node.ReturnType = returnToken.Lexeme.ToUpper();
            Advance();
        }

        // Parse BODY clause
        if (Check(TokenType.BODY))
        {
            Consume(TokenType.BODY);
            var bodyToken = Consume(TokenType.STRING) ?? throw Error("Expected operator body string");
            node.Body = bodyToken.Literal?.ToString() ?? "";
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.FUNCTION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected function name");
        var node = new CreateFunctionNode
        {
            Type = "CREATE_FUNCTION",
            FunctionName = nameToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        // Parse PARAMS clause - handle both "PARAMS (x, y)" and "(x, y)"
        if (Check(TokenType.PARAMS) || Check(TokenType.LPAREN))
        {
            if (Check(TokenType.PARAMS)) Consume(TokenType.PARAMS);
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
            var returnToken = Peek() ?? throw Error("Expected return type");
            node.ReturnType = returnToken.Lexeme.ToUpper();
            Advance();
        }

        // Parse BODY clause
        if (Check(TokenType.BODY))
        {
            Consume(TokenType.BODY);
            var bodyToken = Consume(TokenType.STRING) ?? throw Error("Expected body string");
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
        var firstToken = Peek() ?? throw Error("Expected parameter name or type");
        Advance();

        // Check if next is a token that indicates end of this parameter (closing paren or comma)
        if (Check(TokenType.RPAREN) || Check(TokenType.COMMA))
        {
            // Only one identifier was found, treat it as the name
            return new ParamDefinition { Name = firstToken.Lexeme, Type = "DECIMAL" };
        }

        // If next is an identifier, then first was the type and second is the name
        if (Check(TokenType.IDENTIFIER))
        {
            var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected parameter name");
            var paramDef = new ParamDefinition
            {
                Type = firstToken.Lexeme.ToUpper(),
                Name = nameToken.Lexeme
            };

            // Parse optional length (...) for types like VARCHAR(20)
            if (Check(TokenType.LPAREN))
            {
                Consume(TokenType.LPAREN);
                var lengthToken = Consume(TokenType.NUMBER) ?? throw Error("Expected length");
                paramDef.Length = (int?)ConvertToDouble(lengthToken.Literal);
                if (Check(TokenType.COMMA))
                {
                    Consume(TokenType.COMMA);
                    var scaleToken = Consume(TokenType.NUMBER) ?? throw Error("Expected scale");
                    paramDef.Scale = (int?)ConvertToDouble(scaleToken.Literal);
                }
                Consume(TokenType.RPAREN);
            }
            return paramDef;
        }

        // Default case: treat as name with default type
        return new ParamDefinition { Name = firstToken.Lexeme, Type = "DECIMAL" };
    }

    private CreateRuleNode ParseCreateRule()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.RULE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected rule name");
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
            Consume(TokenType.TYPE);
            var typeToken = ConsumeIdentifier() ?? throw Error("Expected rule type");
            node.RuleType = Enum.Parse<RuleType>(typeToken.Lexeme, true);
        }

        // Parse SCOPE clause
        if (Check(TokenType.SCOPE))
        {
            Consume(TokenType.SCOPE);
            var scopeToken = ConsumeIdentifier() ?? throw Error("Expected scope concept");
            node.ConceptName = scopeToken.Lexeme;
        }

        // Parse IF clause (hypothesis)
        if (Check(TokenType.IF))
        {
            Consume(TokenType.IF);
            node.Hypothesis = ParseExpressionASTList();
        }

        // Parse THEN clause (conclusions)
        if (Check(TokenType.THEN))
        {
            Consume(TokenType.THEN);
            node.Conclusions = ParseExpressionASTList();
        }

        // Parse COST clause
        if (Check(TokenType.COST))
        {
            var costToken = Consume(TokenType.NUMBER) ?? throw Error("Expected cost number");
            node.Cost = (int?)ConvertToDouble(costToken.Literal);
        }

        return node;
    }

    private CreateUserNode ParseCreateUser()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.USER);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected username");
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
                throw Error("Expected password");

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
                throw Error("Expected password string or identifier");
            }
        }

        // Parse ROLE clause
        if (Check(TokenType.ROLE))
        {
            Consume(TokenType.ROLE);
            // Accept any identifier or keyword as role name
            var roleToken = Peek() ?? throw Error("Expected role");
            Advance();
            node.Role = roleToken.Lexeme.ToUpper();
        }

        // Parse SYSTEM_ADMIN clause
        if (Check(TokenType.SYSTEM_ADMIN))
        {
            var adminToken = Peek() ?? throw Error("Expected boolean");
            node.SystemAdmin = adminToken.Type == TokenType.BOOLEAN && (bool)(adminToken.Literal ?? false);
            Advance();
        }

        return node;
    }

    private AlterConceptNode ParseAlterConcept()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.CONCEPT);

        string conceptName;
        if (Check(TokenType.STAR))
        {
            conceptName = Advance().Lexeme;
        }
        else
        {
            var idToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name or '*' after ALTER CONCEPT");
            conceptName = idToken.Lexeme;
        }

        var node = new AlterConceptNode
        {
            ConceptName = conceptName,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.LPAREN);

        while (!Check(TokenType.RPAREN) && !IsAtEnd())
        {
            var actionTypeToken = Peek() ?? throw Error("Expected ALTER action (ADD, DROP, RENAME)");
            switch (actionTypeToken.Type)
            {
                case TokenType.ADD:
                    Consume(TokenType.ADD);
                    Consume(TokenType.LPAREN);
                    while (!Check(TokenType.RPAREN) && !IsAtEnd())
                    {
                        var target = Peek() ?? throw Error("Expected ADD target (VARIABLE, CONSTRAINT, RULE)");
                        if (target.Type == TokenType.VARIABLE || target.Type == TokenType.VARIABLES)
                        {
                            Consume(target.Type);
                            Consume(TokenType.LPAREN);
                            while (!Check(TokenType.RPAREN))
                            {
                                var vDef = ParseVariableDefinition();
                                node.Actions.Add(new KBMS.Models.AlterAction { 
                                    ActionType = KBMS.Models.AlterActionType.AddVariable, 
                                    Variable = new KBMS.Models.Variable { Name = vDef.Name, Type = vDef.Type, Length = vDef.Length, Scale = vDef.Scale } 
                                });
                                if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                            }
                            Consume(TokenType.RPAREN);
                        }
                        else if (target.Type == TokenType.CONSTRAINTS)
                        {
                            Consume(target.Type);
                            Consume(TokenType.LPAREN);
                            var constraints = ParseConstraintList();
                            foreach (var c in constraints)
                                node.Actions.Add(new KBMS.Models.AlterAction { 
                                    ActionType = KBMS.Models.AlterActionType.AddConstraint, 
                                    Constraint = new KBMS.Models.Constraint { Name = c.Name, Expression = c.Expression, Line = c.Line, Column = c.Column } 
                                });
                            Consume(TokenType.RPAREN);
                        }
                        else if (target.Type == TokenType.RULE || target.Type == TokenType.RULES)
                        {
                            Consume(target.Type);
                            Consume(TokenType.LPAREN);
                            var rules = ParseConceptRuleList();
                            foreach (var r in rules)
                                node.Actions.Add(new KBMS.Models.AlterAction { 
                                    ActionType = KBMS.Models.AlterActionType.AddRule, 
                                    Rule = new KBMS.Models.ConceptRule { 
                                        Id = Guid.NewGuid(),
                                        Kind = r.Kind,
                                        Variables = r.Variables.Select(v => new KBMS.Models.Variable { Name = v.Name, Type = v.Type }).ToList(),
                                        Hypothesis = r.Hypothesis.ToList(),
                                        Conclusion = r.Conclusion.ToList()
                                    } 
                                });
                            Consume(TokenType.RPAREN);
                        }
                        else if (target.Type == TokenType.EQUATION || target.Type == TokenType.EQUATIONS)
                        {
                            Consume(target.Type);
                            // Accept string or identifier expression
                            var exprToken = Advance() ?? throw Error("Expected equation expression");
                            var exprStr = exprToken.Type == TokenType.STRING
                                ? exprToken.Literal?.ToString() ?? exprToken.Lexeme
                                : exprToken.Lexeme;
                            node.Actions.Add(new KBMS.Models.AlterAction {
                                ActionType = KBMS.Models.AlterActionType.AddEquation,
                                Equation = new KBMS.Models.Equation { Id = Guid.NewGuid(), Expression = exprStr }
                            });
                        }
                        else if (target.Type == TokenType.PROPERTIES)
                        {
                            Consume(target.Type);
                            var keyToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected property key");
                            Consume(TokenType.COLON);
                            var valToken = Advance() ?? throw Error("Expected property value");
                            var val = valToken.Type == TokenType.STRING
                                ? valToken.Literal?.ToString() ?? valToken.Lexeme
                                : valToken.Lexeme;
                            node.Actions.Add(new KBMS.Models.AlterAction {
                                ActionType = KBMS.Models.AlterActionType.AddProperty,
                                Property = new KBMS.Models.Property { Key = keyToken.Lexeme, Value = val }
                            });
                        }
                        else if (target.Type == TokenType.CONSTRUCT_RELATIONS)
                        {
                            Consume(target.Type);
                            var relNameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected relation name");
                            var args = new List<string>();
                            if (Check(TokenType.LPAREN))
                            {
                                Consume(TokenType.LPAREN);
                                while (!Check(TokenType.RPAREN) && !IsAtEnd())
                                {
                                    var arg = Advance()!;
                                    args.Add(arg.Lexeme);
                                    if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                                }
                                Consume(TokenType.RPAREN);
                            }
                            node.Actions.Add(new KBMS.Models.AlterAction {
                                ActionType = KBMS.Models.AlterActionType.AddConstructRelation,
                                ConstructRelation = new KBMS.Models.ConstructRelation { RelationName = relNameToken.Lexeme, Arguments = args }
                            });
                        }
                        
                        if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                        else break;
                    }
                    Consume(TokenType.RPAREN);
                    break;

                case TokenType.DROP:
                case TokenType.REMOVE:
                    Consume(actionTypeToken.Type);
                    Consume(TokenType.LPAREN);
                    while (!Check(TokenType.RPAREN) && !IsAtEnd())
                    {
                        var dropTarget = Peek() ?? throw Error("Expected drop target (VARIABLE, etc.)");
                        Consume(dropTarget.Type);
                        var name = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected name to drop");
                        
                        KBMS.Models.AlterActionType type = dropTarget.Type switch {
                            TokenType.VARIABLE => KBMS.Models.AlterActionType.DropVariable,
                            TokenType.VARIABLES => KBMS.Models.AlterActionType.DropVariable,
                            TokenType.CONSTRAINTS => KBMS.Models.AlterActionType.DropConstraint,
                            TokenType.RULE => KBMS.Models.AlterActionType.DropRule,
                            TokenType.RULES => KBMS.Models.AlterActionType.DropRule,
                            TokenType.EQUATION => KBMS.Models.AlterActionType.DropEquation,
                            TokenType.EQUATIONS => KBMS.Models.AlterActionType.DropEquation,
                            TokenType.PROPERTIES => KBMS.Models.AlterActionType.DropProperty,
                            TokenType.CONSTRUCT_RELATIONS => KBMS.Models.AlterActionType.DropConstructRelation,
                            _ => throw Error("Invalid drop target")
                        };

                        node.Actions.Add(new KBMS.Models.AlterAction { ActionType = type, TargetName = name.Lexeme });
                        if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                    }
                    Consume(TokenType.RPAREN);
                    break;

                case TokenType.RENAME:
                    Consume(TokenType.RENAME);
                    Consume(TokenType.LPAREN);
                    Consume(TokenType.VARIABLE);
                    var oldVar = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected old variable name");
                    Consume(TokenType.TO);
                    var newVar = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected new variable name");
                    node.Actions.Add(new KBMS.Models.AlterAction { 
                        ActionType = KBMS.Models.AlterActionType.RenameVariable, 
                        OldName = oldVar.Lexeme, 
                        NewName = newVar.Lexeme 
                    });
                    Consume(TokenType.RPAREN);
                    break;

                default:
                    throw Error("Unexpected ALTER action: {actionTypeToken.Lexeme}");
            }

            if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
        }

        Consume(TokenType.RPAREN);
        return node;
    }

    private AstNode ParseAlterKnowledgeBase()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.KNOWLEDGE);
        Consume(TokenType.BASE);

        var node = new AlterKbNode
        {
            KbName = (Check(TokenType.STAR) ? Advance().Lexeme : (Consume(TokenType.IDENTIFIER)?.Lexeme ?? throw Error("Expected KB name or '*'"))),
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.LPAREN);
        Consume(TokenType.SET);
        Consume(TokenType.LPAREN);
        Consume(TokenType.DESCRIPTION);
        Consume(TokenType.COLON);
        node.NewDescription = Consume(TokenType.STRING)?.Literal?.ToString() ?? throw Error("Expected description string");
        Consume(TokenType.RPAREN);
        Consume(TokenType.RPAREN);

        return node;
    }

    private AstNode ParseAlterUser()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.USER);
        var uname = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected username");

        var node = new AlterUserNode
        {
            Username = uname.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.LPAREN);
        Consume(TokenType.SET);
        Consume(TokenType.LPAREN);

        while (!Check(TokenType.RPAREN) && !IsAtEnd())
        {
            var fieldToken = Advance();
            if (fieldToken.Type != TokenType.IDENTIFIER && fieldToken.Type != TokenType.PASSWORD)
                throw Error("Expected SET field (PASSWORD or ADMIN)");

            Consume(TokenType.COLON);
            if (fieldToken.Lexeme.Equals("PASSWORD", StringComparison.OrdinalIgnoreCase))
            {
                node.NewPassword = Consume(TokenType.STRING)?.Literal?.ToString() ?? Consume(TokenType.IDENTIFIER)?.Lexeme;
            }
            else if (fieldToken.Lexeme.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                node.NewAdminStatus = bool.Parse(Consume(TokenType.BOOLEAN)?.Lexeme ?? "false");
            }

            if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
        }

        Consume(TokenType.RPAREN);
        Consume(TokenType.RPAREN);
        return node;
    }

    private CreateIndexNode ParseCreateIndex()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.INDEX);
        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected index name");
        Consume(TokenType.ON);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        Consume(TokenType.LPAREN);
        var vs = ParseIdentifierList();
        Consume(TokenType.RPAREN);
        return new CreateIndexNode { 
            IndexName = nameToken.Lexeme,
            ConceptName = conceptToken.Lexeme,
            Variables = vs,
            Line = token.Line,
            Column = token.Column
        }; 
    }
    
    private KBMS.Parser.Ast.Kdl.CreateTriggerNode ParseCreateTrigger()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.TRIGGER);

        var nameToken = Advance() ?? throw Error("Expected trigger name");

        if (Consume(TokenType.LPAREN) == null) throw Error("Expected '(' after TRIGGER name");

        // ON block: ON ( INSERT|UPDATE|DELETE OF ConceptName )
        if (Consume(TokenType.ON) == null) throw Error("Expected 'ON' in TRIGGER definition");
        if (Consume(TokenType.LPAREN) == null) throw Error("Expected '(' before TRIGGER dynamic event");
        var eventToken = Advance() ?? throw Error("Expected event type (INSERT, UPDATE, DELETE)");
        var triggerEvent = eventToken.Type switch
        {
            TokenType.INSERT => KBMS.Parser.Ast.Kdl.TriggerEvent.Insert,
            TokenType.UPDATE => KBMS.Parser.Ast.Kdl.TriggerEvent.Update,
            TokenType.DELETE => KBMS.Parser.Ast.Kdl.TriggerEvent.Delete,
            _ => throw Error($"Unknown trigger event: {eventToken.Lexeme}", eventToken)
        };
        if (Consume(TokenType.OF) == null) throw Error("Expected 'OF' after trigger event");
        // Concept name might be an identifier; use Advance() so keywords like a concept named after a keyword aren't rejected
        string conceptName = Check(TokenType.STAR) 
            ? Advance().Lexeme 
            : (Advance()?.Lexeme ?? throw Error("Expected concept name or '*'"));
        if (Consume(TokenType.RPAREN) == null) throw Error("Expected ')' after trigger event concept");

        if (Consume(TokenType.COMMA) == null) throw Error("Expected ',' between ON and DO blocks");

        // DO block: DO ( <statement> )
        // We parse the action as a statement; the statement parser will stop before the closing ')'
        if (Consume(TokenType.DO) == null) throw Error("Expected 'DO' in TRIGGER definition");
        if (Consume(TokenType.LPAREN) == null) throw Error("Expected '(' before TRIGGER action");
        
        var action = ParseStatement() ?? throw Error("Expected action statement in TRIGGER DO block");
        
        // Consume trailing semicolon from inner statement if present
        if (Check(TokenType.SEMICOLON)) Consume(TokenType.SEMICOLON);
        
        if (Consume(TokenType.RPAREN) == null) throw Error("Expected ')' after TRIGGER action");
        if (Consume(TokenType.RPAREN) == null) throw Error("Expected ')' at end of TRIGGER definition");

        return new KBMS.Parser.Ast.Kdl.CreateTriggerNode 
        { 
            TriggerName = nameToken.Lexeme,
            Event = triggerEvent,
            TargetConcept = conceptName,
            Action = action,
            Line = token.Line,
            Column = token.Column
        };
    }

    private AstNode ParseExplain()
    {
        Consume(TokenType.EXPLAIN);
        Consume(TokenType.LPAREN);
        var inner = ParseStatement() ?? throw Error("Expected statement to explain");
        Consume(TokenType.RPAREN);
        return new ExplainNode { Query = inner };
    }
    
    private MaintenanceNode ParseMaintenance() 
    { 
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.MAINTENANCE); 
        
        var node = new MaintenanceNode() { Line = token.Line, Column = token.Column };
        Consume(TokenType.LPAREN);

        while (!Check(TokenType.RPAREN) && !IsAtEnd())
        {
            var actionToken = Advance() ?? throw Error("Expected maintenance action (VACUUM, REINDEX, CHECK)");
            
            if (actionToken.Type == TokenType.VACUUM)
            {
                node.Actions.Add(new MaintenanceAction { ActionType = MaintenanceActionType.Vacuum });
            }
            else if (actionToken.Type == TokenType.REINDEX)
            {
                Consume(TokenType.LPAREN);
                string target = Check(TokenType.STAR) ? Advance().Lexeme : (Consume(TokenType.IDENTIFIER)?.Lexeme ?? throw Error("Expected concept name or '*'"));
                Consume(TokenType.RPAREN);
                node.Actions.Add(new MaintenanceAction { ActionType = MaintenanceActionType.Reindex, TargetName = target });
            }
            else if (actionToken.Type == TokenType.CHECK)
            {
                Consume(TokenType.LPAREN);
                Consume(TokenType.CONSISTENCY);
                Consume(TokenType.COLON);
                string target = Check(TokenType.STAR) ? Advance().Lexeme : (Consume(TokenType.IDENTIFIER)?.Lexeme ?? throw Error("Expected concept name or '*'"));
                Consume(TokenType.RPAREN);
                node.Actions.Add(new MaintenanceAction { ActionType = MaintenanceActionType.CheckConsistency, TargetName = target });
            }
            else
            {
                throw Error($"Unexpected maintenance action: {actionToken.Lexeme}");
            }

            if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
        }

        Consume(TokenType.RPAREN);
        return node;
    }
    private KBMS.Parser.Ast.Kql.DescribeNode ParseDescribe()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.DESCRIBE);

        bool hasParen = Check(TokenType.LPAREN);
        if (hasParen) Consume(TokenType.LPAREN);

        var typeToken = Advance() ?? throw Error("Expected target type (CONCEPT, KB, RULE, HIERARCHY)");
        string typeStr = typeToken.Lexeme.ToUpper();
        if (typeStr == "KNOWLEDGE") {
            if (Consume(TokenType.BASE) == null) throw Error("Expected 'BASE' after 'KNOWLEDGE'");
            typeStr = "KB";
        }
        else if (typeStr == "KB" || typeStr == "CONCEPT" || typeStr == "RULE" || typeStr == "HIERARCHY" ||
                 typeStr == "RELATION" || typeStr == "FUNCTION" || typeStr == "OPERATOR") {
            // Valid
        }
        else {
            throw Error($"Unexpected target type: {typeStr}", typeToken);
        }

        if (Check(TokenType.COLON)) Consume(TokenType.COLON);

        string targetName = "";

        if (typeStr == "HIERARCHY") {
            if (Check(TokenType.STRING)) {
                targetName = Advance()!.Literal!.ToString()!;
            } else {
                var childToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected child concept name");
                if (Check(TokenType.COLON)) {
                    Consume(TokenType.COLON);
                    var parentToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected parent concept name after ':'");
                    targetName = $"{childToken.Lexeme}:{parentToken.Lexeme}";
                } else if (Check(TokenType.IS_A) || Check(TokenType.PART_OF)) {
                    var relToken = Advance()!;
                    var parentToken = Consume(TokenType.IDENTIFIER) ?? throw Error($"Expected parent concept name after {relToken.Lexeme}");
                    targetName = $"{childToken.Lexeme}:{parentToken.Lexeme}";
                } else {
                    throw Error("Expected ':' or IS_A/PART_OF in hierarchy description");
                }
            }
        } else {
            var nameToken = Advance() ?? throw Error("Expected target name or id");
            targetName = nameToken.Lexeme;
            if (nameToken.Type == TokenType.STRING && nameToken.Literal != null) {
                targetName = nameToken.Literal.ToString()!;
            }
        }

        if (hasParen && Consume(TokenType.RPAREN) == null) throw Error("Expected ')' at end of DESCRIBE");

        return new KBMS.Parser.Ast.Kql.DescribeNode
        {
            TargetType = typeStr,
            TargetName = targetName,
            Line = token.Line,
            Column = token.Column
        };
    }

    private KBMS.Parser.Ast.Kml.ExportNode ParseExport() 
    { 
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.EXPORT);
        if (Consume(TokenType.LPAREN) == null) throw Error("Expected '(' after EXPORT");
        
        if (Consume(TokenType.CONCEPT) == null) throw Error("Expected 'CONCEPT' in EXPORT parameters");
        if (Consume(TokenType.COLON) == null) throw Error("Expected ':' after 'CONCEPT'");
        string name = Check(TokenType.STAR) ? Advance().Lexeme : (ConsumeIdentifier()?.Lexeme ?? throw Error("Expected concept name or '*'"));
        
        if (Consume(TokenType.COMMA) == null) throw Error("Expected ',' after concept parameter");
        if (Consume(TokenType.FORMAT) == null) throw Error("Expected 'FORMAT' in EXPORT parameters");
        if (Consume(TokenType.COLON) == null) throw Error("Expected ':' after 'FORMAT'");
        var formatToken = Advance() ?? throw Error("Expected format (JSON)");

        if (Consume(TokenType.COMMA) == null) throw Error("Expected ',' after format parameter");
        if (Consume(TokenType.FILE) == null) throw Error("Expected 'FILE' in EXPORT parameters");
        if (Consume(TokenType.COLON) == null) throw Error("Expected ':' after 'FILE'");
        var fileToken = Consume(TokenType.STRING) ?? throw Error("Expected file path string");

        if (Consume(TokenType.RPAREN) == null) throw Error("Expected ')' at end of EXPORT");
        
        return new KBMS.Parser.Ast.Kml.ExportNode 
        { 
            TargetType = "CONCEPT",
            TargetName = name,
            Format = formatToken.Lexeme,
            FilePath = fileToken.Literal?.ToString() ?? fileToken.Lexeme.Trim('\\', '$', '"'),
            Line = token.Line,
            Column = token.Column
        }; 
    }
    
    private KBMS.Parser.Ast.Kml.ImportNode ParseImport() 
    { 
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.IMPORT);
        if (Consume(TokenType.LPAREN) == null) throw Error("Expected '(' after IMPORT");
        
        if (Consume(TokenType.CONCEPT) == null) throw Error("Expected 'CONCEPT' in IMPORT parameters");
        if (Consume(TokenType.COLON) == null) throw Error("Expected ':' after 'CONCEPT'");
        string name = Check(TokenType.STAR) ? Advance().Lexeme : (ConsumeIdentifier()?.Lexeme ?? throw Error("Expected concept name or '*'"));
        
        if (Consume(TokenType.COMMA) == null) throw Error("Expected ',' after concept parameter");
        if (Consume(TokenType.FORMAT) == null) throw Error("Expected 'FORMAT' in IMPORT parameters");
        if (Consume(TokenType.COLON) == null) throw Error("Expected ':' after 'FORMAT'");
        var formatToken = Advance() ?? throw Error("Expected format (CSV|JSON)");

        if (Consume(TokenType.COMMA) == null) throw Error("Expected ',' after format parameter");
        if (Consume(TokenType.FILE) == null) throw Error("Expected 'FILE' in IMPORT parameters");
        if (Consume(TokenType.COLON) == null) throw Error("Expected ':' after 'FILE'");
        var fileToken = Consume(TokenType.STRING) ?? throw Error("Expected file path string");

        if (Consume(TokenType.RPAREN) == null) throw Error("Expected ')' at end of IMPORT");
        
        return new KBMS.Parser.Ast.Kml.ImportNode 
        { 
            TargetType = "CONCEPT",
            TargetName = name,
            Format = formatToken.Lexeme,
            FilePath = fileToken.Literal?.ToString() ?? fileToken.Lexeme.Trim('\\', '$', '"'),
            Line = token.Line,
            Column = token.Column
        };
    }

    private AstNode ParseDrop()
    {
        Consume(TokenType.DROP);
        var token = Peek();

        if (token == null)
            throw Error("Expected token after DROP");

        return token.Type switch
        {
            TokenType.KNOWLEDGE => ParseDropKnowledgeBase(),
            TokenType.CONCEPT => ParseDropConcept(),
            TokenType.RELATION => ParseDropRelation(),
            TokenType.OPERATOR => ParseDropOperator(),
            TokenType.FUNCTION => ParseDropFunction(),
            TokenType.RULE => ParseDropRule(),
            TokenType.USER => ParseDropUser(),
            _ => throw Error($"Unexpected token after DROP: {token.Lexeme}", token)
        };
    }

    private DropKbNode ParseDropKnowledgeBase()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.KNOWLEDGE);
        Consume(TokenType.BASE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.CONCEPT);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.RELATION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected relation name");
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.OPERATOR);

        var symbolToken = Peek() ?? throw Error("Expected operator symbol");
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
            throw Error($"Expected operator symbol, got {symbolToken.Lexeme}", symbolToken);
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.FUNCTION);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected function name");
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.RULE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected rule name");
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.USER);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected username");
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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.USE);

        var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
            throw Error("Expected token after ADD");

        return token.Type switch
        {
            TokenType.VARIABLE => ParseAddVariable(),
            TokenType.HIERARCHY => ParseAddHierarchy(),
            TokenType.COMPUTATION => ParseAddComputation(),
            _ => throw Error($"Unexpected token after ADD: {token.Lexeme}", token)
        };
    }

    private AddVariableNode ParseAddVariable()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.VARIABLE);

        // Accept any token as variable name (including keywords)
        var nameToken = Peek();
        if (nameToken == null || nameToken.Type == TokenType.COLON)
            throw Error("Expected variable name");
        Advance();
        Consume(TokenType.COLON);
        var typeToken = Peek() ?? throw Error("Expected variable type");

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
            var lengthToken = Consume(TokenType.NUMBER) ?? throw Error("Expected length");
            node.Length = (int?)ConvertToDouble(lengthToken.Literal);

            if (Check(TokenType.COMMA))
            {
                var scaleToken = Consume(TokenType.NUMBER) ?? throw Error("Expected scale");
                node.Scale = (int?)ConvertToDouble(scaleToken.Literal);
            }
            Consume(TokenType.RPAREN);
        }

        Consume(TokenType.TO);
        Consume(TokenType.CONCEPT);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        node.ConceptName = conceptToken.Lexeme;

        return node;
    }

    private AddHierarchyNode ParseAddHierarchy()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.HIERARCHY);

        var firstConceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");

        var typeToken = Peek() ?? throw Error("Expected hierarchy type");
        KBMS.Parser.Ast.Kdl.HierarchyType hierarchyType;

        if (typeToken.Type == TokenType.IS_A || typeToken.Type == TokenType.ISA)
        {
            hierarchyType = KBMS.Parser.Ast.Kdl.HierarchyType.IS_A;
        }
        else if (typeToken.Type == TokenType.PART_OF)
        {
            hierarchyType = KBMS.Parser.Ast.Kdl.HierarchyType.PART_OF;
        }
        else
        {
            throw Error($"Expected IS_A, ISA, or PART_OF, got {typeToken.Lexeme}", typeToken);
        }
        Advance();

        var secondConceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");

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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.COMPUTATION);
        Consume(TokenType.TO);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
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
            var varToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected variable name");
            node.InputVariables.Add(varToken.Lexeme);
            if (Check(TokenType.COMMA))
            {
                Consume(TokenType.COMMA);
            }
            else if (!Check(TokenType.FORMULA))
            {
                throw Error("Expected comma or FORMULA", Peek()!);
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
        var formulaToken = Consume(TokenType.STRING) ?? throw Error("Expected formula string");
        node.Formula = formulaToken.Literal?.ToString() ?? "";

        // Parse COST clause
        if (Check(TokenType.COST))
        {
            Consume(TokenType.COST);
            var costToken = Consume(TokenType.NUMBER) ?? throw Error("Expected cost number");
            node.Cost = (int?)ConvertToDouble(costToken.Literal);
        }

        return node;
    }

    private AstNode ParseRemove()
    {
        Consume(TokenType.REMOVE);
        var token = Peek();

        if (token == null)
            throw Error("Expected token after REMOVE");

        return token.Type switch
        {
            TokenType.HIERARCHY => ParseRemoveHierarchy(),
            TokenType.COMPUTATION => ParseRemoveComputation(),
            _ => throw Error($"Unexpected token after REMOVE: {token.Lexeme}", token)
        };
    }

    private RemoveHierarchyNode ParseRemoveHierarchy()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.HIERARCHY);

        var parentToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected parent concept");

        var typeToken = Peek() ?? throw Error("Expected hierarchy type");
        KBMS.Parser.Ast.Kdl.HierarchyType hierarchyType;

        if (typeToken.Type == TokenType.IS_A || typeToken.Type == TokenType.ISA)
        {
            hierarchyType = KBMS.Parser.Ast.Kdl.HierarchyType.IS_A;
        }
        else if (typeToken.Type == TokenType.PART_OF)
        {
            hierarchyType = KBMS.Parser.Ast.Kdl.HierarchyType.PART_OF;
        }
        else
        {
            throw Error($"Expected IS_A, ISA, or PART_OF, got {typeToken.Lexeme}", typeToken);
        }
        Advance();

        var childToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected child concept");

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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.COMPUTATION);

        var varToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected variable name");
        Consume(TokenType.FROM);
        Consume(TokenType.CONCEPT);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");

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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.GRANT);

        // Accept any identifier or keyword as privilege name
        var privToken = Peek() ?? throw Error("Expected privilege");
        Advance();
        Consume(TokenType.ON);
        var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
        Consume(TokenType.TO);
        var userToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected username");

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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.REVOKE);

        // Accept any identifier or keyword as privilege name
        var privToken = Peek() ?? throw Error("Expected privilege");
        Advance();
        Consume(TokenType.ON);
        var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
        Consume(TokenType.FROM);
        var userToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected username");

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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.SELECT);

        var node = new SelectNode
        {
            Type = "SELECT",
            Line = token.Line,
            Column = token.Column
        };

        // Parse SELECT columns: *, aggregates, or named column list with optional AS aliases
        if (Check(TokenType.STAR))
        {
            Consume(TokenType.STAR);
            // SELECT * - SelectColumns remains empty => means all columns
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
        else if (Check(TokenType.IDENTIFIER) || Check(TokenType.NUMBER) || Check(TokenType.LPAREN) || Check(TokenType.CALC))
        {
            // Heuristic to distinguish SELECT <ColumnList> FROM ... vs SELECT <ShorthandConceptName> [WHERE...]
            // It's a column list if followed by DOT, AS, COMMA, or FROM, or if it starts with literal/expression markers.
            var next = PeekNext();
            bool isColumnList = Check(TokenType.NUMBER) || Check(TokenType.LPAREN) || Check(TokenType.CALC) || (
                next != null && (
                    next.Type == TokenType.DOT || 
                    next.Type == TokenType.AS || 
                    next.Type == TokenType.COMMA || 
                    next.Type == TokenType.FROM ||
                    next.Type == TokenType.STAR ||
                    next.Type == TokenType.PLUS ||
                    next.Type == TokenType.MINUS ||
                    next.Type == TokenType.SLASH ||
                    next.Type == TokenType.NUMBER ||
                    next.Type == TokenType.LPAREN
                )
            );

            if (isColumnList)
            {
                // Parse column list
                node.SelectColumns.Add(ParseSelectColumn());
                while (Check(TokenType.COMMA))
                {
                    Consume(TokenType.COMMA);
                    if (Check(TokenType.FROM))
                        throw Error("Trailing comma in SELECT list");

                    if (Check(TokenType.STAR))
                    {
                        Consume(TokenType.STAR);
                        node.SelectColumns.Clear();
                        break;
                    }
                    node.SelectColumns.Add(ParseSelectColumn());
                }
            }
            else
            {
                // Shorthand ConceptName (e.g. SELECT Person WHERE...)
                node.ConceptName = Consume(TokenType.IDENTIFIER)!.Lexeme;
            }
        }

        // FROM clause (for SELECT * FROM concept or SELECT COUNT(*) FROM concept)
        if (Check(TokenType.FROM))
        {
            Consume(TokenType.FROM);

            // Handle SELECT * FROM <ENTITY> <NAME>
            if (Check(TokenType.CONCEPT) || Check(TokenType.RELATION) || Check(TokenType.RULE) || 
                Check(TokenType.HIERARCHY) || Check(TokenType.OPERATOR) || Check(TokenType.FUNCTION))
            {
                var entityTypeToken = Advance()!;
                node.TargetType = entityTypeToken.Type.ToString();
                
                // For HIERARCHY, the name might be an identifier or a string, 
                // but let's assume IDENTIFIER for consistency with other entities.
                var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected " + node.TargetType + " name");
                node.ConceptName = nameToken.Lexeme;
            }
            else
            {
                var nameToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name or entity type after FROM");
                node.ConceptName = nameToken.Lexeme;
                node.TargetType = "CONCEPT"; // Default if not specified
            }

            // Handle dots (e.g., system.concepts or Concept.variables)
            if (Check(TokenType.DOT))
            {
                Consume(TokenType.DOT);
                var subTargetToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected sub-target after dot");
                node.ConceptName += "." + subTargetToken.Lexeme;
            }
        }

        // Parse optional AS alias or shorthand alias
        if (Check(TokenType.AS))
        {
            Consume(TokenType.AS);
            var aliasToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected alias");
            node.Alias = aliasToken.Lexeme;
        }
        else if (Check(TokenType.IDENTIFIER) && 
                 Peek()?.Type != TokenType.JOIN && Peek()?.Type != TokenType.WHERE && 
                 Peek()?.Type != TokenType.GROUP && Peek()?.Type != TokenType.ORDER && 
                 Peek()?.Type != TokenType.LIMIT && Peek()?.Type != TokenType.SEMICOLON)
        {
            node.Alias = Consume(TokenType.IDENTIFIER)!.Lexeme;
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
            Consume(TokenType.HAVING);
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
            var limitToken = Consume(TokenType.NUMBER) ?? throw Error("Expected limit value");
            var limit = int.Parse(limitToken.Lexeme);
            int? offset = null;
            if (Check(TokenType.OFFSET))
            {
                Consume(TokenType.OFFSET);
                var offsetToken = Consume(TokenType.NUMBER) ?? throw Error("Expected offset value");
                offset = int.Parse(offsetToken.Lexeme);
            }
            node.Limit = new LimitClause { Limit = limit, Offset = offset };
        }

        return node;
    }

    private SelectColumn ParseSelectColumn()
    {
        var col = new SelectColumn();
        
        // Parse as a full expression
        var expr = ParseExpression();
        col.Expression = expr;
        col.Name = expr.ToString(); // Fallback/Display name

        // Try to extract TablePrefix for simple p.name patterns
        if (expr is VariableNode varNode)
        {
            var rawName = varNode.Name;
            var dotIndex = rawName.IndexOf('.');
            if (dotIndex > 0 && dotIndex < rawName.Length - 1)
            {
                var possiblePrefix = rawName.Substring(0, dotIndex);
                var possibleName = rawName.Substring(dotIndex + 1);
                
                // Check if it's a simple identifier.identifier
                if (!possiblePrefix.Contains(" ") && !possibleName.Contains(" ") && !possibleName.Contains("*"))
                {
                    col.TablePrefix = possiblePrefix;
                    col.Name = possibleName;
                }
            }
        }

        if (Check(TokenType.AS))
        {
            Consume(TokenType.AS);
            col.Alias = (Consume(TokenType.IDENTIFIER) ?? throw Error("Expected alias name")).Lexeme;
        }
        return col;
    }

    private JoinClause ParseJoinClause()
    {
        var join = new JoinClause();

        var targetToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept or relation name");
        join.Target = targetToken.Lexeme;

        // Parse optional AS alias
        if (Check(TokenType.AS))
        {
            Consume(TokenType.AS);
            var aliasToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected alias");
            join.Alias = aliasToken.Lexeme;
        }
        else if (Check(TokenType.IDENTIFIER) && Peek()?.Type != TokenType.ON)
        {
            join.Alias = Consume(TokenType.IDENTIFIER)!.Lexeme;
        }

        // Parse ON clause
        if (Check(TokenType.ON))
        {
            Consume(TokenType.ON);
            join.OnCondition = ParseCondition();
        }

        return join;
    }

    private AstNode ParseInsertOrBulk()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.INSERT);

        // Check for BULK keyword
        if (Check(TokenType.IDENTIFIER) && Peek()?.Lexeme?.Equals("BULK", StringComparison.OrdinalIgnoreCase) == true)
        {
            Advance(); // consume BULK
            return ParseInsertBulkBody(token);
        }

        // Standard single-row INSERT
        Consume(TokenType.INTO);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        var node = new InsertNode
        {
            Type = "INSERT",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.ATTRIBUTE);
        Consume(TokenType.LPAREN);

        int positionIndex = 0;
        while (!Check(TokenType.RPAREN))
        {
            var firstToken = Peek();
            if (firstToken == null)
                throw Error("Expected value");

            if (firstToken.Type == TokenType.IDENTIFIER)
            {
                var nextToken = PeekNext();
                if (nextToken?.Type == TokenType.COLON)
                {
                    var fieldToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected field name");
                    Consume(TokenType.COLON);
                    var valueNode = ParseValueNode();
                    node.Values[fieldToken.Lexeme] = valueNode;
                }
                else
                {
                    var valueNode = ParseValueNode();
                    node.Values[$"_{positionIndex}"] = valueNode;
                    positionIndex++;
                }
            }
            else
            {
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


    private InsertBulkNode ParseInsertBulkBody(Token startToken)
    {
        var bulkNode = new InsertBulkNode
        {
            Line = startToken.Line,
            Column = startToken.Column
        };

        Consume(TokenType.INTO);
        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        bulkNode.ConceptName = conceptToken.Lexeme;

        // Consume ATTRIBUTES or ATTRIBUTE
        if (!Check(TokenType.ATTRIBUTE))
            throw Error("Expected ATTRIBUTES after concept name");
        Consume(TokenType.ATTRIBUTE);

        // Parse one or more row groups: (...), (...), ...
        do
        {
            if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
            Consume(TokenType.LPAREN);

            var row = new Dictionary<string, ValueNode>();
            int positionIndex = 0;

            while (!Check(TokenType.RPAREN))
            {
                var firstToken = Peek();
                if (firstToken == null) throw Error("Expected value");

                if (firstToken.Type == TokenType.IDENTIFIER)
                {
                    var nextToken = PeekNext();
                    if (nextToken?.Type == TokenType.COLON)
                    {
                        var fieldToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected field name");
                        Consume(TokenType.COLON);
                        var valueNode = ParseValueNode();
                        row[fieldToken.Lexeme] = valueNode;
                    }
                    else
                    {
                        var valueNode = ParseValueNode();
                        row[$"_{positionIndex}"] = valueNode;
                        positionIndex++;
                    }
                }
                else
                {
                    var valueNode = ParseValueNode();
                    row[$"_{positionIndex}"] = valueNode;
                    positionIndex++;
                }

                if (!Check(TokenType.RPAREN))
                    Consume(TokenType.COMMA);
            }
            Consume(TokenType.RPAREN);
            bulkNode.Rows.Add(row);

        } while (Check(TokenType.COMMA));

        return bulkNode;
    }

    private InsertNode ParseInsert()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.INSERT);
        Consume(TokenType.INTO);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        var node = new InsertNode
        {
            Type = "INSERT",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.ATTRIBUTE);
        Consume(TokenType.LPAREN);

        // Parse values - support both positional and named syntax
        // Positional: ATTRIBUTE (value1, value2, value3)
        // Named: ATTRIBUTE (field1 : value1, field2 : value2)
        int positionIndex = 0;
        while (!Check(TokenType.RPAREN))
        {
            var firstToken = Peek();
            if (firstToken == null)
                throw Error("Expected value");

            // Check if this is named syntax: IDENTIFIER : value
            if (firstToken.Type == TokenType.IDENTIFIER)
            {
                var nextToken = PeekNext();
                if (nextToken?.Type == TokenType.COLON)
                {
                    // Named syntax
                    var fieldToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected field name");
                    Consume(TokenType.COLON);
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
        var token = Peek() ?? throw Error("Expected value");

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
                throw Error($"Unexpected value type: {token.Lexeme}", token);
        }

        return valueNode;
    }

    private UpdateNode ParseUpdate()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.UPDATE);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
        var node = new UpdateNode
        {
            Type = "UPDATE",
            ConceptName = conceptToken.Lexeme,
            Line = token.Line,
            Column = token.Column
        };

        Consume(TokenType.ATTRIBUTE);
        Consume(TokenType.LPAREN);
        Consume(TokenType.SET);

        // Parse SET values
        while (!Check(TokenType.RPAREN) && !IsAtEnd())
        {
            var fieldToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected field name");
            Consume(TokenType.COLON);
            var expr = ParseExpression();
            node.SetValues[fieldToken.Lexeme] = expr;

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else if (Check(TokenType.RPAREN))
                break;
            else
                throw Error("Expected ',' or ')' in ATTRIBUTE block");
        }
        Consume(TokenType.RPAREN);

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
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.DELETE);
        
        if (Check(TokenType.FROM))
            Consume(TokenType.FROM);

        var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
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



    private ShowNode ParseShow()
    {
        var token = Peek() ?? throw Error("Unexpected end of input");
        Consume(TokenType.SHOW);

        var node = new ShowNode
        {
            Line = token.Line,
            Column = token.Column
        };

        // Determine show type
        if (Peek()?.Type == TokenType.KNOWLEDGE)
        {
            Consume(TokenType.KNOWLEDGE);
            
            // Handle both BASE (keyword) and BASES (identifier)
            if (Peek()?.Type == TokenType.BASE) 
            {
                Consume(TokenType.BASE);
            }
            else if (Peek()?.Type == TokenType.IDENTIFIER && Peek()?.Lexeme.ToUpper() == "BASES")
            {
                Consume(TokenType.IDENTIFIER);
            }
            else 
            {
                throw Error("Expected BASE or BASES after KNOWLEDGE");
            }

            node.ShowType = ShowType.KnowledgeBases;
            node.Type = "SHOW_KNOWLEDGE_BASES";
        }
        else if (Peek()?.Type == TokenType.CONCEPT && PeekNext()?.Type == TokenType.IDENTIFIER)
        {
            // SHOW CONCEPT <name> - show concept detail
            Consume(TokenType.CONCEPT);
            var conceptToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected concept name");
            node.ShowType = ShowType.ConceptDetail;
            node.Type = "SHOW_CONCEPT";
            node.ConceptName = conceptToken.Lexeme;

            if (Check(TokenType.IN))
            {
                Consume(TokenType.IN);
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
                Consume(TokenType.IN);
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
                Consume(TokenType.IN);
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }

            if (Check(TokenType.TYPE))
            {
                var ruleTypeToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected rule type");
                node.RuleType = ruleTypeToken.Lexeme.ToLower();
            }
        }
        else if (Peek()?.Type == TokenType.HIERARCHIES)
        {
            Consume(TokenType.HIERARCHIES);
            node.ShowType = ShowType.Hierarchies;
            node.Type = "SHOW_HIERARCHIES";

            if (Check(TokenType.IN))
            {
                Consume(TokenType.IN);
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
        }
        else if (Peek()?.Type == TokenType.RELATIONS)
        {
            Consume(TokenType.RELATIONS);
            node.ShowType = ShowType.Relations;
            node.Type = "SHOW_RELATIONS";

            if (Check(TokenType.IN))
            {
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
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
                var kbToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected knowledge base name");
                node.KbName = kbToken.Lexeme;
            }
            else if (Check(TokenType.OF))
            {
                node.ShowType = ShowType.PrivilegesOfUser;
                node.Type = "SHOW_PRIVILEGES_OF";
                var userToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected username");
                node.Username = userToken.Lexeme;
            }
            else
            {
                throw Error("Expected ON or OF after PRIVILEGES");
            }
        }
        else
        {
            var errorToken = Peek();
            throw Error($"Unexpected show type: {errorToken?.Lexeme}", errorToken);
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
        var token = Peek() ?? throw Error("Unexpected end of input");

        if (Check(TokenType.CALC))
        {
            Consume(TokenType.CALC);
            Consume(TokenType.LPAREN);
            var expr = ParseExpression();
            Consume(TokenType.RPAREN);
            return expr; // CALC(x) just returns the expression but makes it explicit
        }

        if (token == null)
            throw Error("Unexpected end of expression");

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
                throw Error($"Unexpected token in expression: {token.Lexeme}", token);
        }
    }

    private Condition ParseCondition()
    {
        var left = ParseExpression();

        if (left is VariableNode varNode)
        {
            var opToken = Peek() ?? throw Error("Expected operator");

            if (IsComparisonOperator(opToken.Type))
            {
                Advance();
                object? value = null;

                if (opToken.Type == TokenType.IN)
                {
                    Consume(TokenType.LPAREN);
                    if (Check(TokenType.SELECT))
                    {
                        var subQuery = ParseSelect();
                        value = subQuery;
                    }
                    else
                    {
                        var list = new List<object>();
                        while (!Check(TokenType.RPAREN) && !IsAtEnd())
                        {
                            var expr = ParseExpression();
                            if (expr is LiteralNode lit) list.Add(lit.Value);
                            else if (expr is VariableNode vNode) list.Add(vNode.Name);
                            else list.Add(expr.ToString()!);
                            
                            if (Check(TokenType.COMMA)) Advance();
                        }
                        value = list;
                    }
                    Consume(TokenType.RPAREN);
                }
                else
                {
                    var right = ParseExpression();
                    value = right switch
                    {
                        LiteralNode lit => lit.Value,
                        VariableNode v => v.Name,
                        _ => right.ToString()
                    };
                }

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
            object? value = binary.Right switch
            {
                LiteralNode lit => lit.Value,
                VariableNode v => v.Name,
                _ => binary.Right?.ToString()
            };

            return new Condition
            {
                Field = binary.Left?.ToString() ?? "",
                Operator = binary.Operator,
                Value = value
            };
        }

        throw Error("Invalid condition");
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

        bool hasParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasParens && Check(TokenType.RPAREN))
                break;
            if (!hasParens && IsClauseKeyword(Peek()?.Type))
                break;

            var token = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected identifier");
            list.Add(token.Lexeme);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        if (hasParens)
        {
            Consume(TokenType.RPAREN);
        }

        return list;
    }

    private List<ConstructRelationDef> ParseConstructRelationList()
    {
        var list = new List<ConstructRelationDef>();

        bool hasParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasParens && Check(TokenType.RPAREN))
                break;
            if (!hasParens && IsClauseKeyword(Peek()?.Type))
                break;

            var relToken = ConsumeIdentifier() ?? throw Error("Expected relation name");
            var def = new ConstructRelationDef { RelationName = relToken.Lexeme };

            // Parse function-style arguments: RelName(arg1, arg2, ...)
            if (Check(TokenType.LPAREN))
            {
                Consume(TokenType.LPAREN);
                while (!Check(TokenType.RPAREN) && !IsAtEnd())
                {
                    var argToken = ConsumeIdentifier() ?? throw Error("Expected argument name");
                    def.Arguments.Add(argToken.Lexeme);
                    if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
                }
                Consume(TokenType.RPAREN);
            }
            else
            {
                // Fallback: old syntax with two identifiers
                var arg1 = ConsumeIdentifier();
                var arg2 = ConsumeIdentifier();
                if (arg1 != null) def.Arguments.Add(arg1.Lexeme);
                if (arg2 != null) def.Arguments.Add(arg2.Lexeme);
            }

            list.Add(def);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        if (hasParens)
        {
            Consume(TokenType.RPAREN);
        }

        return list;
    }

    private List<PropertyDef> ParsePropertyList()
    {
        var list = new List<PropertyDef>();

        bool hasParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasParens && Check(TokenType.RPAREN))
                break;
            if (!hasParens && IsClauseKeyword(Peek()?.Type))
                break;

            var keyToken = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected property key");
            if (Check(TokenType.COLON)) Consume(TokenType.COLON); else if (Check(TokenType.EQUALS)) Consume(TokenType.EQUALS); else throw Error("Expected ':' or '=' after property key, but found " + (Peek()?.Type.ToString() ?? "null") + " ('" + (Peek()?.Lexeme ?? "") + "')");
            var valToken = Advance() ?? throw Error("Expected property value");
            
            list.Add(new PropertyDef {
                Key = keyToken.Lexeme,
                Value = valToken.Literal?.ToString() ?? valToken.Lexeme
            });

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        if (hasParens)
        {
            Consume(TokenType.RPAREN);
        }

        return list;
    }

    private List<ConceptRuleDef> ParseConceptRuleList()
    {
        var list = new List<ConceptRuleDef>();

        bool hasParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasParens && Check(TokenType.RPAREN))
                break;
            if (!hasParens && IsClauseKeyword(Peek()?.Type))
                break;
            if (!Check(TokenType.TYPE) && !Check(TokenType.IF) && !Check(TokenType.RULE) && !Check(TokenType.VARIABLES))
                break;

            var rule = new ConceptRuleDef();
            
            if (Check(TokenType.RULE) || Check(TokenType.TYPE))
            {
                Advance(); // Consume RULE or TYPE
                // Optional colon after RULE/TYPE
                if (Check(TokenType.COLON)) Consume(TokenType.COLON);
                
                // Optional name/kind
                if (Check(TokenType.IDENTIFIER))
                {
                    var typeToken = Consume(TokenType.IDENTIFIER);
                    rule.Kind = typeToken?.Lexeme;
                    if (Check(TokenType.COLON)) Consume(TokenType.COLON);
                }
            }

            if (Check(TokenType.VARIABLES))
            {
                Consume(TokenType.VARIABLES);
                Consume(TokenType.LPAREN);
                while (!Check(TokenType.RPAREN))
                {
                    rule.Variables.Add(ParseVariableDefinition());
                    if (!Check(TokenType.RPAREN)) Consume(TokenType.COMMA);
                }
                Consume(TokenType.RPAREN);
            }

            if (Check(TokenType.IF))
            {
                Consume(TokenType.IF);
                rule.Hypothesis = new List<string> { ParseExpressionString() };
            }

            if (Check(TokenType.THEN))
            {
                Consume(TokenType.THEN);
                rule.Conclusion = new List<string> { ParseExpressionString() };
            }

            list.Add(rule);

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
            else
                break;
        }

        if (hasParens) { Consume(TokenType.RPAREN); }

        return list;
    }

    private List<EquationDef> ParseEquationList()
    {
        var list = new List<EquationDef>();

        bool hasParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasParens && Check(TokenType.RPAREN))
                break;
            if (!hasParens && IsClauseKeyword(Peek()?.Type))
                break;

            var startToken = Peek() ?? throw Error("Expected equation");
            var expression = ParseExpressionString();
            if (string.IsNullOrEmpty(expression))
            {
                throw Error($"Empty equation or unexpected token '{Peek()?.Lexeme}' inside EQUATIONS block", Peek());
            }
            list.Add(new EquationDef 
            { 
                Expression = expression,
                Line = startToken.Line,
                Column = startToken.Column
            });

            if (Check(TokenType.COMMA)) Consume(TokenType.COMMA);
            else break;
        }

        if (hasParens)
        {
            Consume(TokenType.RPAREN);
        }

        return list;
    }

    private List<ConstraintDef> ParseConstraintList()
    {
        var list = new List<ConstraintDef>();

        // (V2) Check for optional block parentheses
        bool hasBlockParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasBlockParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasBlockParens && Check(TokenType.RPAREN))
                break;
            if (!hasBlockParens && IsClauseKeyword(Peek()?.Type))
                break;
            if (Check(TokenType.RPAREN)) // Safety for nested cases
                break;

            string name = string.Empty;
            var startToken = Peek() ?? throw Error("Expected constraint");

            // Lookahead for named constraint: name: expression
            if (Peek()?.Type == TokenType.IDENTIFIER && PeekNext()?.Type == TokenType.COLON)
            {
                name = Advance().Lexeme;
                Consume(TokenType.COLON);
                // The actual expression starts after the colon
                startToken = Peek() ?? throw Error("Expected constraint expression after ':'");
            }

            var expression = ParseExpressionString();
            if (string.IsNullOrEmpty(expression))
            {
                throw Error($"Empty constraint or unexpected token '{Peek()?.Lexeme}' inside CONSTRAINTS block. Note: IF statements are not allowed in CONSTRAINTS, use RULES instead.", Peek());
            }
            list.Add(new ConstraintDef
            {
                Name = name,
                Expression = expression,
                Line = startToken.Line,
                Column = startToken.Column
            });

            if (Check(TokenType.COMMA))
                Consume(TokenType.COMMA);
        }

        if (hasBlockParens)
        {
            Consume(TokenType.RPAREN);
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
            if (parenCount == 0 && (token.Type == TokenType.COMMA || token.Type == TokenType.RPAREN || IsClauseKeyword(token.Type)))
                break;

            if (token.Type == TokenType.LPAREN) parenCount++;
            if (token.Type == TokenType.RPAREN) parenCount--;

            sb.Append(token.Lexeme);
            Advance();
        }

        return sb.ToString().Trim();
    }

    private List<SameVariableGroup> ParseSameVariablesList()
    {
        var list = new List<SameVariableGroup>();

        bool hasParens = false;
        if (Check(TokenType.LPAREN))
        {
            Consume(TokenType.LPAREN);
            hasParens = true;
        }

        while (!IsAtEnd())
        {
            if (hasParens && Check(TokenType.RPAREN))
                break;
            if (!hasParens && IsClauseKeyword(Peek()?.Type))
                break;

            var var1Token = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected variable name");
            Consume(TokenType.EQUALS);
            var var2Token = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected variable name");

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

        if (hasParens)
        {
            Consume(TokenType.RPAREN);
        }

        return list;
    }

    private List<ExpressionNode> ParseExpressionASTList()
    {
        var list = new List<ExpressionNode>();

        while (!IsAtEnd() && !IsClauseKeyword(Peek()?.Type))
        {
            // (RC4) Allow optional SET keyword before each expression (common in rule conclusions)
            if (Check(TokenType.SET))
                Consume(TokenType.SET);

            var expr = ParseExpression();
            list.Add(expr);

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
            var token = Consume(TokenType.IDENTIFIER) ?? throw Error("Expected variable name");
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
                throw Error("Expected variable name");
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
               type == TokenType.LESS_EQUAL ||
               type == TokenType.IN;
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
               type == TokenType.CONSTRUCT_RELATIONS ||
               type == TokenType.RULES ||
               type == TokenType.RETURNS ||
               type == TokenType.BODY ||
               type == TokenType.PROPERTIES ||
               type == TokenType.FORMULA ||
               type == TokenType.COST ||
               type == TokenType.EQUATION ||
               type == TokenType.EQUATIONS ||
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
    private Token? ConsumeIdentifier()
    {
        var token = Peek();
        if (token == null) return null;

        if (token.Type == TokenType.IDENTIFIER || IsSoftKeyword(token.Type))
        {
            return Advance();
        }
        return null;
    }

    private bool IsSoftKeyword(TokenType type)
    {
        return type == TokenType.DESCRIPTION || 
               type == TokenType.DESC || 
               type == TokenType.ASC || 
               type == TokenType.TYPE || 
               type == TokenType.COST || 
               type == TokenType.DATE ||
               type == TokenType.TIMESTAMP;
    }

    // ==================== TCL Parsers ====================

    private AstNode ParseBeginTransaction()
    {
        Consume(TokenType.BEGIN);
        // Optionally consume TRANSACTION keyword
        if (Peek()?.Type == TokenType.TRANSACTION)
            Consume(TokenType.TRANSACTION);
        return new Ast.Tcl.BeginTransactionNode();
    }

    private AstNode ParseCommit()
    {
        Consume(TokenType.COMMIT);
        return new Ast.Tcl.CommitNode();
    }

    private AstNode ParseRollback()
    {
        Consume(TokenType.ROLLBACK);
        return new Ast.Tcl.RollbackNode();
    }

    private ParserException Error(string message, Token? token = null)
    {
        var t = token ?? Peek() ?? (_tokens.Count > 0 ? _tokens[^1] : null);
        var response = ErrorResponse.ParserErrorResponse(message, _originalQuery, t?.Line ?? 0, t?.Column ?? 0);
        return new ParserException(response);
    }
}
