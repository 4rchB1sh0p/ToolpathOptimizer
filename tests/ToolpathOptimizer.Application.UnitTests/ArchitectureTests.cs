using NetArchTest.Rules;
using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.UnitTests;

/// <summary>Enforces the dependency table in solution-structure.md for the Application layer.</summary>
public sealed class ArchitectureTests
{
    [Theory]
    [InlineData("ToolpathOptimizer.Infrastructure")]
    [InlineData("ToolpathOptimizer.Cli")]
    [InlineData("ToolpathOptimizer.Web")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.Extensions.Hosting")]
    public void Application_DoesNotDependOnOuterLayers(string forbidden)
    {
        TestResult result = Types.InAssembly(typeof(OperationOutcome).Assembly).ShouldNot().HaveDependencyOn(forbidden).GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
