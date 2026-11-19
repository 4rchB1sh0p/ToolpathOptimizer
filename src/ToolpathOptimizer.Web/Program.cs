using ToolpathOptimizer.Application;
using ToolpathOptimizer.Infrastructure;
using ToolpathOptimizer.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Fail closed: only the Development header identity exists until plan 0004 verifies a real provider (TD-004).
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "No verified identity adapter is configured (plan 0004, TD-004). The web host only runs in the Development environment.");
}

builder.Services.AddToolpathApplication();
builder.Services.AddToolpathInfrastructure(builder.Configuration);
builder.Services.AddToolpathWeb(builder.Configuration);
builder.WebHost.ConfigureKestrel((context, kestrel) =>
    kestrel.Limits.MaxRequestBodySize = context.Configuration.GetValue(WebLimitsOptions.SectionName + ":MaxRequestBytes", WebLimitsOptions.DefaultMaxRequestBytes));

WebApplication app = builder.Build();
app.UseToolpathWeb();
app.Run();

/// <summary>
/// Entry point; partial so integration tests can reference it from <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
