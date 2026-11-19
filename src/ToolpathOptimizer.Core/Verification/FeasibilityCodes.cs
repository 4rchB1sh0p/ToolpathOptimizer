namespace ToolpathOptimizer.Core.Verification;

/// <summary>
/// Stable violation codes reported by <see cref="FeasibilityValidator"/>.
/// </summary>
public static class FeasibilityCodes
{
    /// <summary>A job point is absent from the route.</summary>
    public const string RouteMissingPoint = "ROUTE_MISSING_POINT";

    /// <summary>A point appears more than once in the route.</summary>
    public const string RouteDuplicatePoint = "ROUTE_DUPLICATE_POINT";

    /// <summary>The route contains a point not in the job.</summary>
    public const string RouteUnknownPoint = "ROUTE_UNKNOWN_POINT";

    /// <summary>A precedence edge is not satisfied.</summary>
    public const string PrecedenceViolated = "PRECEDENCE_VIOLATED";

    /// <summary>A locked group is not visited contiguously.</summary>
    public const string LockedGroupNotContiguous = "LOCKED_GROUP_NOT_CONTIGUOUS";

    /// <summary>A locked group is not visited in baseline order.</summary>
    public const string LockedGroupOrderChanged = "LOCKED_GROUP_ORDER_CHANGED";
}
