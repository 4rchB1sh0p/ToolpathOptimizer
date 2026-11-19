namespace ToolpathOptimizer.Web.Security;

/// <summary>
/// Claim types every identity adapter must emit, so <see cref="HttpContextPrincipal"/> stays provider-neutral.
/// </summary>
public static class ToolpathClaimTypes
{
    /// <summary>Claim carrying the tenant identifier from the validated credential.</summary>
    public const string Tenant = "tenant_id";
}
