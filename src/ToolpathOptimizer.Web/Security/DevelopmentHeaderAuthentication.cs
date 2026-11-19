using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Web.Api;

namespace ToolpathOptimizer.Web.Security;

/// <summary>
/// Development-only authentication from <c>X-Dev-Tenant</c>, <c>X-Dev-User</c> and <c>X-Dev-Role</c> headers.
/// Never registered outside Development; replaced by a verified provider adapter after plan 0004.
/// </summary>
public sealed class DevelopmentHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Scheme name.</summary>
    public const string SchemeName = "DevelopmentHeaders";

    /// <summary>Tenant header.</summary>
    public const string TenantHeader = "X-Dev-Tenant";

    /// <summary>User header.</summary>
    public const string UserHeader = "X-Dev-User";

    /// <summary>Role header.</summary>
    public const string RoleHeader = "X-Dev-Role";

    /// <summary>Claim type carrying the tenant.</summary>
    public const string TenantClaim = "tenant_id";

    /// <summary>Creates the handler.</summary>
    /// <param name="options">Scheme options.</param>
    /// <param name="logger">Logger factory.</param>
    /// <param name="encoder">URL encoder.</param>
    public DevelopmentHeaderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string tenant = Request.Headers[TenantHeader].ToString();
        string user = Request.Headers[UserHeader].ToString();
        string roleText = Request.Headers[RoleHeader].ToString();
        if (tenant.Length == 0 && user.Length == 0 && roleText.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!IdentifierRules.IsValid(tenant) || !IdentifierRules.IsValid(user)
            || !Enum.TryParse(roleText, ignoreCase: true, out Role role) || !Enum.IsDefined(role) || int.TryParse(roleText, out _))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid development identity headers."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user), new Claim(TenantClaim, tenant), new Claim(ClaimTypes.Role, role.ToString())],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ProblemResults.From(OperationOutcome.Failure(ErrorCodes.Unauthenticated, "Supply valid X-Dev-Tenant, X-Dev-User and X-Dev-Role headers (Development only)."))
            .ExecuteAsync(Context);
}

/// <summary>
/// Adapts the authenticated <see cref="HttpContext"/> user to the Application identity ports.
/// The tenant comes only from the validated credential, never from route, query or body.
/// </summary>
public sealed class HttpContextPrincipal : ICurrentPrincipal, ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Creates the adapter.</summary>
    /// <param name="accessor">HTTP context accessor.</param>
    public HttpContextPrincipal(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public string UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <inheritdoc />
    public Role Role => Enum.TryParse(User?.FindFirstValue(ClaimTypes.Role), out Role role) ? role : Role.Viewer;

    /// <inheritdoc />
    public string TenantId => IsAuthenticated ? User!.FindFirstValue(DevelopmentHeaderAuthenticationHandler.TenantClaim) ?? string.Empty : string.Empty;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;
}
