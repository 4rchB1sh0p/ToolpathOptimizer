using System.Text;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

public sealed class JobManifestReaderTests
{
    private static Result<JobManifest> Read(string json, AnalysisLimitsOptions? limits = null) =>
        JobManifestReader.Read(Encoding.UTF8.GetBytes(json), "job.json", limits ?? new AnalysisLimitsOptions());

    [Fact]
    public void Read_ParsesSampleAndAppliesDefaults()
    {
        Result<JobManifest> result = Read("""{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[],"locked_groups":[]},"algorithm":{"name":"nearest-neighbor-2opt","seed":0}}""");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.EndXMm);
        Assert.Equal(100, result.Value.Parameters.MaxPasses);
        Assert.Equal(0.001, result.Value.Parameters.MinImprovementMm);
    }

    [Fact]
    public void Read_ParsesConstraintsEndAndBudgets()
    {
        const string extra = ",\"job_id\":\"J-1\",\"end_x_mm\":5,\"end_y_mm\":6,"
            + "\"constraints\":{\"precedence\":[{\"before\":\"A\",\"after\":\"B\"}],\"locked_groups\":[\"G1\"],\"allowed_transitions\":[]}";
        Result<JobManifest> result = Read(Inputs.Manifest(extra)
            .Replace("\"algorithm\":{\"name\":\"nearest-neighbor-2opt\"}", "\"algorithm\":{\"name\":\"nearest-neighbor-2opt\",\"max_passes\":0,\"max_move_evaluations\":10,\"min_improvement_mm\":0.5}", StringComparison.Ordinal));

        Assert.True(result.IsSuccess, result.IsSuccess ? string.Empty : result.Outcome.Errors[0].Message);
        Assert.Equal("J-1", result.Value.JobId);
        Assert.Equal(6, result.Value.EndYMm);
        Assert.Equal("B", Assert.Single(result.Value.Precedence).After);
        Assert.Equal(0, result.Value.Parameters.MaxPasses);
        Assert.Equal(0.5, result.Value.Parameters.MinImprovementMm);
    }

    [Theory]
    [InlineData(ErrorCodes.InputMalformedJson, "{\"schema_version\":\"1.0\",}")]
    [InlineData(ErrorCodes.InputMalformedJson, "{/*c*/\"schema_version\":\"1.0\"}")]
    [InlineData(ErrorCodes.InputMalformedJson, "not json")]
    [InlineData(ErrorCodes.InputInvalidType, "[]")]
    [InlineData(ErrorCodes.InputMissingField, "{}")]
    [InlineData(ErrorCodes.InputUnsupportedSchemaVersion, "{\"schema_version\":\"1.1\"}")]
    [InlineData(ErrorCodes.InputDuplicateField, "{\"schema_version\":\"1.0\",\"schema_version\":\"1.0\"}")]
    public void Read_RejectsStructuralViolation(string expectedCode, string json)
    {
        Assert.Equal(expectedCode, Read(json).Outcome.Errors[0].Code);
    }

    [Theory]
    [InlineData(ErrorCodes.InputUnknownField, ",\"extra\":1")]
    [InlineData(ErrorCodes.InputPartialEnd, ",\"end_x_mm\":1")]
    [InlineData(ErrorCodes.InputInvalidIdentifier, ",\"job_id\":\"=bad\"")]
    [InlineData(ErrorCodes.InputInvalidType, ",\"end_x_mm\":\"1\",\"end_y_mm\":2")]
    [InlineData(ErrorCodes.InputLimitExceeded, ",\"end_x_mm\":20000,\"end_y_mm\":2")]
    [InlineData(ErrorCodes.InputUnsupportedConstraint, ",\"constraints\":{\"allowed_transitions\":[{\"from\":\"A\",\"to\":\"B\"}]}")]
    [InlineData(ErrorCodes.InputUnknownField, ",\"constraints\":{\"precedence\":[{\"before\":\"A\",\"after\":\"B\",\"x\":1}]}")]
    [InlineData(ErrorCodes.InputMissingField, ",\"constraints\":{\"precedence\":[{\"before\":\"A\"}]}")]
    [InlineData(ErrorCodes.InputInvalidIdentifier, ",\"constraints\":{\"locked_groups\":[\"+G\"]}")]
    public void Read_RejectsFieldViolation(string expectedCode, string extra)
    {
        Assert.Equal(expectedCode, Read(Inputs.Manifest(extra)).Outcome.Errors[0].Code);
    }

    [Theory]
    [InlineData("\"name\":\"genetic\"")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"seed\":1")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":-1")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":1.5")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":10001")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"min_improvement_mm\":-0.1")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"min_improvement_mm\":1e400")]
    public void Read_RejectsInvalidAlgorithmParameter(string algorithm)
    {
        string json = Inputs.Manifest(string.Empty).Replace("\"name\":\"nearest-neighbor-2opt\"", algorithm, StringComparison.Ordinal);

        Assert.Equal(ErrorCodes.InputInvalidParameter, Read(json).Outcome.Errors[0].Code);
    }

    [Theory]
    [InlineData(ErrorCodes.InputUnsupportedUnit, "\"units\":\"mm\"", "\"units\":\"inch\"")]
    [InlineData(ErrorCodes.InputUnsupportedCoordinateSystem, "\"coordinate_system\":\"XY_CARTESIAN\"", "\"coordinate_system\":\"POLAR\"")]
    [InlineData(ErrorCodes.InputMissingStart, "\"start_x_mm\":0,", "")]
    public void Read_RejectsUnsupportedSetting(string expectedCode, string original, string replacement)
    {
        string json = Inputs.Manifest(string.Empty).Replace(original, replacement, StringComparison.Ordinal);

        Assert.Equal(expectedCode, Read(json).Outcome.Errors[0].Code);
    }

    [Fact]
    public void Read_RejectsDepthOver16()
    {
        string nested = string.Concat(Enumerable.Repeat("[", 20)) + string.Concat(Enumerable.Repeat("]", 20));

        Assert.Equal(ErrorCodes.InputMalformedJson, Read(Inputs.Manifest(",\"extra\":" + nested)).Outcome.Errors[0].Code);
    }

    [Fact]
    public void Read_RejectsTooManyPrecedenceEdges()
    {
        var limits = new AnalysisLimitsOptions { MaxPrecedenceEdges = 1 };
        string json = Inputs.Manifest(",\"constraints\":{\"precedence\":[{\"before\":\"A\",\"after\":\"B\"},{\"before\":\"B\",\"after\":\"C\"}]}");

        Assert.Equal(ErrorCodes.InputLimitExceeded, Read(json, limits).Outcome.Errors[0].Code);
    }
}
