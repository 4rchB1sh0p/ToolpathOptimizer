using System.Security.Claims;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Security;

/// <summary>
/// Adapts the authenticated <see cref="HttpContext"/> user to the Application identity ports. Works with any
/// identity adapter that emits <see cref="ToolpathClaimTypes.Tenant"/>, a name identifier and a role claim.
/// A user without a recognised role is treated as unauthenticated rather than silently downgraded.
/// </summary>
public sealed class HttpContextPrincipal : ICurrentPrincipal, ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Creates the adapter.</summary>
    /// <param name="accessor">Accessor for the current request.</param>
    public HttpContextPrincipal(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true && TryReadRole(out _);

    /// <inheritdoc />
    public string UserId => IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty : string.Empty;

    /// <inheritdoc />
    public Role Role => TryReadRole(out Role role) ? role : Role.Viewer;

    /// <inheritdoc />
    public string TenantId => IsAuthenticated ? User.FindFirstValue(ToolpathClaimTypes.Tenant) ?? string.Empty : string.Empty;

    private ClaimsPrincipal User => _accessor.HttpContext?.User ?? new ClaimsPrincipal();

    private bool TryReadRole(out Role role) => Enum.TryParse(User.FindFirstValue(ClaimTypes.Role), out role) && Enum.IsDefined(role);
}
