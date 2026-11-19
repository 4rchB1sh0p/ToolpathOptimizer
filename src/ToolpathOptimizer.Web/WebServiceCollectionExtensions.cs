using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Web.Api;
using ToolpathOptimizer.Web.Security;

namespace ToolpathOptimizer.Web;

/// <summary>
/// Web host composition: limits, development identity, rate limiting, API JSON, health and the request pipeline.
/// </summary>
public static class WebServiceCollectionExtensions
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";

    /// <summary>Adds all Web services.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddToolpathWeb(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddWebLimits(configuration)
            .AddDevelopmentIdentity()
            .AddPerUserRateLimiting()
            .AddApiJson()
            .AddHealthChecks()
            .Services;

    /// <summary>Configures the request pipeline.</summary>
    /// <param name="app">Web application.</param>
    /// <returns>The same application.</returns>
    public static WebApplication UseToolpathWeb(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseExceptionHandler();
        app.Use(AddSecurityHeadersAsync);
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready");
        app.MapProjectEndpoints();
        return app;
    }

    private static IServiceCollection AddWebLimits(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebLimitsOptions>()
            .Bind(configuration.GetSection(WebLimitsOptions.SectionName))
            .Validate(limits => limits.MaxRequestBytes > 0 && limits.PermitLimit > 0 && limits.Window > TimeSpan.Zero, "WebLimits must be positive.")
            .ValidateOnStart();
        services.AddOptions<FormOptions>()
            .Configure<IOptions<WebLimitsOptions>>((form, limits) => form.MultipartBodyLengthLimit = limits.Value.MaxRequestBytes);
        return services;
    }

    private static IServiceCollection AddDevelopmentIdentity(this IServiceCollection services)
    {
        services.AddAuthentication(DevelopmentHeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(DevelopmentHeaderAuthenticationHandler.SchemeName, _ => { });
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<HttpContextPrincipal>();
        services.AddScoped<ICurrentPrincipal>(provider => provider.GetRequiredService<HttpContextPrincipal>());
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<HttpContextPrincipal>());
        return services;
    }

    private static IServiceCollection AddPerUserRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) => new ValueTask(ProblemResults
                .From(OperationOutcome.Failure(ErrorCodes.QuotaExceeded, "Too many requests; retry later."))
                .ExecuteAsync(context.HttpContext));
            options.AddPolicy(ProjectEndpoints.RateLimitPolicy, CreateUserPartition);
        });

    private static RateLimitPartition<string> CreateUserPartition(HttpContext http)
    {
        WebLimitsOptions limits = http.RequestServices.GetRequiredService<IOptions<WebLimitsOptions>>().Value;
        string partitionKey = $"{http.User.FindFirstValue(ToolpathClaimTypes.Tenant)}/{http.User.FindFirstValue(ClaimTypes.NameIdentifier)}";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limits.PermitLimit,
            Window = limits.Window,
            QueueLimit = 0,
        });
    }

    private static IServiceCollection AddApiJson(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        });
        return services;
    }

    private static async Task AddSecurityHeadersAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next(context).ConfigureAwait(false);
    }
}
