namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// The v1 constraints of a job: precedence edges and locked operation groups, in input order.
/// </summary>
public sealed class ConstraintSet
{
    /// <summary>Creates a constraint set.</summary>
    /// <param name="precedence">Precedence edges in input order.</param>
    /// <param name="lockedGroups">Names of operation groups that must stay contiguous in baseline order.</param>
    public ConstraintSet(IReadOnlyList<PrecedenceEdge> precedence, IReadOnlyList<string> lockedGroups)
    {
        ArgumentNullException.ThrowIfNull(precedence);
        ArgumentNullException.ThrowIfNull(lockedGroups);
        Precedence = precedence.ToArray();
        LockedGroups = lockedGroups.ToArray();
    }

    /// <summary>Gets an empty constraint set.</summary>
    public static ConstraintSet Empty { get; } = new([], []);

    /// <summary>Gets the precedence edges in input order.</summary>
    public IReadOnlyList<PrecedenceEdge> Precedence { get; }

    /// <summary>Gets the locked group names in input order.</summary>
    public IReadOnlyList<string> LockedGroups { get; }
}
