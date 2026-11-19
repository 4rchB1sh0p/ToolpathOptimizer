using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Cli.CommandLine;

/// <summary>
/// Small, dependency-free parser for the fixed command set (cli-design.md, D-020). Supports <c>--name value</c>
/// and <c>--name=value</c>; rejects unknown, duplicate and missing options.
/// </summary>
public static class CliArgumentParser
{
    private const string OptionPrefix = "--";
    private const string PointsOption = "points";
    private const string JobOption = "job";
    private const string OutputOption = "output";
    private const string OverwriteFlag = "overwrite";
    private const string FormatOption = "format";
    private const string JsonFormat = "json";

    private static readonly Dictionary<CliCommand, string[]> _valueOptionsByCommand = new()
    {
        [CliCommand.Analyze] = [PointsOption, JobOption, OutputOption, FormatOption],
        [CliCommand.Validate] = [PointsOption, JobOption],
        [CliCommand.Help] = [],
        [CliCommand.Version] = [],
    };

    private static readonly Dictionary<CliCommand, string[]> _requiredOptionsByCommand = new()
    {
        [CliCommand.Analyze] = [PointsOption, JobOption, OutputOption],
        [CliCommand.Validate] = [PointsOption, JobOption],
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
    /// <param name="args">Raw arguments; the first is the command.</param>
    /// <returns>Parsed arguments, or an <c>ARGS_*</c> failure.</returns>
    public static Result<CliArguments> Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Count == 0)
        {
            return Result<CliArguments>.Failure(ErrorCodes.ArgsMissingCommand, "No command given; run 'toolpath-optimize help'.");
        }

        Result<CliCommand> command = ParseCommand(args[0]);
        if (!command.IsSuccess)
        {
            return command.FailAs<CliArguments>();
        }

        Result<ParsedOptions> options = ParseOptions(command.Value, args);
        return options.IsSuccess ? BuildArguments(command.Value, options.Value) : options.FailAs<CliArguments>();
    }

    private static Result<CliCommand> ParseCommand(string token) => token switch
    {
        "analyze" => Result<CliCommand>.Success(CliCommand.Analyze),
        "validate" => Result<CliCommand>.Success(CliCommand.Validate),
        "version" or "--version" => Result<CliCommand>.Success(CliCommand.Version),
        "help" or "--help" or "-h" => Result<CliCommand>.Success(CliCommand.Help),
        _ => Result<CliCommand>.Failure(ErrorCodes.ArgsUnknownCommand, $"Unknown command {InputText.Quote(token)}; run 'toolpath-optimize help'."),
    };

    private static Result<ParsedOptions> ParseOptions(CliCommand command, IReadOnlyList<string> args)
    {
        var options = new ParsedOptions();
        int index = 1;
        while (index < args.Count)
        {
            Result<OptionMatch> match = MatchOption(command, args, index);
            if (!match.IsSuccess)
            {
                return match.FailAs<ParsedOptions>();
            }

            OperationOutcome added = options.Add(match.Value);
            if (!added.IsSuccess)
            {
                return Result<ParsedOptions>.Failure(added);
            }

            index += match.Value.ConsumedArgumentCount;
        }

        return Result<ParsedOptions>.Success(options);
    }

    /// <summary>Recognises the option at <paramref name="index"/> without changing any state.</summary>
    private static Result<OptionMatch> MatchOption(CliCommand command, IReadOnlyList<string> args, int index)
    {
        string token = args[index];
        if (!token.StartsWith(OptionPrefix, StringComparison.Ordinal))
        {
            return Result<OptionMatch>.Failure(ErrorCodes.ArgsUnknownOption, $"Unexpected argument {InputText.Quote(token)}.");
        }

        string body = token[OptionPrefix.Length..];
        int equalsIndex = body.IndexOf('=', StringComparison.Ordinal);
        bool hasInlineValue = equalsIndex >= 0;
        string name = hasInlineValue ? body[..equalsIndex] : body;

        if (name == OverwriteFlag && command == CliCommand.Analyze && !hasInlineValue)
        {
            return Result<OptionMatch>.Success(OptionMatch.ForFlag(name));
        }

        if (Array.IndexOf(_valueOptionsByCommand[command], name) < 0)
        {
            return Result<OptionMatch>.Failure(ErrorCodes.ArgsUnknownOption, $"Unknown option {InputText.Quote(token)} for '{command.ToString().ToLowerInvariant()}'.");
        }

        if (hasInlineValue)
        {
            return CreateValueMatch(name, body[(equalsIndex + 1)..], consumedArgumentCount: 1);
        }

        bool hasFollowingValue = index + 1 < args.Count && !args[index + 1].StartsWith(OptionPrefix, StringComparison.Ordinal);
        return hasFollowingValue
            ? CreateValueMatch(name, args[index + 1], consumedArgumentCount: 2)
            : CreateValueMatch(name, string.Empty, consumedArgumentCount: 1);
    }

    private static Result<OptionMatch> CreateValueMatch(string name, string value, int consumedArgumentCount) =>
        value.Length == 0
            ? Result<OptionMatch>.Failure(ErrorCodes.ArgsMissingValue, $"Option '--{name}' requires a value.")
            : Result<OptionMatch>.Success(OptionMatch.ForValue(name, value, consumedArgumentCount));

    private static Result<CliArguments> BuildArguments(CliCommand command, ParsedOptions options)
    {
        string? missing = _requiredOptionsByCommand[command].FirstOrDefault(name => !options.Values.ContainsKey(name));
        if (missing is not null)
        {
            return Result<CliArguments>.Failure(ErrorCodes.ArgsMissingOption, $"Missing required option '--{missing}'.");
        }

        if (options.Values.TryGetValue(FormatOption, out string? format) && format != JsonFormat)
        {
            return Result<CliArguments>.Failure(ErrorCodes.ArgsInvalidValue, $"Unsupported --format {InputText.Quote(format)}; only 'json' is supported.");
        }

        return Result<CliArguments>.Success(new CliArguments(
            command,
            options.Values.GetValueOrDefault(PointsOption, string.Empty),
            options.Values.GetValueOrDefault(JobOption, string.Empty),
            options.Values.GetValueOrDefault(OutputOption, string.Empty),
            options.WriteMode));
    }

    /// <summary>One recognised option: a value option or a flag, and how many arguments it used.</summary>
    private sealed class OptionMatch
    {
        private OptionMatch(string name, string value, bool isFlag, int consumedArgumentCount)
        {
            Name = name;
            Value = value;
            IsFlag = isFlag;
            ConsumedArgumentCount = consumedArgumentCount;
        }

        /// <summary>Gets the option name without the prefix.</summary>
        public string Name { get; }

        /// <summary>Gets the option value; empty for flags.</summary>
        public string Value { get; }

        /// <summary>Gets a value indicating whether the option is a flag without a value.</summary>
        public bool IsFlag { get; }

        /// <summary>Gets the number of arguments the option used (1 or 2).</summary>
        public int ConsumedArgumentCount { get; }

        /// <summary>Creates a flag match.</summary>
        /// <param name="name">Flag name.</param>
        /// <returns>The match.</returns>
        public static OptionMatch ForFlag(string name) => new(name, string.Empty, isFlag: true, consumedArgumentCount: 1);

        /// <summary>Creates a value match.</summary>
        /// <param name="name">Option name.</param>
        /// <param name="value">Non-empty value.</param>
        /// <param name="consumedArgumentCount">Arguments used.</param>
        /// <returns>The match.</returns>
        public static OptionMatch ForValue(string name, string value, int consumedArgumentCount) => new(name, value, isFlag: false, consumedArgumentCount);
    }

    /// <summary>Options collected so far; rejects duplicates.</summary>
    private sealed class ParsedOptions
    {
        /// <summary>Gets the value options by name.</summary>
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the report write mode set by <c>--overwrite</c>.</summary>
        public ReportWriteMode WriteMode { get; private set; } = ReportWriteMode.CreateNew;

        /// <summary>Adds a recognised option.</summary>
        /// <param name="match">The option.</param>
        /// <returns>Success, or <c>ARGS_DUPLICATE_OPTION</c>.</returns>
        public OperationOutcome Add(OptionMatch match)
        {
            bool isDuplicate = match.IsFlag ? WriteMode == ReportWriteMode.Overwrite : !Values.TryAdd(match.Name, match.Value);
            if (isDuplicate)
            {
                return OperationOutcome.Failure(ErrorCodes.ArgsDuplicateOption, $"Option '--{match.Name}' given more than once.");
            }

            if (match.IsFlag)
            {
                WriteMode = ReportWriteMode.Overwrite;
            }

            return OperationOutcome.Success;
        }
    }
}
