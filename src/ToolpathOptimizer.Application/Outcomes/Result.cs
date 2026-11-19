namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// A value or the <see cref="OperationOutcome"/> explaining why there is none. Used instead of <c>null</c>
/// or exceptions for every expected failure, including "not found".
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, OperationOutcome outcome)
    {
        _value = value;
        Outcome = outcome;
    }

    /// <summary>Gets the outcome; successful when a value is present.</summary>
    public OperationOutcome Outcome { get; }

    /// <summary>Gets a value indicating whether a value is present.</summary>
    public bool IsSuccess => Outcome.IsSuccess;

    /// <summary>Gets the value; throws when the result is a failure.</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on failure.");

    /// <summary>Creates a success.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The result.</returns>
    public static Result<T> Success(T value) => new(value, OperationOutcome.Success);

    /// <summary>Creates a failure.</summary>
    /// <param name="outcome">Failure outcome.</param>
    /// <returns>The result.</returns>
    public static Result<T> Failure(OperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.IsSuccess)
        {
            throw new ArgumentException("Outcome must be a failure.", nameof(outcome));
        }

        return new Result<T>(default, outcome);
    }

    /// <summary>Creates a failure from a code and message.</summary>
    /// <param name="code">Stable code.</param>
    /// <param name="message">Explanation.</param>
    /// <returns>The result.</returns>
    public static Result<T> Failure(string code, string message) => Failure(OperationOutcome.Failure(code, message));

    /// <summary>Creates a <see cref="ErrorCodes.ResourceNotFound"/> failure.</summary>
    /// <param name="resourceKind">Kind of resource, e.g. <c>Project</c>.</param>
    /// <returns>The result.</returns>
    public static Result<T> NotFound(string resourceKind) => Failure(ErrorCodes.ResourceNotFound, $"{resourceKind} not found.");

    /// <summary>Re-types this failure for a caller that returns a different value type.</summary>
    /// <typeparam name="TOther">Caller's value type.</typeparam>
    /// <returns>A failure carrying the same outcome.</returns>
    public Result<TOther> FailAs<TOther>() =>
        IsSuccess ? throw new InvalidOperationException("Only failures can be re-typed.") : Result<TOther>.Failure(Outcome);
}
