using System.Net;
using System.Net.Http.Json;
using ToolpathOptimizer.Web.IntegrationTests.Support;

namespace ToolpathOptimizer.Web.IntegrationTests;

/// <summary>Same-tenant requests denied by the role matrix must return 403 problem details.</summary>
public sealed class RolePermissionEndpointTests : IClassFixture<ToolpathWebFactory>
{
    private readonly ToolpathWebFactory _factory;

    /// <summary>Creates the tests with a shared host.</summary>
    /// <param name="factory">Shared web host.</param>
    public RolePermissionEndpointTests(ToolpathWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Gets (role, request) pairs the matrix denies.</summary>
    public static TheoryData<string, string> DeniedRequests { get; } = new()
    {
        { "Viewer", "create" }, { "Viewer", "upload" }, { "Viewer", "run" },
        { "Reviewer", "create" }, { "Reviewer", "upload" }, { "Reviewer", "run" },
        { "Engineer", "export" }, { "Engineer", "delete" },
    };

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, SeededRun seeded, string request) => request switch
    {
        "create" => client.PostAsJsonAsync("/api/v1/projects", new { name = "x" }),
        "upload" => client.UploadAsync(seeded.ProjectId, [1], [1]),
        "run" => client.RunAnalysisAsync(seeded.ProjectId, seeded.InputId),
        "export" => client.GetAsync($"/api/v1/projects/{seeded.ProjectId}/export"),
        _ => client.DeleteAsync($"/api/v1/projects/{seeded.ProjectId}"),
    };

    [Theory]
    [MemberData(nameof(DeniedRequests))]
    public async Task Endpoint_RoleWithoutPermission_ReturnsForbidden(string role, string request)
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);

        HttpResponseMessage response = await SendAsync(_factory.CreateClientFor(seeded.Tenant, "user-2", role), seeded, request);

        Assert.Equal((HttpStatusCode.Forbidden, "ACCESS_DENIED"), (response.StatusCode, await response.ReadProblemCodeAsync()));
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("Reviewer")]
    public async Task ListProjects_ReadOnlyRole_ReturnsOk(string role)
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);

        HttpResponseMessage response = await _factory.CreateClientFor(seeded.Tenant, "user-2", role).GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_Owner_ReturnsNoContentThenNotFound()
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);
        HttpClient owner = _factory.CreateClientFor(seeded.Tenant, "owner", "Owner");

        HttpResponseMessage deleted = await owner.DeleteAsync($"/api/v1/projects/{seeded.ProjectId}");
        HttpResponseMessage afterwards = await owner.GetAsync($"/api/v1/projects/{seeded.ProjectId}/export");

        Assert.Equal((HttpStatusCode.NoContent, HttpStatusCode.NotFound), (deleted.StatusCode, afterwards.StatusCode));
    }
}
