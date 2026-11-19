using System.Diagnostics;
using System.Text.Json;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>Runs the built CLI as a child process.</summary>
internal static class CliProcess
{
    public static string Samples(string name) => Path.Combine(AppContext.BaseDirectory, "samples", name);

    public static async Task<CliRun> RunAsync(IReadOnlyDictionary<string, string>? environment, params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "ToolpathOptimizer.Cli.dll"));
        foreach (string arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        foreach ((string key, string value) in environment ?? new Dictionary<string, string>())
        {
            start.Environment[key] = value;
        }

        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await process.WaitForExitAsync(timeout.Token);
        return new CliRun(process.ExitCode, await stdout, await stderr);
    }

    public static Task<CliRun> RunAsync(params string[] args) => RunAsync(null, args);
}

internal sealed class CliRun(int exitCode, string stdout, string stderr)
{
    public int ExitCode { get; } = exitCode;

    public string Stdout { get; } = stdout;

    public string Stderr { get; } = stderr;

    public JsonElement Envelope()
    {
        string[] lines = Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        return JsonDocument.Parse(lines[0]).RootElement.Clone();
    }

    public string FirstErrorCode() => Envelope().GetProperty("errors")[0].GetProperty("code").GetString()!;
}
