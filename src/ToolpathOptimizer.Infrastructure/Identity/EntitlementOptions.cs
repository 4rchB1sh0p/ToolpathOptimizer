namespace ToolpathOptimizer.Infrastructure.Identity;

/// <summary>
/// Options for <see cref="StaticEntitlementService"/>.
/// </summary>
public sealed class EntitlementOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Entitlements";

    /// <summary>Gets or sets the maximum points per job for every tenant.</summary>
    public int MaxPointsPerJob { get; set; } = 5000;
}

