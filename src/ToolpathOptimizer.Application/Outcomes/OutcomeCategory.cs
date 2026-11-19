namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// Failure classes mapped by hosts to CLI exit codes or HTTP problem types.
/// </summary>
public enum OutcomeCategory
{
    /// <summary>The operation succeeded.</summary>
    Success,

    /// <summary>Invalid arguments or configuration (exit 2).</summary>
    Arguments,

    /// <summary>Invalid input (exit 3).</summary>
    Input,

    /// <summary>Infeasible constraints (exit 4).</summary>
    Infeasible,

    /// <summary>Processing failure or defect (exit 5).</summary>
    Processing,

    /// <summary>Cancelled by the caller (exit 6).</summary>
    Cancelled,

    /// <summary>I/O failure (exit 7).</summary>
    Io,

    /// <summary>Resource budget exceeded (exit 8).</summary>
    Budget,

    /// <summary>Resource not found or not visible to the tenant (web only).</summary>
    NotFound,

    /// <summary>Authenticated but not permitted (web only).</summary>
    Forbidden,

    /// <summary>Plan quota exceeded (web only).</summary>
    Quota,

    /// <summary>Conflicting request, e.g. idempotency key reuse (web only).</summary>
    Conflict,
}
