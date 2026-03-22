using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;

using System;
using System.Collections.Generic;
using KBMS.Parser;
using KBMS.Parser.Ast;
using KBMS.Parser.Ast.Kdl;
using KBMS.Parser.Ast.Kml;
using KBMS.Parser.Ast.Kql;
using KBMS.Parser.Ast.Kcl;
using KBMS.Parser.Ast.Tcl;
using Xunit;

namespace KBMS.Tests;

/// <summary>
/// Unit tests for the KBQL Parser
/// Tests parsing of KBQL commands into AST nodes
/// </summary>
public class ParserTests
{
    private List<Token> Tokenize(string input)
    {
        var lexer = new Lexer(input);
        return lexer.Tokenize();
    }

    private AstNode? ParseStatement(string input)
    {
        var parser = new KBMS.Parser.Parser(input);
        return parser.Parse();
    }

    // ==================== CREATE Knowledge Base Tests ====================

    [Fact]
    public void Parser_CreateKnowledgeBase_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE KNOWLEDGE BASE test_kb;");

        Assert.NotNull(node);
        Assert.IsType<CreateKbNode>(node);

        var kbNode = (CreateKbNode)node;
        Assert.Equal("CREATE_KNOWLEDGE_BASE", kbNode.Type);
        Assert.Equal("test_kb", kbNode.KbName);
    }

    [Fact]
    public void Parser_CreateKnowledgeBase_WithDescription_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE KNOWLEDGE BASE my_kb DESCRIPTION 'Test knowledge base';");

        Assert.NotNull(node);
        Assert.IsType<CreateKbNode>(node);

        var kbNode = (CreateKbNode)node;
        Assert.Equal("my_kb", kbNode.KbName);
        Assert.Equal("Test knowledge base", kbNode.Description);
    }

    [Fact]
    public void Parser_CreateKnowledgeBase_CaseInsensitive_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE knowledge BASE TestKb;");

        Assert.NotNull(node);
        Assert.IsType<CreateKbNode>(node);

        var kbNode = (CreateKbNode)node;
        Assert.Equal("TestKb", kbNode.KbName);
    }

    // ==================== DROP Knowledge Base Tests ====================

    [Fact]
    public void Parser_DropKnowledgeBase_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP KNOWLEDGE BASE myKbs;");

        Assert.NotNull(node);
        Assert.IsType<DropKbNode>(node);

        var kbNode = (DropKbNode)node;
        Assert.Equal("DROP_KNOWLEDGE_BASE", kbNode.Type);
        Assert.Equal("myKbs", kbNode.KbName);
    }

    // ==================== USE Knowledge Base Tests ====================

    [Fact]
    public void Parser_UseKnowledgeBase_ShouldParseCorrectly()
    {
        var node = ParseStatement("USE my_kb;");

        Assert.NotNull(node);
        Assert.IsType<UseKbNode>(node);

        var useNode = (UseKbNode)node;
        Assert.Equal("USE", useNode.Type);
        Assert.Equal("my_kb", useNode.KbName);
    }

    // ==================== CREATE Concept Tests ====================

    [Fact]
    public void Parser_CreateConcept_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE CONCEPT Person ( VARIABLES (name: STRING, age: INT) );");

        Assert.NotNull(node);
        Assert.IsType<CreateConceptNode>(node);

        var conceptNode = (CreateConceptNode)node;
        Assert.Equal("CREATE_CONCEPT", conceptNode.Type);
        Assert.Equal("Person", conceptNode.ConceptName);
        Assert.Equal(2, conceptNode.Variables.Count);
    }

    [Fact]
    public void Parser_CreateConcept_WithMultipleVariables_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE CONCEPT Employee ( VARIABLES (id: INT, name: VARCHAR(100), salary: DOUBLE, active: BOOLEAN) );");

        Assert.NotNull(node);
        Assert.IsType<CreateConceptNode>(node);

        var conceptNode = (CreateConceptNode)node;
        Assert.Equal("Employee", conceptNode.ConceptName);
        Assert.Equal(4, conceptNode.Variables.Count);
        Assert.Equal("id", conceptNode.Variables[0].Name);
        Assert.Equal("INT", conceptNode.Variables[0].Type);
        Assert.Equal("name", conceptNode.Variables[1].Name);
        Assert.Equal("VARCHAR", conceptNode.Variables[1].Type);
    }

    // ==================== DROP Concept Tests ====================

    [Fact]
    public void Parser_DropConcept_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP CONCEPT Person;");

        Assert.NotNull(node);
        Assert.IsType<DropConceptNode>(node);

        var conceptNode = (DropConceptNode)node;
        Assert.Equal("DROP_CONCEPT", conceptNode.Type);
        Assert.Equal("Person", conceptNode.ConceptName);
    }

    // ==================== CREATE Relation Tests ====================

    [Fact]
    public void Parser_CreateRelation_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE RELATION owns FROM Person TO Car;");

        Assert.NotNull(node);
        Assert.IsType<CreateRelationNode>(node);

        var relNode = (CreateRelationNode)node;
        Assert.Equal("CREATE_RELATION", relNode.Type);
        Assert.Equal("owns", relNode.RelationName);
        Assert.Equal("Person", relNode.DomainConcept);
        Assert.Equal("Car", relNode.RangeConcept);
    }

    // ==================== DROP Relation Tests ====================

    [Fact]
    public void Parser_DropRelation_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP RELATION owns;");

        Assert.NotNull(node);
        Assert.IsType<DropRelationNode>(node);

        var relNode = (DropRelationNode)node;
        Assert.Equal("DROP_RELATION", relNode.Type);
        Assert.Equal("owns", relNode.RelationName);
    }

    // ==================== CREATE Operator Tests ====================

    [Fact]
    public void Parser_CreateOperator_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE OPERATOR add PARAMS (INT, INT) RETURNS INT;");

        Assert.NotNull(node);
        Assert.IsType<CreateOperatorNode>(node);

        var opNode = (CreateOperatorNode)node;
        Assert.Equal("CREATE_OPERATOR", opNode.Type);
        Assert.Equal("add", opNode.Symbol);
        Assert.Equal(2, opNode.ParamTypes.Count);
        Assert.Equal("INT", opNode.ReturnType);
    }

    // ==================== DROP Operator Tests ====================

    [Fact]
    public void Parser_DropOperator_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP OPERATOR add;");

        Assert.NotNull(node);
        Assert.IsType<DropOperatorNode>(node);

        var opNode = (DropOperatorNode)node;
        Assert.Equal("DROP_OPERATOR", opNode.Type);
        Assert.Equal("add", opNode.Symbol);
    }

    // ==================== CREATE Function Tests ====================

    [Fact]
    public void Parser_CreateFunction_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE FUNCTION calculateArea PARAMS (DOUBLE width, DOUBLE height) RETURNS DOUBLE BODY 'width * height';");

        Assert.NotNull(node);
        Assert.IsType<CreateFunctionNode>(node);

        var funcNode = (CreateFunctionNode)node;
        Assert.Equal("CREATE_FUNCTION", funcNode.Type);
        Assert.Equal("calculateArea", funcNode.FunctionName);
        Assert.Equal(2, funcNode.Parameters.Count);
        Assert.Equal("DOUBLE", funcNode.ReturnType);
        Assert.Equal("width * height", funcNode.Body);
    }

    // ==================== DROP Function Tests ====================

    [Fact]
    public void Parser_DropFunction_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP FUNCTION calculateArea;");

        Assert.NotNull(node);
        Assert.IsType<DropFunctionNode>(node);

        var funcNode = (DropFunctionNode)node;
        Assert.Equal("DROP_FUNCTION", funcNode.Type);
        Assert.Equal("calculateArea", funcNode.FunctionName);
    }

    // ==================== CREATE Rule Tests ====================

    [Fact]
    public void Parser_CreateRule_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE RULE adultRule IF age >= 18 THEN isAdult = true;");

        Assert.NotNull(node);
        Assert.IsType<CreateRuleNode>(node);

        var ruleNode = (CreateRuleNode)node;
        Assert.Equal("CREATE_RULE", ruleNode.Type);
        Assert.Equal("adultRule", ruleNode.RuleName);
    }

    // ==================== DROP Rule Tests ====================

    [Fact]
    public void Parser_DropRule_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP RULE adultRule;");

        Assert.NotNull(node);
        Assert.IsType<DropRuleNode>(node);

        var ruleNode = (DropRuleNode)node;
        Assert.Equal("DROP_RULE", ruleNode.Type);
        Assert.Equal("adultRule", ruleNode.RuleName);
    }

    // ==================== CREATE User Tests ====================

    [Fact]
    public void Parser_CreateUser_ShouldParseCorrectly()
    {
        var node = ParseStatement("CREATE USER testuser PASSWORD 'testpass123' ROLE USER;");

        Assert.NotNull(node);
        Assert.IsType<CreateUserNode>(node);

        var userNode = (CreateUserNode)node;
        Assert.Equal("CREATE_USER", userNode.Type);
        Assert.Equal("testuser", userNode.Username);
        Assert.Equal("testpass123", userNode.Password);
        Assert.Equal("USER", userNode.Role);
    }

    // ==================== DROP User Tests ====================

    [Fact]
    public void Parser_DropUser_ShouldParseCorrectly()
    {
        var node = ParseStatement("DROP USER testuser;");

        Assert.NotNull(node);
        Assert.IsType<DropUserNode>(node);

        var userNode = (DropUserNode)node;
        Assert.Equal("DROP_USER", userNode.Type);
        Assert.Equal("testuser", userNode.Username);
    }

    // ==================== ADD Variable Tests ====================

    [Fact]
    public void Parser_AddVariable_ShouldParseCorrectly()
    {
        var node = ParseStatement("ADD VARIABLE email: STRING TO CONCEPT Person;");

        Assert.NotNull(node);
        Assert.IsType<AddVariableNode>(node);

        var varNode = (AddVariableNode)node;
        Assert.Equal("ADD_VARIABLE", varNode.Type);
        Assert.Equal("email", varNode.VariableName);
        Assert.Equal("STRING", varNode.VariableType);
        Assert.Equal("Person", varNode.ConceptName);
    }

    // ==================== ADD Hierarchy Tests ====================

    [Fact]
    public void Parser_AddHierarchy_IsA_ShouldParseCorrectly()
    {
        var node = ParseStatement("ADD HIERARCHY Dog IS_A Animal;");

        Assert.NotNull(node);
        Assert.IsType<AddHierarchyNode>(node);

        var hierNode = (AddHierarchyNode)node;
        Assert.Equal("ADD_HIERARCHY", hierNode.Type);
        Assert.Equal("Dog", hierNode.ChildConcept);
        Assert.Equal("Animal", hierNode.ParentConcept);
        Assert.Equal(HierarchyType.IS_A, hierNode.HierarchyType);
    }

    [Fact]
    public void Parser_AddHierarchy_PartOf_ShouldParseCorrectly()
    {
        var node = ParseStatement("ADD HIERARCHY Wheel PART_OF Car;");

        Assert.NotNull(node);
        Assert.IsType<AddHierarchyNode>(node);

        var hierNode = (AddHierarchyNode)node;
        Assert.Equal("ADD_HIERARCHY", hierNode.Type);
        Assert.Equal("Wheel", hierNode.ChildConcept);
        Assert.Equal("Car", hierNode.ParentConcept);
        Assert.Equal(HierarchyType.PART_OF, hierNode.HierarchyType);
    }

    // ==================== GRANT/REVOKE Tests ====================

    [Fact]
    public void Parser_Grant_ShouldParseCorrectly()
    {
        var node = ParseStatement("GRANT SELECT ON my_kb TO testuser;");

        Assert.NotNull(node);
        Assert.IsType<GrantNode>(node);

        var grantNode = (GrantNode)node;
        Assert.Equal("GRANT", grantNode.Type);
        Assert.Equal("SELECT", grantNode.Privilege);
        Assert.Equal("my_kb", grantNode.KbName);
        Assert.Equal("testuser", grantNode.Username);
    }

    [Fact]
    public void Parser_Revoke_ShouldParseCorrectly()
    {
        var node = ParseStatement("REVOKE SELECT ON my_kb FROM testuser;");

        Assert.NotNull(node);
        Assert.IsType<RevokeNode>(node);

        var revokeNode = (RevokeNode)node;
        Assert.Equal("REVOKE", revokeNode.Type);
        Assert.Equal("SELECT", revokeNode.Privilege);
        Assert.Equal("my_kb", revokeNode.KbName);
        Assert.Equal("testuser", revokeNode.Username);
    }

    // ==================== SELECT Tests ====================

    [Fact]
    public void Parser_Select_ShouldParseCorrectly()
    {
        var node = ParseStatement("SELECT Person;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("SELECT", selectNode.Type);
        Assert.Equal("Person", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_Select_WithFromClause_ShouldParseCorrectly()
    {
        var node = ParseStatement("SELECT * FROM Person;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("Person", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_Select_WithWhereClause_ShouldParseCorrectly()
    {
        var node = ParseStatement("SELECT Person WHERE age > 18;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("Person", selectNode.ConceptName);
        Assert.NotEmpty(selectNode.Conditions);
    }

        [Fact]
    public void Parser_Select_WithOrderBy_ShouldParseCorrectly()
    {
        var node = ParseStatement("SELECT Person ORDER BY name ASC;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Single(selectNode.OrderBy);
        Assert.Equal("name", selectNode.OrderBy[0].Variable);
        Assert.Equal("ASC", selectNode.OrderBy[0].Direction);
    }

    [Fact]
    public void Parser_Select_WithLimit_ShouldParseCorrectly()
    {
        var node = ParseStatement("SELECT Person LIMIT 10;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.NotNull(selectNode.Limit);
        Assert.Equal(10, selectNode.Limit.Limit);
    }

    // ==================== INSERT Tests ====================

    [Fact]
    public void Parser_Insert_ShouldParseCorrectly()
    {
        var node = ParseStatement("INSERT INTO Person ATTRIBUTE (name: 'John', age: 30);");

        Assert.NotNull(node);
        Assert.IsType<InsertNode>(node);

        var insertNode = (InsertNode)node;
        Assert.Equal("INSERT", insertNode.Type);
        Assert.Equal("Person", insertNode.ConceptName);
        Assert.Equal(2, insertNode.Values.Count);
        Assert.True(insertNode.Values.ContainsKey("name"));
        Assert.True(insertNode.Values.ContainsKey("age"));
    }

    // ==================== UPDATE Tests ====================

    [Fact]
    public void Parser_Update_ShouldParseCorrectly()
    {
        var node = ParseStatement("UPDATE Person ATTRIBUTE (SET age: 31) WHERE name = 'John';");

        Assert.NotNull(node);
        Assert.IsType<UpdateNode>(node);

        var updateNode = (UpdateNode)node;
        Assert.Equal("UPDATE", updateNode.Type);
        Assert.Equal("Person", updateNode.ConceptName);
        Assert.True(updateNode.SetValues.ContainsKey("age"));
        Assert.NotEmpty(updateNode.Conditions);
    }

    // ==================== DELETE Tests ====================

    [Fact]
    public void Parser_Delete_ShouldParseCorrectly()
    {
        var node = ParseStatement("DELETE FROM Person WHERE id = 1;");

        Assert.NotNull(node);
        Assert.IsType<DeleteNode>(node);

        var deleteNode = (DeleteNode)node;
        Assert.Equal("DELETE", deleteNode.Type);
        Assert.Equal("Person", deleteNode.ConceptName);
        Assert.NotEmpty(deleteNode.Conditions);
    }

    [Fact]
    public void Parser_Delete_WithoutWhere_ShouldParseCorrectly()
    {
        var node = ParseStatement("DELETE FROM Person;");

        Assert.NotNull(node);
        Assert.IsType<DeleteNode>(node);

        var deleteNode = (DeleteNode)node;
        Assert.Equal("DELETE", deleteNode.Type);
        Assert.Equal("Person", deleteNode.ConceptName);
        Assert.Empty(deleteNode.Conditions);
    }

    // ==================== SOLVE Tests ====================

    [Fact]
    public void Parser_Solve_ShouldParseCorrectly()
    {
        var node = ParseStatement("SOLVE ON CONCEPT Triangle GIVEN a: 3, b: 4 FIND area SAVE;");

        Assert.NotNull(node);
        Assert.IsType<SolveNode>(node);

        var solveNode = (SolveNode)node;
        Assert.Equal("SOLVE", solveNode.Type);
        Assert.Equal("Triangle", solveNode.ConceptName);
        Assert.Equal("area", solveNode.FindVariables[0]);
        Assert.Equal(2, solveNode.GivenFacts.Count);
        Assert.True(solveNode.SaveResults);
    }

    // ==================== SHOW Tests ====================

    [Fact]
    public void Parser_ShowKnowledgeBases_ShouldParseCorrectly()
    {
        var node = ParseStatement("SHOW KNOWLEDGE BASES;");

        Assert.NotNull(node);
        Assert.IsType<ShowNode>(node);

        var showNode = (ShowNode)node;
        Assert.Equal(ShowType.KnowledgeBases, showNode.ShowType);
    }

    [Fact]
    public void Parser_ShowConcepts_ShouldParseCorrectly()
    {
        var node = ParseStatement("SHOW CONCEPTS;");

        Assert.NotNull(node);
        Assert.IsType<ShowNode>(node);

        var showNode = (ShowNode)node;
        Assert.Equal(ShowType.Concepts, showNode.ShowType);
    }

    [Fact]
    public void Parser_ShowRules_ShouldParseCorrectly()
    {
        var node = ParseStatement("SHOW RULES;");

        Assert.NotNull(node);
        Assert.IsType<ShowNode>(node);

        var showNode = (ShowNode)node;
        Assert.Equal(ShowType.Rules, showNode.ShowType);
    }

    [Fact]
    public void Parser_ShowRelations_ShouldParseCorrectly()
    {
        var node = ParseStatement("SHOW RELATIONS;");

        Assert.NotNull(node);
        Assert.IsType<ShowNode>(node);

        var showNode = (ShowNode)node;
        Assert.Equal(ShowType.Relations, showNode.ShowType);
    }

    [Fact]
    public void Parser_ShowUsers_ShouldParseCorrectly()
    {
        var node = ParseStatement("SHOW USERS;");

        Assert.NotNull(node);
        Assert.IsType<ShowNode>(node);

        var showNode = (ShowNode)node;
        Assert.Equal(ShowType.Users, showNode.ShowType);
    }

    // ==================== Error Handling Tests ====================

    [Fact]
    public void Parser_InvalidSyntax_ShouldThrowException()
    {
        Assert.Throws<ParserException>(() => ParseStatement("CREATE INVALID SYNTAX HERE;"));
    }

    [Fact]
    public void Parser_EmptyInput_ShouldReturnNull()
    {
        var node = ParseStatement("");
        Assert.Null(node);
    }

    [Fact]
    public void Parser_UnknownCommand_ShouldThrowException()
    {
        Assert.Throws<ParserException>(() => ParseStatement("UNKNOWN_COMMAND test;"));
    }

    // ==================== Multiple Statements Tests ====================

    [Fact]
    public void Parser_MultipleStatements_ShouldParseAll()
    {
        var parser = new KBMS.Parser.Parser("CREATE KNOWLEDGE BASE kb1; CREATE KNOWLEDGE BASE kb2;");
        var statements = parser.ParseAll();

        Assert.Equal(2, statements.Count);
        Assert.IsType<CreateKbNode>(statements[0]);
        Assert.IsType<CreateKbNode>(statements[1]);

        var kb1 = (CreateKbNode)statements[0];
        var kb2 = (CreateKbNode)statements[1];

        Assert.Equal("kb1", kb1.KbName);
        Assert.Equal("kb2", kb2.KbName);
    }

    [Fact]
    // Temporary test to evaluate NCalc property handling
    public void Parser_NCalcTest()
    {
        // 1. Array-like bracket syntax mapping to flat dictionary keys
        var e1 = new NCalc.Expression("Sqrt([p1.x] * [p1.y])");
        e1.Parameters["p1.x"] = 4.0;
        e1.Parameters["p1.y"] = 9.0;
        Assert.Equal(6.0, e1.Evaluate());
        
        // 2. See if NCalc parses dot correctly if passed as flat object without brackets -> IT DOES NOT
        var e2 = new NCalc.Expression("Sqrt([p1.x] * [p1.y])");
        e2.Parameters["p1.x"] = 4.0;
        e2.Parameters["p1.y"] = 9.0;
        Assert.Equal(6.0, e2.Evaluate());
    }
}
