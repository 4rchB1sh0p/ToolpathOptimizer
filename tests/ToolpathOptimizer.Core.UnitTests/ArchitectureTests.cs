using NetArchTest.Rules;
using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.UnitTests;

/// <summary>Enforces ADR-0001 (pure Core) and ADR-0007 (independent validator).</summary>
public sealed class ArchitectureTests
{
    private static readonly System.Reflection.Assembly s_core = typeof(PointId).Assembly;

    [Theory]
    [InlineData("ToolpathOptimizer.Application")]
    [InlineData("ToolpathOptimizer.Infrastructure")]
    [InlineData("Microsoft.Extensions")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("System.IO")]
    [InlineData("System.Net")]
    public void Core_HasNoOuterOrIoDependency(string forbidden)
    {
        TestResult result = Types.InAssembly(s_core).ShouldNot().HaveDependencyOn(forbidden).GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory]
    [InlineData("ToolpathOptimizer.Core.Optimisation")]
    [InlineData("ToolpathOptimizer.Core.Validation")]
    public void Verification_DoesNotDependOnOptimiserState(string forbidden)
    {
        TestResult result = Types.InAssembly(s_core)
            .That().ResideInNamespace("ToolpathOptimizer.Core.Verification")
            .ShouldNot().HaveDependencyOn(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Core_DeclaresNoRecordTypes()
    {
        Type[] records = s_core.GetTypes().Where(t => t.GetMethod("<Clone>$") is not null).ToArray();

        Assert.Empty(records);
    }
}
