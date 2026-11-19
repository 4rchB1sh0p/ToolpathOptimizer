using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ToolpathOptimizer.Application.Analysis;

namespace ToolpathOptimizer.Web.IntegrationTests;

public sealed class ProjectEndpointsTests : IClassFixture<ToolpathWebFactory>
{
    private readonly ToolpathWebFactory _factory;

    public ProjectEndpointsTests(ToolpathWebFactory factory)
    {
        _factory = factory;
    }

    private static string NewTenant() => "t" + Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task FullFlow_ProducesReportIdenticalToSharedHandler()
    {
        HttpClient engineer = _factory.ClientFor(NewTenant(), "eng", "Engineer");
        string projectId = await engineer.CreateProjectAsync();
        string inputId = await engineer.UploadSampleAsync(projectId);

        HttpResponseMessage runResponse = await engineer.RunAsync(projectId, inputId, "run-1");
        JsonElement run = await runResponse.JsonAsync();
        byte[] webReport = await engineer.GetByteArrayAsync(run.GetProperty("report_url").GetString());

        Assert.Equal(HttpStatusCode.Created, runResponse.StatusCode);
        Assert.Equal("succeeded", run.GetProperty("status").GetString());
        Assert.Equal("candidate", run.GetProperty("summary").GetProperty("selected_route").GetString());

        var input = new JobInput(await File.ReadAllBytesAsync(Api.Sample("plate.csv")), await File.ReadAllBytesAsync(Api.Sample("plate-job.json")));
        byte[] handlerReport = _factory.Services.GetRequiredService<AnalyzeJobHandler>().Handle(input, CancellationToken.None).Value.Content;
        Assert.Equal(handlerReport, webReport);
    }

    [Fact]
    public async Task Report_CsvSummaryIsAvailable()
    {
        HttpClient engineer = _factory.ClientFor(NewTenant(), "eng", "Engineer");
        string projectId = await engineer.CreateProjectAsync();
        JsonElement run = await (await engineer.RunAsync(projectId, await engineer.UploadSampleAsync(projectId))).JsonAsync();

        HttpResponseMessage csv = await engineer.GetAsync(run.GetProperty("report_url").GetString() + "?format=csv");

        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("position,point_id,x_mm,y_mm,operation_group", await csv.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepeatedIdempotencyKey_ReturnsSameRun()
    {
        HttpClient engineer = _factory.ClientFor(NewTenant(), "eng", "Engineer");
        string projectId = await engineer.CreateProjectAsync();
        string inputId = await engineer.UploadSampleAsync(projectId);

        string first = (await (await engineer.RunAsync(projectId, inputId, "same")).JsonAsync()).GetProperty("id").GetString()!;
        string second = (await (await engineer.RunAsync(projectId, inputId, "same")).JsonAsync()).GetProperty("id").GetString()!;

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task CrossTenantAccess_ReturnsNotFound()
    {
        HttpClient owner = _factory.ClientFor(NewTenant(), "eng", "Owner");
        HttpClient intruder = _factory.ClientFor(NewTenant(), "eng", "Owner");
        string projectId = await owner.CreateProjectAsync();
        string inputId = await owner.UploadSampleAsync(projectId);
        string runId = (await (await owner.RunAsync(projectId, inputId)).JsonAsync()).GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/v1/projects/{projectId}/analyses/{runId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/v1/projects/{projectId}/analyses/{runId}/report")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.RunAsync(projectId, inputId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/v1/projects/{projectId}")).StatusCode);
        Assert.Equal(0, (await (await intruder.GetAsync("/api/v1/projects")).JsonAsync()).GetArrayLength());
    }

    [Fact]
    public async Task TenantInBody_IsIgnored()
    {
        string tenantA = NewTenant();
        string tenantB = NewTenant();
        HttpClient clientA = _factory.ClientFor(tenantA, "eng", "Engineer");
        HttpClient clientB = _factory.ClientFor(tenantB, "eng", "Engineer");

        HttpResponseMessage created = await clientA.PostAsJsonAsync("/api/v1/projects", new { name = "Spoof", tenant_id = tenantB });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(1, (await (await clientA.GetAsync("/api/v1/projects")).JsonAsync()).GetArrayLength());
        Assert.Equal(0, (await (await clientB.GetAsync("/api/v1/projects")).JsonAsync()).GetArrayLength());
    }

    [Theory]
    [InlineData("Viewer")]
    [InlineData("Reviewer")]
    public async Task ReadOnlyRoles_CannotCreateUploadOrRun(string role)
    {
        string tenant = NewTenant();
        HttpClient engineer = _factory.ClientFor(tenant, "eng", "Engineer");
        HttpClient readOnly = _factory.ClientFor(tenant, "ro", role);
        string projectId = await engineer.CreateProjectAsync();
        string inputId = await engineer.UploadSampleAsync(projectId);

        HttpResponseMessage create = await readOnly.PostAsJsonAsync("/api/v1/projects", new { name = "x" });
        HttpResponseMessage upload = await readOnly.UploadAsync(projectId, [1], [1]);
        HttpResponseMessage run = await readOnly.RunAsync(projectId, inputId);
        HttpResponseMessage read = await readOnly.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, run.StatusCode);
        Assert.Equal("ACCESS_DENIED", (await run.JsonAsync()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task OnlyOwner_CanExportAndDelete()
    {
        string tenant = NewTenant();
        HttpClient engineer = _factory.ClientFor(tenant, "eng", "Engineer");
        HttpClient owner = _factory.ClientFor(tenant, "own", "Owner");
        string projectId = await engineer.CreateProjectAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await engineer.GetAsync($"/api/v1/projects/{projectId}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await engineer.DeleteAsync($"/api/v1/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/projects/{projectId}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/v1/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/v1/projects/{projectId}/export")).StatusCode);
    }

    [Fact]
    public async Task InvalidInput_ReturnsProblemDetailsWithStableCode()
    {
        HttpClient engineer = _factory.ClientFor(NewTenant(), "eng", "Engineer");
        string projectId = await engineer.CreateProjectAsync();

        HttpResponseMessage response = await engineer.UploadAsync(projectId, Encoding.UTF8.GetBytes("point_id,x_mm,y_mm,sequence,operation_group\nA,NaN,0,1,\n"), await File.ReadAllBytesAsync(Api.Sample("job.json")));
        JsonElement problem = await response.JsonAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("INPUT_NON_FINITE_COORDINATE", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InfeasibleConstraints_Return422()
    {
        HttpClient engineer = _factory.ClientFor(NewTenant(), "eng", "Engineer");
        string projectId = await engineer.CreateProjectAsync();
        byte[] manifest = Encoding.UTF8.GetBytes("""{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"constraints":{"precedence":[{"before":"H01","after":"H02"},{"before":"H02","after":"H01"}]},"algorithm":{"name":"nearest-neighbor-2opt"}}""");

        HttpResponseMessage response = await engineer.UploadAsync(projectId, await File.ReadAllBytesAsync(Api.Sample("holes.csv")), manifest);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("CONSTRAINT_CYCLE", (await response.JsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task MissingIdentity_Returns401Problem()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", (await response.JsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvalidIdentityHeaders_Return401()
    {
        HttpResponseMessage response = await _factory.ClientFor("bad tenant", "u", "Owner").GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
