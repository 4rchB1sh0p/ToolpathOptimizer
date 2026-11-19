namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Stable error codes produced by <see cref="JobValidator"/> (public contract; see input-output-contracts.md).
/// </summary>
public static class ValidationCodes
{
    /// <summary>A precedence edge references a point that does not exist.</summary>
    public const string ConstraintUnknownPoint = "CONSTRAINT_UNKNOWN_POINT";

    /// <summary>A locked group names an operation group no point belongs to.</summary>
    public const string ConstraintUnknownGroup = "CONSTRAINT_UNKNOWN_GROUP";

    /// <summary>Precedence edges form a cycle.</summary>
    public const string ConstraintCycle = "CONSTRAINT_CYCLE";

    /// <summary>Precedence edges conflict with locked group order or contiguity.</summary>
    public const string ConstraintLockedGroupConflict = "CONSTRAINT_LOCKED_GROUP_CONFLICT";
}
