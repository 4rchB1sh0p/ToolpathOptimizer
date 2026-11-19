using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Cli.CommandLine;

namespace ToolpathOptimizer.Cli.IntegrationTests.CommandLine;

public sealed class CliArgumentParserTests
{
    [Fact]
    public void Parse_AcceptsSpaceAndEqualsForms()
    {
        Result<CliArguments> result = CliArgumentParser.Parse(["analyze", "--points=a.csv", "--job", "b.json", "--output", "c.json", "--overwrite"]);

        Assert.True(result.IsSuccess);
        Assert.Equal("a.csv", result.Value.PointsPath);
        Assert.True(result.Value.Overwrite);
    }

    [Theory]
    [InlineData(ErrorCodes.ArgsDuplicateOption, "validate", "--points", "a", "--points", "b", "--job", "c")]
    [InlineData(ErrorCodes.ArgsMissingValue, "validate", "--points", "--job", "c")]
    [InlineData(ErrorCodes.ArgsUnknownOption, "validate", "--points", "a", "--job", "c", "--overwrite")]
    [InlineData(ErrorCodes.ArgsUnknownOption, "validate", "stray")]
    public void Parse_RejectsMalformedArguments(string expectedCode, params string[] args)
    {
        Assert.Equal(expectedCode, CliArgumentParser.Parse(args).Outcome.Errors[0].Code);
    }
}
