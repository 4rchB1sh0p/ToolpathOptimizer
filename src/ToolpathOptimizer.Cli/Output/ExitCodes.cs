using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Cli.Output;

/// <summary>
/// Stable process exit codes (ADR-0011). Code 1 is reserved and never returned.
/// </summary>
public static class ExitCodes
{
    /// <summary>Success.</summary>
    public const int Success = 0;

    /// <summary>Invalid arguments or configuration.</summary>
    public const int Arguments = 2;

    /// <summary>Invalid input.</summary>
    public const int Input = 3;

    /// <summary>Infeasible constraints.</summary>
    public const int Infeasible = 4;

    /// <summary>Processing failure or defect.</summary>
    public const int Processing = 5;

    /// <summary>Cancelled by Ctrl+C, SIGTERM or the caller.</summary>
    public const int Cancelled = 6;

    /// <summary>I/O failure.</summary>
    public const int Io = 7;

    /// <summary>Wall-clock safety deadline exceeded.</summary>
    public const int Budget = 8;

    /// <summary>Maps an outcome category to its exit code.</summary>
    /// <param name="category">Outcome category.</param>
    /// <returns>The exit code; web-only categories map to <see cref="Processing"/>.</returns>
    public static int FromCategory(OutcomeCategory category) => category switch
    {
        OutcomeCategory.Success => Success,
        OutcomeCategory.Arguments => Arguments,
        OutcomeCategory.Input => Input,
        OutcomeCategory.Infeasible => Infeasible,
        OutcomeCategory.Cancelled => Cancelled,
        OutcomeCategory.Io => Io,
        OutcomeCategory.Budget => Budget,
        _ => Processing,
    };
}
