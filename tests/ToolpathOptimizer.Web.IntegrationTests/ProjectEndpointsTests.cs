using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Web.IntegrationTests.Support;

namespace ToolpathOptimizer.Web.IntegrationTests;

/// <summary>End-to-end tests of the project API: report parity, idempotency, problem details and authentication.</summary>
public sealed class ProjectEndpointsTests : IClassFixture<ToolpathWebFactory>
{
    private readonly ToolpathWebFactory _factory;

    /// <summary>Creates the tests with a shared host.</summary>
    /// <param name="factory">Shared web host.</param>
    public ProjectEndpointsTests(ToolpathWebFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateEngineer() => _factory.CreateClientFor(SeededRun.CreateUniqueTenant(), "eng", "Engineer");

    private string ReportUrl(SeededRun seeded) => $"/api/v1/projects/{seeded.ProjectId}/analyses/{seeded.RunId}/report";

    [Fact]
    public async Task RunAnalysis_PlateSample_SelectsCandidate()
    {
        HttpClient engineer = CreateEngineer();
        string projectId = await engineer.CreateProjectAsync();

        JsonElement run = await (await engineer.RunAnalysisAsync(projectId, await engineer.UploadPlateSampleAsync(projectId))).ReadJsonAsync();

        Assert.Equal(("succeeded", "candidate"), (run.GetProperty("status").GetString(), run.GetProperty("summary").GetProperty("selected_route").GetString()));
    }

    [Fact]
    public async Task GetReport_PlateSample_MatchesSharedHandlerBytes()
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);
        byte[] webReport = await _factory.CreateClientFor(seeded.Tenant, "eng", "Engineer").GetByteArrayAsync(ReportUrl(seeded));

        var input = new JobInput(await File.ReadAllBytesAsync(ApiClientExtensions.SamplePath("plate.csv")), await File.ReadAllBytesAsync(ApiClientExtensions.SamplePath("plate-job.json")));
        byte[] handlerReport = _factory.Services.GetRequiredService<AnalyzeJobHandler>().Handle(input, CancellationToken.None).Value.Content;

        Assert.Equal(handlerReport, webReport);
    }

    [Fact]
    public async Task GetReport_CsvFormat_ReturnsSummaryCsv()
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);

        HttpResponseMessage csv = await _factory.CreateClientFor(seeded.Tenant, "eng", "Engineer").GetAsync(ReportUrl(seeded) + "?format=csv");

        Assert.StartsWith("position,point_id,x_mm,y_mm,operation_group", await csv.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetReport_UnknownFormat_ReturnsBadRequest()
    {
        SeededRun seeded = await SeededRun.CreateAsync(_factory);

        HttpResponseMessage response = await _factory.CreateClientFor(seeded.Tenant, "eng", "Engineer").GetAsync(ReportUrl(seeded) + "?format=xml");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RunAnalysis_RepeatedIdempotencyKey_ReturnsSameRun()
    {
        HttpClient engineer = CreateEngineer();
        string projectId = await engineer.CreateProjectAsync();
        string inputId = await engineer.UploadPlateSampleAsync(projectId);

        string first = (await (await engineer.RunAnalysisAsync(projectId, inputId, "same")).ReadJsonAsync()).GetProperty("id").GetString()!;
        string second = (await (await engineer.RunAnalysisAsync(projectId, inputId, "same")).ReadJsonAsync()).GetProperty("id").GetString()!;

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task UploadInput_NonFiniteCoordinate_ReturnsBadRequestProblem()
    {
        HttpClient engineer = CreateEngineer();
        string projectId = await engineer.CreateProjectAsync();
        byte[] csv = Encoding.UTF8.GetBytes("point_id,x_mm,y_mm,sequence,operation_group\nA,NaN,0,1,\n");

        HttpResponseMessage response = await engineer.UploadAsync(projectId, csv, await File.ReadAllBytesAsync(ApiClientExtensions.SamplePath("job.json")));

        Assert.Equal((HttpStatusCode.BadRequest, "application/problem+json", "INPUT_NON_FINITE_COORDINATE"),
            (response.StatusCode, response.Content.Headers.ContentType?.MediaType, await response.ReadProblemCodeAsync()));
    }

    [Fact]
    public async Task UploadInput_CyclicConstraints_ReturnsUnprocessableEntity()
    {
        HttpClient engineer = CreateEngineer();
        string projectId = await engineer.CreateProjectAsync();
        byte[] manifest = Encoding.UTF8.GetBytes("""{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[{"before":"H01","after":"H02"},{"before":"H02","after":"H01"}]},"algorithm":{"name":"nearest-neighbor-2opt"}}""");

        HttpResponseMessage response = await engineer.UploadAsync(projectId, await File.ReadAllBytesAsync(ApiClientExtensions.SamplePath("holes.csv")), manifest);

        Assert.Equal((HttpStatusCode.UnprocessableEntity, "CONSTRAINT_CYCLE"), (response.StatusCode, await response.ReadProblemCodeAsync()));
    }

    [Fact]
    public async Task ListProjects_NoIdentity_ReturnsUnauthorizedProblem()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/projects");

        Assert.Equal((HttpStatusCode.Unauthorized, "UNAUTHENTICATED"), (response.StatusCode, await response.ReadProblemCodeAsync()));
    }

    [Theory]
    [InlineData("bad tenant", "u", "Owner")]
    [InlineData("tenant-x", "u", "Admin")]
    [InlineData("tenant-x", "u", "3")]
    public async Task ListProjects_InvalidIdentityHeaders_ReturnsUnauthorized(string tenant, string user, string role)
    {
        HttpResponseMessage response = await _factory.CreateClientFor(tenant, user, role).GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_TenantInBody_IsIgnored()
    {
        string otherTenant = SeededRun.CreateUniqueTenant();
        HttpClient caller = CreateEngineer();

        await caller.PostAsJsonAsync("/api/v1/projects", new { name = "Spoof", tenant_id = otherTenant });
        JsonElement otherTenantProjects = await (await _factory.CreateClientFor(otherTenant, "eng", "Engineer").GetAsync("/api/v1/projects")).ReadJsonAsync();

        Assert.Equal(0, otherTenantProjects.GetArrayLength());
    }
}
