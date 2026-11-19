namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Outcome of <see cref="JobValidator.Validate"/>: either issues, or a prepared <see cref="ConstraintModel"/>.
/// </summary>
public sealed class ValidationResult
{
    private readonly ConstraintModel? _model;

    private ValidationResult(IReadOnlyList<ValidationIssue> issues, ConstraintModel? model)
    {
        Issues = issues;
        _model = model;
    }

    /// <summary>Gets the issues in deterministic order; empty when valid.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>Gets a value indicating whether the job is valid.</summary>
    public bool IsValid => Issues.Count == 0;

    /// <summary>Gets the prepared constraint model; only available when <see cref="IsValid"/>.</summary>
    public ConstraintModel Model => _model ?? throw new InvalidOperationException("Validation failed; no model available.");

    internal static ValidationResult Valid(ConstraintModel model) => new([], model);

    internal static ValidationResult Invalid(IReadOnlyList<ValidationIssue> issues) => new(issues.ToArray(), null);
}
