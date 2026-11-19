using System.Net;
using System.Text.Json;
using ToolpathOptimizer.Web.IntegrationTests.Support;

namespace ToolpathOptimizer.Web.IntegrationTests;

/// <summary>Cross-tenant requests must return 404 for every endpoint, even for an Owner of another tenant.</summary>
public sealed class TenantIsolationTests : IClassFixture<ToolpathWebFactory>
{
    private readonly ToolpathWebFactory _factory;

    /// <summary>Creates the tests with a shared host.</summary>
    /// <param name="factory">Shared web host.</param>
    public TenantIsolationTests(ToolpathWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Gets one request per tenant-scoped endpoint, built from the seeded identifiers.</summary>
    public static TheoryData<string> CrossTenantRequests { get; } = new(["get-run", "get-report", "run-analysis", "export", "delete"]);

    private static Task<HttpResponseMessage> SendAsync(HttpClient intruder, SeededRun seeded, string request) => request switch
    {
        "get-run" => intruder.GetAsync($"/api/v1/projects/{seeded.ProjectId}/analyses/{seeded.RunId}"),
        "get-report" => intruder.GetAsync($"/api/v1/projects/{seeded.ProjectId}/analyses/{seeded.RunId}/report"),
        "run-analysis" => intruder.RunAnalysisAsync(seeded.ProjectId, seeded.InputId),
        "export" => intruder.GetAsync($"/api/v1/projects/{seeded.ProjectId}/export"),
        _ => intruder.DeleteAsync($"/api/v1/projects/{seeded.ProjectId}"),
    };

    [Theory]
    [MemberData(nameof(CrossTenantRequests))]
    public async Task Endpoint_OtherTenantOwner_ReturnsNotFound(string request)
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);
        HttpClient intruder = _factory.CreateClientFor(SeededRun.CreateUniqueTenant(), "intruder", "Owner");

        HttpResponseMessage response = await SendAsync(intruder, seeded, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListProjects_OtherTenant_IsEmpty()
    {
        await SeededRun.CreateAsync(_factory);
        HttpClient intruder = _factory.CreateClientFor(SeededRun.CreateUniqueTenant(), "intruder", "Owner");

        JsonElement projects = await (await intruder.GetAsync("/api/v1/projects")).ReadJsonAsync();

        Assert.Equal(0, projects.GetArrayLength());
    }
}
