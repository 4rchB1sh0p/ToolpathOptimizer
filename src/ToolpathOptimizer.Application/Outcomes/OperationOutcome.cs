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

/// <summary>
/// A coded error with a message that never echoes more than 64 characters of input.
/// </summary>
public sealed class OutcomeError
{
    /// <summary>Creates an error.</summary>
    /// <param name="code">Stable code.</param>
    /// <param name="message">Explanation naming file, row or field.</param>
    public OutcomeError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable code.</summary>
    public string Code { get; }

    /// <summary>Gets the explanation.</summary>
    public string Message { get; }
}

/// <summary>
/// The result category and errors of an application operation.
/// </summary>
public sealed class OperationOutcome
{
    private OperationOutcome(OutcomeCategory category, IReadOnlyList<OutcomeError> errors)
    {
        Category = category;
        Errors = errors;
    }

    /// <summary>Gets the shared success outcome.</summary>
    public static OperationOutcome Success { get; } = new(OutcomeCategory.Success, []);

    /// <summary>Gets the category.</summary>
    public OutcomeCategory Category { get; }

    /// <summary>Gets the errors; empty on success.</summary>
    public IReadOnlyList<OutcomeError> Errors { get; }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => Category == OutcomeCategory.Success;

    /// <summary>Creates a failure whose category is derived from the code via <see cref="ErrorCatalog"/>.</summary>
    /// <param name="code">Stable code.</param>
    /// <param name="message">Explanation.</param>
    /// <returns>The failure outcome.</returns>
    public static OperationOutcome Failure(string code, string message) => Failure([new OutcomeError(code, message)]);

    /// <summary>Creates a failure from one or more errors; the first error determines the category.</summary>
    /// <param name="errors">Errors in deterministic order.</param>
    /// <returns>The failure outcome.</returns>
    public static OperationOutcome Failure(IReadOnlyList<OutcomeError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failure needs at least one error.", nameof(errors));
        }

        return new OperationOutcome(ErrorCatalog.CategoryOf(errors[0].Code), errors.ToArray());
    }
}
