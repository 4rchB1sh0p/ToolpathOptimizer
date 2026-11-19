using System.Net;
using Microsoft.AspNetCore.Hosting;
using ToolpathOptimizer.Web.IntegrationTests.Support;

namespace ToolpathOptimizer.Web.IntegrationTests;

/// <summary>Host-level behaviour: health, static UI headers, request limits and fail-closed start-up.</summary>
public sealed class HostingTests
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_RunningHost_ReturnsOk(string path)
    {
        using var factory = new ToolpathWebFactory();

        HttpResponseMessage response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Index_AnyRequest_SendsStrictContentSecurityPolicy()
    {
        using var factory = new ToolpathWebFactory();

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Index_AnyRequest_SendsNoSniff()
    {
        using var factory = new ToolpathWebFactory();

        HttpResponseMessage response = await factory.CreateClient().GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task UploadInput_OversizedBody_ReturnsPayloadTooLargeProblem()
    {
        using var factory = new ToolpathWebFactory(new Dictionary<string, string> { ["WebLimits:MaxRequestBytes"] = "512" });
        HttpClient client = factory.CreateClientFor("tenant-x", "eng", "Engineer");
        string projectId = await client.CreateProjectAsync();

        HttpResponseMessage response = await client.UploadAsync(projectId, new byte[2048], new byte[16]);

        Assert.Equal((HttpStatusCode.RequestEntityTooLarge, "INPUT_LIMIT_EXCEEDED"), (response.StatusCode, await response.ReadProblemCodeAsync()));
    }

    [Fact]
    public async Task ListProjects_OverRateLimit_ReturnsTooManyRequestsProblem()
    {
        using var factory = new ToolpathWebFactory(new Dictionary<string, string> { ["WebLimits:PermitLimit"] = "2" });
        HttpClient client = factory.CreateClientFor("tenant-x", "u1", "Viewer");
        await client.GetAsync("/api/v1/projects");
        await client.GetAsync("/api/v1/projects");

        HttpResponseMessage limited = await client.GetAsync("/api/v1/projects");

        Assert.Equal((HttpStatusCode.TooManyRequests, "QUOTA_EXCEEDED"), (limited.StatusCode, await limited.ReadProblemCodeAsync()));
    }

    [Fact]
    public async Task ListProjects_OtherUserUnderRateLimit_ReturnsOk()
    {
        using var factory = new ToolpathWebFactory(new Dictionary<string, string> { ["WebLimits:PermitLimit"] = "2" });
        HttpClient busy = factory.CreateClientFor("tenant-x", "u1", "Viewer");
        await busy.GetAsync("/api/v1/projects");
        await busy.GetAsync("/api/v1/projects");
        await busy.GetAsync("/api/v1/projects");

        HttpResponseMessage otherUser = await factory.CreateClientFor("tenant-x", "u2", "Viewer").GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
    }

    [Fact]
    public void Start_NonDevelopmentEnvironment_FailsWithoutVerifiedIdentityAdapter()
    {
        using var factory = new ToolpathWebFactory();
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        Exception error = Assert.ThrowsAny<Exception>(() => production.CreateClient());

        Assert.Contains("identity adapter", FlattenMessages(error), StringComparison.Ordinal);
    }

    private static string FlattenMessages(Exception error) =>
        error.InnerException is null ? error.Message : error.Message + " | " + FlattenMessages(error.InnerException);
}
