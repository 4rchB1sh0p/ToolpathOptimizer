namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// A node of the contracted constraint graph: a single point or a locked block visited contiguously in baseline order.
/// </summary>
public sealed class RouteNode
{
    internal RouteNode(int[] members)
    {
        MemberArray = members;
    }

    /// <summary>Gets the baseline indices of the member points in visit order.</summary>
    public IReadOnlyList<int> Members => MemberArray;

    /// <summary>Gets the baseline index of the first member (entry point).</summary>
    public int Entry => MemberArray[0];

    /// <summary>Gets the baseline index of the last member (exit point).</summary>
    public int Exit => MemberArray[^1];

    internal int[] MemberArray { get; }
}
