using Xunit;
using System.Collections.Generic;
using KBMS.Reasoning;
using KBMS.Reasoning.Rete;

namespace KBMS.Tests;

public class ExpressionCompilerTests
{
    [Fact]
    public void CompileCondition_ShouldCompileNativeAndExecute()
    {
        // 1. Arrange
        var engine = new InferenceEngine();
        
        // "sys" is a number, "category" is a string
        string expr = "sys > 140 && category == \"Admin\"";

        var facts = new[] {
            new Fact("sys", 150.0),
            new Fact("category", "Admin")
        };
        var token = new Token(facts);

        // 2. Act
        var compiledDelegate = ExpressionCompiler.CompileCondition(expr, engine);
        bool result = compiledDelegate(token);

        // 3. Assert
        Assert.True(result, "Native C# compiled expression should evaluate to true.");
    }

    [Fact]
    public void CompileCondition_ShouldCompileNativeAndFailOnFalse()
    {
        // 1. Arrange
        var engine = new InferenceEngine();
        string expr = "sys > 140 && category == \"User\"";

        var facts = new[] {
            new Fact("sys", 150.0),
            new Fact("category", "Admin")
        };
        var token = new Token(facts);

        // 2. Act
        var compiledDelegate = ExpressionCompiler.CompileCondition(expr, engine);
        bool result = compiledDelegate(token);

        // 3. Assert
        Assert.False(result, "Native C# compiled expression should evaluate to false.");
    }

    [Fact]
    public void CompileCondition_ShouldFallbackToNCalcOnComplexMath()
    {
        // 1. Arrange
        var engine = new InferenceEngine();
        
        // Pow is not supported by our simple native expression builder, so it should fallback to NCalc
        string expr = "Pow(sys, 2) > 20000";

        var facts = new[] {
            new Fact("sys", 150.0) // 150^2 = 22500 > 20000 -> true
        };
        var token = new Token(facts);

        // 2. Act
        var compiledDelegate = ExpressionCompiler.CompileCondition(expr, engine);
        bool result = compiledDelegate(token);

        // 3. Assert
        Assert.True(result, "Fallback to NCalc should evaluate complex math functions correctly.");
    }
}
