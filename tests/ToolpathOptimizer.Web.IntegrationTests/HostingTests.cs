using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace ToolpathOptimizer.Web.IntegrationTests;

/// <summary>Host-level behaviour: health, static UI headers, limits and fail-closed start-up.</summary>
public sealed class HostingTests
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReturnOk(string path)
    {
        using var factory = new ToolpathWebFactory();

        HttpResponseMessage response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Index_IsServedWithStrictSecurityHeaders()
    {
        using var factory = new ToolpathWebFactory();

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task OversizedUpload_Returns413Problem()
    {
        using var factory = new ToolpathWebFactory(new Dictionary<string, string> { ["WebLimits:MaxRequestBytes"] = "512" });
        HttpClient client = factory.ClientFor("tenant-x", "eng", "Engineer");
        string projectId = await client.CreateProjectAsync();

        HttpResponseMessage response = await client.UploadAsync(projectId, new byte[2048], new byte[16]);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("INPUT_LIMIT_EXCEEDED", (await response.JsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task RateLimit_Returns429ProblemPerUser()
    {
        using var factory = new ToolpathWebFactory(new Dictionary<string, string> { ["WebLimits:PermitLimit"] = "2" });
        HttpClient first = factory.ClientFor("tenant-x", "u1", "Viewer");
        HttpClient second = factory.ClientFor("tenant-x", "u2", "Viewer");

        await first.GetAsync("/api/v1/projects");
        await first.GetAsync("/api/v1/projects");
        HttpResponseMessage limited = await first.GetAsync("/api/v1/projects");
        HttpResponseMessage otherUser = await second.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("QUOTA_EXCEEDED", (await limited.JsonAsync()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
    }

    [Fact]
    public void NonDevelopmentStart_FailsWithoutVerifiedIdentityAdapter()
    {
        using var factory = new ToolpathWebFactory();
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        Exception error = Assert.ThrowsAny<Exception>(() => production.CreateClient());

        Assert.Contains("identity adapter", FlattenMessages(error), StringComparison.Ordinal);
    }

    private static string FlattenMessages(Exception error) =>
        error.InnerException is null ? error.Message : error.Message + " | " + FlattenMessages(error.InnerException);
}
