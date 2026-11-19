using System.Text;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

/// <summary>Tests for <see cref="JobManifestReader"/>: strict JSON rules and per-field codes.</summary>
public sealed class JobManifestReaderTests
{
    private const string DefaultAlgorithm = "\"algorithm\":{\"name\":\"nearest-neighbor-2opt\"}";

    private static Result<JobManifest> Read(string json) => Read(json, new AnalysisLimitsOptions());

    private static Result<JobManifest> Read(string json, AnalysisLimitsOptions limits) =>
        JobManifestReader.Read(Encoding.UTF8.GetBytes(json), "job.json", limits);

    private static string FirstCode(Result<JobManifest> result) => result.IsSuccess ? "(success)" : result.Outcome.Errors[0].Code;

    private static string ManifestWithAlgorithm(string algorithmBody) =>
        TestInputs.MinimalManifest.Replace(DefaultAlgorithm, "\"algorithm\":{" + algorithmBody + "}", StringComparison.Ordinal);

    [Fact]
    public void Read_SampleManifest_AppliesDefaultBudgets()
    {
        Result<JobManifest> result = Read("""{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[],"locked_groups":[]},"algorithm":{"name":"nearest-neighbor-2opt","seed":0}}""");

        Assert.Equal((100L, 0.001), (result.Value.Parameters.MaxPasses, result.Value.Parameters.MinImprovementMm));
    }

    [Fact]
    public void Read_SampleManifest_HasNoEnd()
    {
        Assert.Null(Read(TestInputs.MinimalManifest).Value.EndXMm);
    }

    [Fact]
    public void Read_FullManifest_ReadsJobIdAndEnd()
    {
        JobManifest manifest = Read(TestInputs.ManifestWith(",\"job_id\":\"J-1\",\"end_x_mm\":5,\"end_y_mm\":6")).Value;

        Assert.Equal(("J-1", 5.0, 6.0), (manifest.JobId, manifest.EndXMm!.Value, manifest.EndYMm!.Value));
    }

    [Fact]
    public void Read_FullManifest_ReadsConstraints()
    {
        string json = TestInputs.ManifestWith(",\"constraints\":{\"precedence\":[{\"before\":\"A\",\"after\":\"B\"}],\"locked_groups\":[\"G1\"],\"allowed_transitions\":[]}");

        JobManifest manifest = Read(json).Value;

        Assert.Equal(("B", "G1"), (Assert.Single(manifest.Precedence).After, Assert.Single(manifest.LockedGroups)));
    }

    [Fact]
    public void Read_ExplicitBudgets_OverrideDefaults()
    {
        AlgorithmParameters parameters = Read(ManifestWithAlgorithm("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":0,\"max_move_evaluations\":10,\"min_improvement_mm\":0.5")).Value.Parameters;

        Assert.Equal((0L, 10L, 0.5), (parameters.MaxPasses, parameters.MaxMoveEvaluations, parameters.MinImprovementMm));
    }

    [Theory]
    [InlineData(ErrorCodes.InputMalformedJson, "{\"schema_version\":\"1.0\",}")]
    [InlineData(ErrorCodes.InputMalformedJson, "{/*c*/\"schema_version\":\"1.0\"}")]
    [InlineData(ErrorCodes.InputMalformedJson, "not json")]
    [InlineData(ErrorCodes.InputInvalidType, "[]")]
    [InlineData(ErrorCodes.InputMissingField, "{}")]
    [InlineData(ErrorCodes.InputUnsupportedSchemaVersion, "{\"schema_version\":\"1.1\"}")]
    [InlineData(ErrorCodes.InputDuplicateField, "{\"schema_version\":\"1.0\",\"schema_version\":\"1.0\"}")]
    public void Read_StructuralViolation_ReturnsStableCode(string expectedCode, string json)
    {
        Assert.Equal(expectedCode, FirstCode(Read(json)));
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
    public void Read_FieldViolation_ReturnsStableCode(string expectedCode, string extraFields)
    {
        Assert.Equal(expectedCode, FirstCode(Read(TestInputs.ManifestWith(extraFields))));
    }

    [Theory]
    [InlineData("\"name\":\"genetic\"")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"seed\":1")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":-1")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":1.5")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"max_passes\":10001")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"min_improvement_mm\":-0.1")]
    [InlineData("\"name\":\"nearest-neighbor-2opt\",\"min_improvement_mm\":1e400")]
    public void Read_InvalidAlgorithmParameter_ReturnsInvalidParameter(string algorithmBody)
    {
        Assert.Equal(ErrorCodes.InputInvalidParameter, FirstCode(Read(ManifestWithAlgorithm(algorithmBody))));
    }

    [Theory]
    [InlineData(ErrorCodes.InputUnsupportedUnit, "\"units\":\"mm\"", "\"units\":\"inch\"")]
    [InlineData(ErrorCodes.InputUnsupportedCoordinateSystem, "\"coordinate_system\":\"XY_CARTESIAN\"", "\"coordinate_system\":\"POLAR\"")]
    [InlineData(ErrorCodes.InputMissingStart, "\"start_x_mm\":0,", "")]
    [InlineData(ErrorCodes.InputMissingField, "," + DefaultAlgorithm, "")]
    public void Read_UnsupportedOrMissingSetting_ReturnsStableCode(string expectedCode, string original, string replacement)
    {
        Assert.Equal(expectedCode, FirstCode(Read(TestInputs.MinimalManifest.Replace(original, replacement, StringComparison.Ordinal))));
    }

    [Fact]
    public void Read_DepthOver16_ReturnsMalformedJson()
    {
        string nested = string.Concat(Enumerable.Repeat("[", 20)) + string.Concat(Enumerable.Repeat("]", 20));

        Assert.Equal(ErrorCodes.InputMalformedJson, FirstCode(Read(TestInputs.ManifestWith(",\"extra\":" + nested))));
    }

    [Fact]
    public void Read_TooManyPrecedenceEdges_ReturnsLimitExceeded()
    {
        string json = TestInputs.ManifestWith(",\"constraints\":{\"precedence\":[{\"before\":\"A\",\"after\":\"B\"},{\"before\":\"B\",\"after\":\"C\"}]}");

        Assert.Equal(ErrorCodes.InputLimitExceeded, FirstCode(Read(json, new AnalysisLimitsOptions { MaxPrecedenceEdges = 1 })));
    }
}
