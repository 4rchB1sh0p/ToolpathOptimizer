using System.Text.Json;
using System.Text.RegularExpressions;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>Child-process tests: exit codes, stdout envelope, stderr separation and golden reports.</summary>
public sealed partial class CliProcessTests : IDisposable
{
    private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "tpo-cli-tests", Guid.NewGuid().ToString("N"));

    public CliProcessTests()
    {
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }

    private string Output(string name) => Path.Combine(_outputDirectory, name);

    private string WriteInput(string name, string content)
    {
        string path = Output(name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task Analyze_Succeeds_WritesReportAndSingleEnvelope()
    {
        CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.Samples("plate.csv"), "--job", CliProcess.Samples("plate-job.json"), "--output", Output("report.json"));

        Assert.Equal(0, run.ExitCode);
        JsonElement envelope = run.Envelope();
        Assert.Equal("ok", envelope.GetProperty("status").GetString());
        Assert.Equal(Output("report.json"), envelope.GetProperty("report_path").GetString());
        Assert.DoesNotContain("schema_version", run.Stderr, StringComparison.Ordinal);
        using JsonDocument report = JsonDocument.Parse(await File.ReadAllBytesAsync(Output("report.json")));
        Assert.Equal("candidate", report.RootElement.GetProperty("selected_route").GetString());
        Assert.Single(Directory.GetFiles(_outputDirectory));
    }

    [Fact]
    public async Task Validate_ValidInput_ExitsZeroWithoutReport()
    {
        CliRun run = await CliProcess.RunAsync("validate", "--points", CliProcess.Samples("holes.csv"), "--job", CliProcess.Samples("job.json"));

        Assert.Equal(0, run.ExitCode);
        Assert.Equal(JsonValueKind.Null, run.Envelope().GetProperty("report_path").ValueKind);
    }

    [Theory]
    [InlineData("ARGS_MISSING_COMMAND")]
    [InlineData("ARGS_UNKNOWN_COMMAND", "optimise")]
    [InlineData("ARGS_MISSING_OPTION", "analyze", "--points", "a.csv")]
    [InlineData("ARGS_UNKNOWN_OPTION", "validate", "--points", "a.csv", "--job", "b.json", "--gcode")]
    [InlineData("ARGS_INVALID_VALUE", "analyze", "--points", "a", "--job", "b", "--output", "c", "--format", "xml")]
    public async Task InvalidArguments_ExitTwo(string expectedCode, params string[] args)
    {
        CliRun run = await CliProcess.RunAsync(args);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal(expectedCode, run.FirstErrorCode());
    }

    [Fact]
    public async Task InvalidInput_ExitsThree()
    {
        string csv = WriteInput("bad.csv", "point_id,x_mm,y_mm,sequence,operation_group\nA,NaN,0,1,\n");

        CliRun run = await CliProcess.RunAsync("validate", "--points", csv, "--job", CliProcess.Samples("job.json"));

        Assert.Equal(3, run.ExitCode);
        Assert.Equal("INPUT_NON_FINITE_COORDINATE", run.FirstErrorCode());
    }

    [Fact]
    public async Task CyclicConstraints_ExitFour()
    {
        string job = WriteInput("cycle.json", """{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[{"before":"H01","after":"H02"},{"before":"H02","after":"H01"}]},"algorithm":{"name":"nearest-neighbor-2opt"}}""");

        CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.Samples("holes.csv"), "--job", job, "--output", Output("r.json"));

        Assert.Equal(4, run.ExitCode);
        Assert.Equal("CONSTRAINT_CYCLE", run.FirstErrorCode());
        Assert.False(File.Exists(Output("r.json")));
    }

    [Fact]
    public async Task ExistingOutput_ExitsSevenAndKeepsFile()
    {
        string existing = WriteInput("r.json", "keep");

        CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.Samples("holes.csv"), "--job", CliProcess.Samples("job.json"), "--output", existing);

        Assert.Equal(7, run.ExitCode);
        Assert.Equal("IO_OUTPUT_EXISTS", run.FirstErrorCode());
        Assert.Equal("keep", await File.ReadAllTextAsync(existing));
    }

    [Fact]
    public async Task MissingInputFile_ExitsSeven()
    {
        CliRun run = await CliProcess.RunAsync("validate", "--points", Output("missing.csv"), "--job", CliProcess.Samples("job.json"));

        Assert.Equal(7, run.ExitCode);
        Assert.Equal("IO_READ_FAILED", run.FirstErrorCode());
    }

    [Fact]
    public async Task OutputEqualsInput_IsRejectedEvenWithOverwrite()
    {
        string csv = WriteInput("in.csv", await File.ReadAllTextAsync(CliProcess.Samples("holes.csv")));
        string before = await File.ReadAllTextAsync(csv);

        CliRun run = await CliProcess.RunAsync("analyze", "--points", csv, "--job", CliProcess.Samples("job.json"), "--output", csv, "--overwrite");

        Assert.Equal(2, run.ExitCode);
        Assert.Equal("ARGS_OUTPUT_IS_INPUT", run.FirstErrorCode());
        Assert.Equal(before, await File.ReadAllTextAsync(csv));
    }

    [Fact]
    public async Task InvalidConfiguration_ExitsTwo()
    {
        var environment = new Dictionary<string, string> { ["TOOLPATH_AnalysisLimits__Deadline"] = "00:00:00" };

        CliRun run = await CliProcess.RunAsync(environment, "validate", "--points", CliProcess.Samples("holes.csv"), "--job", CliProcess.Samples("job.json"));

        Assert.Equal(2, run.ExitCode);
        Assert.Equal("CONFIG_INVALID", run.FirstErrorCode());
    }

    [Fact]
    public async Task Version_PrintsVersionsAsJson()
    {
        CliRun run = await CliProcess.RunAsync("version");

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("nearest-neighbor-2opt", run.Envelope().GetProperty("algorithm_name").GetString());
    }

    [Theory]
    [InlineData("holes.csv", "job.json", "holes.report.json")]
    [InlineData("plate.csv", "plate-job.json", "plate.report.json")]
    public async Task Analyze_MatchesGoldenReportTwice(string points, string job, string golden)
    {
        string expected = Normalise(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Golden", golden)));

        for (int attempt = 0; attempt < 2; attempt++)
        {
            string output = Output($"{attempt}-{golden}");
            CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.Samples(points), "--job", CliProcess.Samples(job), "--output", output);

            Assert.Equal(0, run.ExitCode);
            Assert.Equal(expected, Normalise(await File.ReadAllTextAsync(output)));
        }
    }

    /// <summary>Removes the fields excluded from determinism comparisons.</summary>
    private static string Normalise(string report) =>
        VolatileFields().Replace(report.ReplaceLineEndings("\n"), "\"$1\": \"<excluded>\"");

    [GeneratedRegex("\"(generated_at_utc|tool_version)\": \"[^\"]*\"")]
    private static partial Regex VolatileFields();
}
