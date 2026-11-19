using System.Text.Json;
using ToolpathOptimizer.Cli.IntegrationTests.Support;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>Child-process tests: each failure class maps to its stable exit code and error code (ADR-0011).</summary>
[Trait("Category", "Process")]
public sealed class ExitCodeProcessTests : IDisposable
{
    private const string CycleManifest =
        """{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[{"before":"H01","after":"H02"},{"before":"H02","after":"H01"}]},"algorithm":{"name":"nearest-neighbor-2opt"}}""";

    private readonly OutputDirectory _output = new();

    /// <inheritdoc />
    public void Dispose() => _output.Dispose();

    [Fact]
    public async Task Validate_ValidSample_ExitsZeroWithoutReportPath()
    {
        CliRun run = await CliProcess.RunAsync("validate", "--points", CliProcess.SamplePath("holes.csv"), "--job", CliProcess.SamplePath("job.json"));

        Assert.Equal((0, JsonValueKind.Null), (run.ExitCode, run.ReadEnvelope().GetProperty("report_path").ValueKind));
    }

    [Theory]
    [InlineData("ARGS_MISSING_COMMAND")]
    [InlineData("ARGS_UNKNOWN_COMMAND", "optimise")]
    [InlineData("ARGS_MISSING_OPTION", "analyze", "--points", "a.csv")]
    [InlineData("ARGS_UNKNOWN_OPTION", "validate", "--points", "a.csv", "--job", "b.json", "--gcode")]
    [InlineData("ARGS_INVALID_VALUE", "analyze", "--points", "a", "--job", "b", "--output", "c", "--format", "xml")]
    public async Task Run_InvalidArguments_ExitsTwo(string expectedCode, params string[] args)
    {
        CliRun run = await CliProcess.RunAsync(args);

        Assert.Equal((2, expectedCode), (run.ExitCode, run.ReadFirstErrorCode()));
    }

    [Fact]
    public async Task Validate_NonFiniteCoordinate_ExitsThree()
    {
        string csv = _output.WriteFile("bad.csv", "point_id,x_mm,y_mm,sequence,operation_group\nA,NaN,0,1,\n");

        CliRun run = await CliProcess.RunAsync("validate", "--points", csv, "--job", CliProcess.SamplePath("job.json"));

        Assert.Equal((3, "INPUT_NON_FINITE_COORDINATE"), (run.ExitCode, run.ReadFirstErrorCode()));
    }

    [Fact]
    public async Task Analyze_CyclicConstraints_ExitsFour()
    {
        string job = _output.WriteFile("cycle.json", CycleManifest);

        CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.SamplePath("holes.csv"), "--job", job, "--output", _output.PathOf("r.json"));

        Assert.Equal((4, "CONSTRAINT_CYCLE"), (run.ExitCode, run.ReadFirstErrorCode()));
    }

    [Fact]
    public async Task Analyze_CyclicConstraints_WritesNoReport()
    {
        string job = _output.WriteFile("cycle.json", CycleManifest);

        await CliProcess.RunAsync("analyze", "--points", CliProcess.SamplePath("holes.csv"), "--job", job, "--output", _output.PathOf("r.json"));

        Assert.False(File.Exists(_output.PathOf("r.json")), "No report may be written for infeasible constraints.");
    }

    [Fact]
    public async Task Analyze_ExistingOutputWithoutOverwrite_ExitsSevenAndKeepsFile()
    {
        string existing = _output.WriteFile("r.json", "keep");

        CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.SamplePath("holes.csv"), "--job", CliProcess.SamplePath("job.json"), "--output", existing);

        Assert.Equal((7, "IO_OUTPUT_EXISTS", "keep"), (run.ExitCode, run.ReadFirstErrorCode(), File.ReadAllText(existing)));
    }

    [Fact]
    public async Task Validate_MissingInputFile_ExitsSeven()
    {
        CliRun run = await CliProcess.RunAsync("validate", "--points", _output.PathOf("missing.csv"), "--job", CliProcess.SamplePath("job.json"));

        Assert.Equal((7, "IO_READ_FAILED"), (run.ExitCode, run.ReadFirstErrorCode()));
    }

    [Fact]
    public async Task Analyze_OutputEqualsInputWithOverwrite_ExitsTwoAndKeepsInput()
    {
        string csv = _output.WriteFile("in.csv", File.ReadAllText(CliProcess.SamplePath("holes.csv")));
        string before = File.ReadAllText(csv);

        CliRun run = await CliProcess.RunAsync("analyze", "--points", csv, "--job", CliProcess.SamplePath("job.json"), "--output", csv, "--overwrite");

        Assert.Equal((2, "ARGS_OUTPUT_IS_INPUT", before), (run.ExitCode, run.ReadFirstErrorCode(), File.ReadAllText(csv)));
    }

    [Fact]
    public async Task Validate_InvalidConfiguration_ExitsTwo()
    {
        var environment = new Dictionary<string, string> { ["TOOLPATH_AnalysisLimits__Deadline"] = "00:00:00" };

        CliRun run = await CliProcess.RunWithEnvironmentAsync(environment, "validate", "--points", CliProcess.SamplePath("holes.csv"), "--job", CliProcess.SamplePath("job.json"));

        Assert.Equal((2, "CONFIG_INVALID"), (run.ExitCode, run.ReadFirstErrorCode()));
    }

    [Fact]
    public async Task Version_AnyEnvironment_PrintsAlgorithmName()
    {
        CliRun run = await CliProcess.RunAsync("version");

        Assert.Equal((0, "nearest-neighbor-2opt"), (run.ExitCode, run.ReadEnvelope().GetProperty("algorithm_name").GetString()));
    }
}
