using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// The authenticated user, derived only from a validated credential.
/// </summary>
public interface ICurrentPrincipal
{
    /// <summary>Gets a value indicating whether a validated identity with a recognised role is present.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets the identity provider's user identifier; empty when unauthenticated.</summary>
    string UserId { get; }

    /// <summary>Gets the user's role within the tenant; meaningful only when <see cref="IsAuthenticated"/>.</summary>
    Role Role { get; }
}
