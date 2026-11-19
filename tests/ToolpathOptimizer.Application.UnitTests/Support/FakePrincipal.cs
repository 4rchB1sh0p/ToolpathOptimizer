using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Mutable identity for tests: an authenticated Engineer in <c>tenant-a</c> by default.</summary>
internal sealed class FakePrincipal : ICurrentPrincipal, ITenantContext
{
    /// <inheritdoc />
    public bool IsAuthenticated { get; set; } = true;

    /// <inheritdoc />
    public string UserId { get; set; } = "user-1";

    /// <inheritdoc />
    public Role Role { get; set; } = Role.Engineer;

    /// <inheritdoc />
    public string TenantId { get; set; } = "tenant-a";
}
