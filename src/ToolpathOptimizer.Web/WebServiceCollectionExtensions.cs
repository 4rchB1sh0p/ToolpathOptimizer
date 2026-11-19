using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Web.Api;
using ToolpathOptimizer.Web.Security;

namespace ToolpathOptimizer.Web;

/// <summary>
/// Web host composition: authentication, identity ports, limits, rate limiting, problem details and the pipeline.
/// </summary>
public static class WebServiceCollectionExtensions
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";

    /// <summary>Adds Web services.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddToolpathWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebLimitsOptions>().Bind(configuration.GetSection(WebLimitsOptions.SectionName))
            .Validate(o => o.MaxRequestBytes > 0 && o.PermitLimit > 0 && o.Window > TimeSpan.Zero, "WebLimits must be positive.")
            .ValidateOnStart();
        services.AddSingleton<IConfigureOptions<FormOptions>>(sp => new ConfigureOptions<FormOptions>(form =>
            form.MultipartBodyLengthLimit = sp.GetRequiredService<IOptions<WebLimitsOptions>>().Value.MaxRequestBytes));

        services.AddAuthentication(DevelopmentHeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(DevelopmentHeaderAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<HttpContextPrincipal>();
        services.AddScoped<ICurrentPrincipal>(sp => sp.GetRequiredService<HttpContextPrincipal>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<HttpContextPrincipal>());

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) => new ValueTask(ProblemResults
                .From(OperationOutcome.Failure(ErrorCodes.QuotaExceeded, "Too many requests; retry later."))
                .ExecuteAsync(context.HttpContext));
            options.AddPolicy(ProjectEndpoints.RateLimitPolicy, http =>
            {
                WebLimitsOptions limits = http.RequestServices.GetRequiredService<IOptions<WebLimitsOptions>>().Value;
                string partition = $"{http.User.FindFirstValue(DevelopmentHeaderAuthenticationHandler.TenantClaim)}/{http.User.FindFirstValue(ClaimTypes.NameIdentifier)}";
                return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = limits.Window,
                    QueueLimit = 0,
                });
            });
        });

        services.AddProblemDetails();
        services.AddHealthChecks();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        });
        return services;
    }

    /// <summary>Configures the request pipeline.</summary>
    /// <param name="app">Web application.</param>
    /// <returns>The same application.</returns>
    public static WebApplication UseToolpathWeb(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseExceptionHandler();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next(context).ConfigureAwait(false);
        });
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready");
        app.MapProjectEndpoints();
        return app;
    }
}
