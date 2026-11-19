namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// A value or the <see cref="OperationOutcome"/> explaining why there is none.
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

    /// <summary>Gets the outcome.</summary>
    public OperationOutcome Outcome { get; }

    /// <summary>Gets a value indicating whether a value is present.</summary>
    public bool IsSuccess => Outcome.IsSuccess;

    /// <summary>Gets the value; only available on success.</summary>
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
}
