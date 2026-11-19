using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Cli.CommandLine;

/// <summary>
/// Small, dependency-free parser for the fixed command set (cli-design.md). Supports <c>--name value</c> and
/// <c>--name=value</c>; rejects unknown, duplicate and missing options.
/// </summary>
public static class CliArgumentParser
{
    private const string Points = "points";
    private const string Job = "job";
    private const string Output = "output";
    private const string Overwrite = "overwrite";
    private const string Format = "format";

    private static readonly Dictionary<CliCommand, string[]> s_valueOptions = new()
    {
        [CliCommand.Analyze] = [Points, Job, Output, Format],
        [CliCommand.Validate] = [Points, Job],
        [CliCommand.Help] = [],
        [CliCommand.Version] = [],
    };

    /// <summary>Gets the usage text.</summary>
    public static string Usage { get; } =
        """
        toolpath-optimize — advisory drilling-sequence analysis (not machine-ready NC code)

        Usage:
          toolpath-optimize analyze --points <csv> --job <json> --output <path> [--overwrite] [--format json]
          toolpath-optimize validate --points <csv> --job <json>
          toolpath-optimize version
          toolpath-optimize help

        stdout carries exactly one JSON result envelope; logs go to stderr.
        Exit codes: 0 ok, 2 arguments/config, 3 invalid input, 4 infeasible constraints,
                    5 processing failure, 6 cancelled, 7 I/O failure, 8 resource budget exceeded.

        """;

    /// <summary>Parses command-line arguments.</summary>
    /// <param name="args">Raw arguments.</param>
    /// <returns>Parsed arguments, or an <c>ARGS_*</c> failure.</returns>
    public static Result<CliArguments> Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 0)
        {
            return Result<CliArguments>.Failure(ErrorCodes.ArgsMissingCommand, "No command given; run 'toolpath-optimize help'.");
        }

        CliCommand? command = args[0] switch
        {
            "analyze" => CliCommand.Analyze,
            "validate" => CliCommand.Validate,
            "version" or "--version" => CliCommand.Version,
            "help" or "--help" or "-h" => CliCommand.Help,
            _ => null,
        };
        if (command is not CliCommand known)
        {
            return Result<CliArguments>.Failure(ErrorCodes.ArgsUnknownCommand, $"Unknown command {InputText.Quote(args[0])}; run 'toolpath-optimize help'.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        bool overwrite = false;
        for (int i = 1; i < args.Count; i++)
        {
            string token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return Result<CliArguments>.Failure(ErrorCodes.ArgsUnknownOption, $"Unexpected argument {InputText.Quote(token)}.");
            }

            string name = token[2..];
            string? inlineValue = null;
            int equals = name.IndexOf('=', StringComparison.Ordinal);
            if (equals >= 0)
            {
                inlineValue = name[(equals + 1)..];
                name = name[..equals];
            }

            if (name == Overwrite && known == CliCommand.Analyze && inlineValue is null)
            {
                if (overwrite)
                {
                    return Result<CliArguments>.Failure(ErrorCodes.ArgsDuplicateOption, "Option '--overwrite' given more than once.");
                }

                overwrite = true;
                continue;
            }

            if (Array.IndexOf(s_valueOptions[known], name) < 0)
            {
                return Result<CliArguments>.Failure(ErrorCodes.ArgsUnknownOption, $"Unknown option {InputText.Quote(token)} for '{args[0]}'.");
            }

            string? value = inlineValue ?? (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : null);
            if (string.IsNullOrEmpty(value))
            {
                return Result<CliArguments>.Failure(ErrorCodes.ArgsMissingValue, $"Option '--{name}' requires a value.");
            }

            if (!values.TryAdd(name, value))
            {
                return Result<CliArguments>.Failure(ErrorCodes.ArgsDuplicateOption, $"Option '--{name}' given more than once.");
            }
        }

        return Validate(known, values, overwrite);
    }

    private static Result<CliArguments> Validate(CliCommand command, Dictionary<string, string> values, bool overwrite)
    {
        string[] required = command switch
        {
            CliCommand.Analyze => [Points, Job, Output],
            CliCommand.Validate => [Points, Job],
            _ => [],
        };
        foreach (string name in required)
        {
            if (!values.ContainsKey(name))
            {
                return Result<CliArguments>.Failure(ErrorCodes.ArgsMissingOption, $"Missing required option '--{name}'.");
            }
        }

        if (values.TryGetValue(Format, out string? format) && format != "json")
        {
            return Result<CliArguments>.Failure(ErrorCodes.ArgsInvalidValue, $"Unsupported --format {InputText.Quote(format)}; only 'json' is supported.");
        }

        return Result<CliArguments>.Success(new CliArguments(
            command,
            values.GetValueOrDefault(Points, string.Empty),
            values.GetValueOrDefault(Job, string.Empty),
            values.GetValueOrDefault(Output, string.Empty),
            overwrite));
    }
}
