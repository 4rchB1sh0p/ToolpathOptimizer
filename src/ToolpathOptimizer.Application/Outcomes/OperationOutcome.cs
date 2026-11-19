namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// The result category and errors of an application operation that returns no value.
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

    /// <summary>Gets the category; <see cref="OutcomeCategory.Success"/> when there are no errors.</summary>
    public OutcomeCategory Category { get; }

    /// <summary>Gets the errors in deterministic order; empty on success.</summary>
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
