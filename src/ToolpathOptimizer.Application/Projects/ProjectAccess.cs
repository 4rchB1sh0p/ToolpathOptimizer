using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Authorisation and tenant scoping shared by project services: same-tenant denial → 403, cross-tenant → 404.
/// </summary>
public sealed class ProjectAccess
{
    private readonly ICurrentPrincipal _principal;
    private readonly ITenantContext _tenant;
    private readonly IProjectRepository _repository;

    /// <summary>Creates the access helper.</summary>
    /// <param name="principal">Authenticated user of the current request.</param>
    /// <param name="tenant">Tenant of the current request.</param>
    /// <param name="repository">Used to resolve projects within the tenant.</param>
    public ProjectAccess(ICurrentPrincipal principal, ITenantContext tenant, IProjectRepository repository)
    {
        _principal = principal;
        _tenant = tenant;
        _repository = repository;
    }

    /// <summary>Gets the current tenant identifier.</summary>
    public string TenantId => _tenant.TenantId;

    /// <summary>Gets the current user identifier.</summary>
    public string UserId => _principal.UserId;

    /// <summary>Checks authentication and the role matrix.</summary>
    /// <param name="action">Requested action.</param>
    /// <returns>Success, <c>UNAUTHENTICATED</c> or <c>ACCESS_DENIED</c>.</returns>
    public OperationOutcome Authorize(ProjectAction action)
    {
        if (!_principal.IsAuthenticated || string.IsNullOrEmpty(_tenant.TenantId))
        {
            return OperationOutcome.Failure(ErrorCodes.Unauthenticated, "A validated identity is required.");
        }

        return RolePermissions.IsAllowed(_principal.Role, action)
            ? OperationOutcome.Success
            : OperationOutcome.Failure(ErrorCodes.AccessDenied, $"Role '{_principal.Role}' may not perform '{action}'.");
    }

    /// <summary>Authorises an action and loads a project visible to the current tenant.</summary>
    /// <param name="action">Requested action.</param>
    /// <param name="projectId">Project the action targets.</param>
    /// <returns>The project, or the authorisation or not-found failure.</returns>
    public Result<Project> AuthorizeProject(ProjectAction action, Guid projectId)
    {
        OperationOutcome authorized = Authorize(action);
        return authorized.IsSuccess ? _repository.FindProject(_tenant.TenantId, projectId) : Result<Project>.Failure(authorized);
    }
}
