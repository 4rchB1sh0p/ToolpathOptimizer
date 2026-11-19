using System.Diagnostics;

namespace ToolpathOptimizer.Cli.IntegrationTests.Support;

/// <summary>Runs the built CLI as a child process (real exit codes, stdout and stderr).</summary>
internal static class CliProcess
{
    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(2);

    /// <summary>Gets the path of a synthetic sample copied next to the test assembly.</summary>
    /// <param name="name">Sample file name.</param>
    /// <returns>The full path.</returns>
    public static string SamplePath(string name) => Path.Combine(AppContext.BaseDirectory, "samples", name);

    /// <summary>Runs the CLI with the inherited environment.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The captured run.</returns>
    public static Task<CliRun> RunAsync(params string[] args) => RunWithEnvironmentAsync(new Dictionary<string, string>(), args);

    /// <summary>Runs the CLI with extra environment variables.</summary>
    /// <param name="environment">Variables added to the inherited environment.</param>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The captured run.</returns>
    public static async Task<CliRun> RunWithEnvironmentAsync(IReadOnlyDictionary<string, string> environment, params string[] args)
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

        foreach ((string name, string value) in environment)
        {
            start.Environment[name] = value;
        }

        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(_timeout);
        await process.WaitForExitAsync(timeout.Token);
        return new CliRun(process.ExitCode, await stdout, await stderr);
    }
}
