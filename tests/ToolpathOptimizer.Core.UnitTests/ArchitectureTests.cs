using System.Reflection;
using NetArchTest.Rules;
using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.UnitTests;

/// <summary>Enforces ADR-0001 (pure Core), ADR-0007 (independent validator) and D-014 (no records).</summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly _coreAssembly = typeof(PointId).Assembly;

    [Theory]
    [InlineData("ToolpathOptimizer.Application")]
    [InlineData("ToolpathOptimizer.Infrastructure")]
    [InlineData("Microsoft.Extensions")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("System.IO")]
    [InlineData("System.Net")]
    public void CoreTypes_ForbiddenNamespace_AreNotReferenced(string forbidden)
    {
        TestResult result = Types.InAssembly(_coreAssembly).ShouldNot().HaveDependencyOn(forbidden).GetResult();

        Assert.True(result.IsSuccessful, "Types depending on " + forbidden + ": " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory]
    [InlineData("ToolpathOptimizer.Core.Optimisation")]
    [InlineData("ToolpathOptimizer.Core.Validation")]
    public void VerificationTypes_OptimiserNamespace_AreNotReferenced(string forbidden)
    {
        TestResult result = Types.InAssembly(_coreAssembly)
            .That().ResideInNamespace("ToolpathOptimizer.Core.Verification")
            .ShouldNot().HaveDependencyOn(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, "Verification types depending on " + forbidden + ": " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void CoreTypes_AnyType_IsNotARecord()
    {
        Type[] records = _coreAssembly.GetTypes().Where(type => type.GetMethod("<Clone>$") is not null).ToArray();

        Assert.Empty(records);
    }
}
