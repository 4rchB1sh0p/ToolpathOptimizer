using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Outcomes;
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

    /// <summary>Header carrying the tenant identifier.</summary>
    public const string TenantHeader = "X-Dev-Tenant";

    /// <summary>Header carrying the user identifier.</summary>
    public const string UserHeader = "X-Dev-User";

    /// <summary>Header carrying the role name (Owner, Engineer, Reviewer or Viewer).</summary>
    public const string RoleHeader = "X-Dev-Role";

    /// <summary>Creates the handler.</summary>
    /// <param name="options">Scheme options.</param>
    /// <param name="logger">Logger factory required by the base handler.</param>
    /// <param name="encoder">URL encoder required by the base handler.</param>
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

        if (!IdentifierRules.IsValid(tenant) || !IdentifierRules.IsValid(user) || !TryParseRole(roleText, out Role role))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid development identity headers."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user), new Claim(ToolpathClaimTypes.Tenant, tenant), new Claim(ClaimTypes.Role, role.ToString())],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ProblemResults.From(OperationOutcome.Failure(ErrorCodes.Unauthenticated, "Supply valid X-Dev-Tenant, X-Dev-User and X-Dev-Role headers (Development only)."))
            .ExecuteAsync(Context);

    /// <summary>Accepts role names only (numeric values such as <c>3</c> are rejected).</summary>
    private static bool TryParseRole(string text, out Role role) =>
        Enum.TryParse(text, ignoreCase: true, out role) && Enum.IsDefined(role) && !int.TryParse(text, out _);
}
