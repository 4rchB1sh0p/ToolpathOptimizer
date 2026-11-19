using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Provenance;
using ToolpathOptimizer.Cli.CommandLine;
using ToolpathOptimizer.Cli.Commands;
using ToolpathOptimizer.Cli.Output;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Infrastructure;

namespace ToolpathOptimizer.Cli;

/// <summary>
/// CLI entry point: parses arguments, builds the Generic Host, runs one command and writes one envelope to stdout.
/// Ctrl+C / SIGTERM stop the host, which cancels the command (exit 6).
/// </summary>
public static partial class CliApplication
{
    /// <summary>Runs the CLI.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="stdout">Writer for the result envelope.</param>
    /// <param name="configureServices">Optional service overrides (tests).</param>
    /// <param name="cancellationToken">External cancellation (tests).</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string[] args, TextWriter stdout, Action<IServiceCollection>? configureServices, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stdout);
        Result<CliArguments> parsed = CliArgumentParser.Parse(args);
        if (!parsed.IsSuccess)
        {
            return Emit(stdout, CliResult.Failure(parsed.Outcome));
        }

        CliArguments arguments = parsed.Value;
        if (arguments.Command == CliCommand.Help)
        {
            await stdout.WriteAsync(CliArgumentParser.Usage).ConfigureAwait(false);
            return ExitCodes.Success;
        }

        if (arguments.Command == CliCommand.Version)
        {
            WriteVersion(stdout);
            return ExitCodes.Success;
        }

        using IHost host = BuildHost(configureServices);
        try
        {
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OptionsValidationException ex)
        {
            return Emit(stdout, CliResult.Failure(OperationOutcome.Failure(ErrorCodes.ConfigInvalid, $"Invalid configuration: {string.Join(" ", ex.Failures)}")));
        }
        catch (OperationCanceledException)
        {
            return Emit(stdout, CliResult.Failure(OperationOutcome.Failure(ErrorCodes.Cancelled, "The command was cancelled.")));
        }

        CliResult result = await ExecuteAsync(host.Services, arguments, cancellationToken).ConfigureAwait(false);
        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        return Emit(stdout, result);
    }

    private static async Task<CliResult> ExecuteAsync(IServiceProvider services, CliArguments arguments, CancellationToken cancellationToken)
    {
        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ToolpathOptimizer.Cli");
        IHostApplicationLifetime lifetime = services.GetRequiredService<IHostApplicationLifetime>();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.ApplicationStopping);
        LogCommandStarted(logger, arguments.Command);
        try
        {
            CliResult result = arguments.Command == CliCommand.Analyze
                ? await services.GetRequiredService<AnalyzeCommand>().ExecuteAsync(arguments, linked.Token).ConfigureAwait(false)
                : await services.GetRequiredService<ValidateCommand>().ExecuteAsync(arguments, linked.Token).ConfigureAwait(false);
            LogCommandFinished(logger, arguments.Command, result.ExitCode);
            return result;
        }
        catch (OperationCanceledException)
        {
            LogCommandFinished(logger, arguments.Command, ExitCodes.Cancelled);
            return CliResult.Failure(OperationOutcome.Failure(ErrorCodes.Cancelled, "The command was cancelled."));
        }
    }

    private static IHost BuildHost(Action<IServiceCollection>? configureServices)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Configuration.AddEnvironmentVariables("TOOLPATH_");
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
        builder.Services.AddToolpathApplication();
        builder.Services.AddToolpathInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<JobInputLoader>();
        builder.Services.AddSingleton<AnalyzeCommand>();
        builder.Services.AddSingleton<ValidateCommand>();
        configureServices?.Invoke(builder.Services);
        return builder.Build();
    }

    private static void WriteVersion(TextWriter stdout)
    {
        ToolInfo tool = ToolInfo.FromAssembly();
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            json.WriteStartObject();
            json.WriteString("tool_name", tool.ToolName);
            json.WriteString("tool_version", tool.ToolVersion);
            json.WriteString("algorithm_name", RouteOptimiser.AlgorithmName);
            json.WriteString("algorithm_version", RouteOptimiser.AlgorithmVersion);
            json.WriteString("input_schema_version", ContractVersions.ManifestSchemaVersion);
            json.WriteString("report_schema_version", ContractVersions.ReportSchemaVersion);
            json.WriteEndObject();
        }

        stdout.Write(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
        stdout.Write('\n');
        stdout.Flush();
    }

    private static int Emit(TextWriter stdout, CliResult result)
    {
        result.WriteTo(stdout);
        return result.ExitCode;
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Command started: {Command}")]
    private static partial void LogCommandStarted(ILogger logger, CliCommand command);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Command finished: {Command} exitCode={ExitCode}")]
    private static partial void LogCommandFinished(ILogger logger, CliCommand command, int exitCode);
}
