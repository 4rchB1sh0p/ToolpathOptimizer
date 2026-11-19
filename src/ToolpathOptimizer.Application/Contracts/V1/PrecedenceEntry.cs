namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// A precedence constraint exactly as written in the manifest (identifiers validated, references not yet checked).
/// </summary>
public sealed class PrecedenceEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="before">Identifier of the point that must be drilled first.</param>
    /// <param name="after">Identifier of the point that must be drilled later.</param>
    public PrecedenceEntry(string before, string after)
    {
        Before = before;
        After = after;
    }

    /// <summary>Gets the identifier of the point that must be drilled first.</summary>
    public string Before { get; }

    /// <summary>Gets the identifier of the point that must be drilled later.</summary>
    public string After { get; }
}
