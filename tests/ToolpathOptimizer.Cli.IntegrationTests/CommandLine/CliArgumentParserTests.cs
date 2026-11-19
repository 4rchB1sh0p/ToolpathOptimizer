using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Cli.CommandLine;

namespace ToolpathOptimizer.Cli.IntegrationTests.CommandLine;

/// <summary>Tests for <see cref="CliArgumentParser"/>.</summary>
public sealed class CliArgumentParserTests
{
    private static CliArguments ParseAnalyze(params string[] extra) =>
        CliArgumentParser.Parse(["analyze", "--points=a.csv", "--job", "b.json", "--output", "c.json", .. extra]).Value;

    [Fact]
    public void Parse_EqualsForm_ReadsValue()
    {
        Assert.Equal("a.csv", ParseAnalyze().PointsPath);
    }

    [Fact]
    public void Parse_SpaceForm_ReadsValue()
    {
        Assert.Equal("b.json", ParseAnalyze().JobPath);
    }

    [Fact]
    public void Parse_OverwriteFlag_SetsOverwriteMode()
    {
        Assert.Equal(ReportWriteMode.Overwrite, ParseAnalyze("--overwrite").WriteMode);
    }

    [Fact]
    public void Parse_NoOverwriteFlag_UsesCreateNewMode()
    {
        Assert.Equal(ReportWriteMode.CreateNew, ParseAnalyze().WriteMode);
    }

    [Theory]
    [InlineData(ErrorCodes.ArgsDuplicateOption, "validate", "--points", "a", "--points", "b", "--job", "c")]
    [InlineData(ErrorCodes.ArgsDuplicateOption, "analyze", "--points", "a", "--job", "b", "--output", "c", "--overwrite", "--overwrite")]
    [InlineData(ErrorCodes.ArgsMissingValue, "validate", "--points", "--job", "c")]
    [InlineData(ErrorCodes.ArgsMissingValue, "validate", "--points=", "--job", "c")]
    [InlineData(ErrorCodes.ArgsUnknownOption, "validate", "--points", "a", "--job", "c", "--overwrite")]
    [InlineData(ErrorCodes.ArgsUnknownOption, "validate", "stray")]
    public void Parse_MalformedArguments_ReturnsStableCode(string expectedCode, params string[] args)
    {
        Assert.Equal(expectedCode, CliArgumentParser.Parse(args).Outcome.Errors[0].Code);
    }
}
