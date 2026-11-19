using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// Maps stable codes to <see cref="OutcomeCategory"/> (input-output-contracts.md, ADR-0011).
/// </summary>
public static class ErrorCatalog
{
    private static readonly Dictionary<string, OutcomeCategory> s_explicit = new(StringComparer.Ordinal)
    {
        [ErrorCodes.ConfigInvalid] = OutcomeCategory.Arguments,
        [ValidationCodes.ConstraintUnknownPoint] = OutcomeCategory.Input,
        [ValidationCodes.ConstraintUnknownGroup] = OutcomeCategory.Input,
        [ValidationCodes.ConstraintCycle] = OutcomeCategory.Infeasible,
        [ValidationCodes.ConstraintLockedGroupConflict] = OutcomeCategory.Infeasible,
        [AnalysisCodes.OptCandidateRejected] = OutcomeCategory.Processing,
        [ErrorCodes.InternalError] = OutcomeCategory.Processing,
        [ErrorCodes.Cancelled] = OutcomeCategory.Cancelled,
        [ErrorCodes.OptDeadlineExceeded] = OutcomeCategory.Budget,
        [ErrorCodes.ResourceNotFound] = OutcomeCategory.NotFound,
        [ErrorCodes.AccessDenied] = OutcomeCategory.Forbidden,
        [ErrorCodes.Unauthenticated] = OutcomeCategory.Forbidden,
        [ErrorCodes.QuotaExceeded] = OutcomeCategory.Quota,
        [ErrorCodes.IdempotencyConflict] = OutcomeCategory.Conflict,
    };

    /// <summary>Gets the category of a code.</summary>
    /// <param name="code">Stable code.</param>
    /// <returns>The category; unknown codes are treated as processing failures.</returns>
    public static OutcomeCategory CategoryOf(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (s_explicit.TryGetValue(code, out OutcomeCategory category))
        {
            return category;
        }

        if (code.StartsWith("ARGS_", StringComparison.Ordinal))
        {
            return OutcomeCategory.Arguments;
        }

        if (code.StartsWith("INPUT_", StringComparison.Ordinal))
        {
            return OutcomeCategory.Input;
        }

        return code.StartsWith("IO_", StringComparison.Ordinal) ? OutcomeCategory.Io : OutcomeCategory.Processing;
    }
}
