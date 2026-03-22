using KBMS.Parser.Ast.Kql;
using Xunit;
using KBMS.Parser;
using KBMS.Parser.Ast;

namespace KBMS.Tests;

public class SelectEntityTests
{
    private AstNode? ParseStatement(string input)
    {
        var parser = new KBMS.Parser.Parser(input);
        return parser.Parse();
    }

    [Fact]
    public void Parser_SelectFromRelation_ShouldHaveCorrectTargetType()
    {
        var node = ParseStatement("SELECT * FROM RELATION Likes;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("RELATION", selectNode.TargetType);
        Assert.Equal("Likes", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_SelectFromRule_ShouldHaveCorrectTargetType()
    {
        var node = ParseStatement("SELECT * FROM RULE MyRule;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("RULE", selectNode.TargetType);
        Assert.Equal("MyRule", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_SelectFromHierarchy_ShouldHaveCorrectTargetType()
    {
        var node = ParseStatement("SELECT * FROM HIERARCHY AnimalTree;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("HIERARCHY", selectNode.TargetType);
        Assert.Equal("AnimalTree", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_SelectFromSystemConcepts_ShouldWork()
    {
        var node = ParseStatement("SELECT * FROM system.concepts;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("CONCEPT", selectNode.TargetType);
        Assert.Equal("system.concepts", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_SelectFromSystemRelations_ShouldWork()
    {
        var node = ParseStatement("SELECT * FROM system.relations;");

        Assert.NotNull(node);
        var selectNode = (SelectNode)node;
        Assert.Equal("system.relations", selectNode.ConceptName);
    }

    [Fact]
    public void Parser_SelectFromConceptShorthand_ShouldWork()
    {
        var node = ParseStatement("SELECT * FROM Person;");

        Assert.NotNull(node);
        Assert.IsType<SelectNode>(node);

        var selectNode = (SelectNode)node;
        Assert.Equal("CONCEPT", selectNode.TargetType);
        Assert.Equal("Person", selectNode.ConceptName);
    }
}
