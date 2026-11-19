using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Api;

/// <summary>
/// Versioned project API (web-and-tenancy.md). Endpoints are thin: they translate HTTP to Application services.
/// </summary>
public static class ProjectEndpoints
{
    /// <summary>Rate-limit policy name applied to the API.</summary>
    public const string RateLimitPolicy = "per-user";

    private const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>Maps the API routes.</summary>
    /// <param name="app">Route builder.</param>
    /// <returns>The route group.</returns>
    public static RouteGroupBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("/api/v1/projects").RequireAuthorization().RequireRateLimiting(RateLimitPolicy);
        api.MapGet("/", ListProjects);
        api.MapPost("/", CreateProject);
        api.MapPost("/{projectId:guid}/inputs", UploadInputAsync);
        api.MapPost("/{projectId:guid}/analyses", RunAnalysisAsync);
        api.MapGet("/{projectId:guid}/analyses/{runId:guid}", GetRun);
        api.MapGet("/{projectId:guid}/analyses/{runId:guid}/report", GetReportAsync);
        api.MapDelete("/{projectId:guid}", DeleteProjectAsync);
        api.MapGet("/{projectId:guid}/export", ExportProject);
        return api;
    }

    private static IResult ListProjects(ProjectQueryService queries) =>
        ToHttp(queries.ListProjects(), projects => Results.Ok(projects.Select(ProjectResponse.From).ToArray()));

    private static IResult CreateProject(CreateProjectRequest request, ProjectCommandService commands) =>
        ToHttp(commands.CreateProject(request.Name), project => Results.Created($"/api/v1/projects/{project.Id}", ProjectResponse.From(project)));

    private static async Task<IResult> RunAnalysisAsync(Guid projectId, RunAnalysisRequest request, HttpRequest http, AnalysisRunService runs, CancellationToken cancellationToken)
    {
        string idempotencyKey = http.Headers[IdempotencyKeyHeader].ToString();
        Result<AnalysisRun> run = await runs.RunAsync(projectId, request.InputId, idempotencyKey, cancellationToken).ConfigureAwait(false);
        return ToHttp(run, created => Results.Created($"/api/v1/projects/{projectId}/analyses/{created.Id}", RunResponse.From(created)));
    }

    private static IResult GetRun(Guid projectId, Guid runId, ProjectQueryService queries) =>
        ToHttp(queries.GetRun(projectId, runId), run => Results.Ok(RunResponse.From(run)));

    private static async Task<IResult> GetReportAsync(Guid projectId, Guid runId, string? format, ProjectQueryService queries, CancellationToken cancellationToken)
    {
        bool isCsv = format == "csv";
        if (!isCsv && format is not (null or "json"))
        {
            return ProblemResults.From(OperationOutcome.Failure(ErrorCodes.InputInvalidParameter, "format must be 'json' or 'csv'."));
        }

        Result<byte[]> report = await queries.GetReportAsync(projectId, runId, cancellationToken).ConfigureAwait(false);
        return ToHttp(report, bytes => isCsv
            ? Results.File(ReportCsvSummaryWriter.Write(bytes), "text/csv; charset=utf-8", $"run-{runId:N}-summary.csv")
            : Results.Bytes(bytes, "application/json"));
    }

    private static async Task<IResult> DeleteProjectAsync(Guid projectId, ProjectCommandService commands, CancellationToken cancellationToken)
    {
        OperationOutcome deleted = await commands.DeleteProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
        return deleted.IsSuccess ? Results.NoContent() : ProblemResults.From(deleted);
    }

    private static IResult ExportProject(Guid projectId, ProjectQueryService queries) =>
        ToHttp(queries.ExportProject(projectId), export => Results.Ok(ExportResponse.From(export)));

    // Reads the multipart form explicitly (no IFormFile binding): the API is header-authenticated, so there is
    // no ambient cookie credential for antiforgery to protect (D-024), and explicit reading maps limits to 413.
    private static async Task<IResult> UploadInputAsync(Guid projectId, HttpContext context, InputUploadService uploads, IOptions<WebLimitsOptions> limits, CancellationToken cancellationToken)
    {
        Result<IFormCollection> form = await ReadFormAsync(context.Request, limits.Value.MaxRequestBytes, cancellationToken).ConfigureAwait(false);
        if (!form.IsSuccess)
        {
            return ProblemResults.From(form.Outcome);
        }

        IFormFile? points = form.Value.Files.GetFile("points");
        IFormFile? manifest = form.Value.Files.GetFile("manifest");
        if (points is null || manifest is null)
        {
            return ProblemResults.From(OperationOutcome.Failure(ErrorCodes.InputMissingField, "Both 'points' and 'manifest' files are required."));
        }

        var input = new JobInput(await ReadAllBytesAsync(points, cancellationToken).ConfigureAwait(false), await ReadAllBytesAsync(manifest, cancellationToken).ConfigureAwait(false));
        Result<InputArtifact> stored = await uploads.UploadAsync(projectId, input, cancellationToken).ConfigureAwait(false);
        return ToHttp(stored, artifact => Results.Created($"/api/v1/projects/{projectId}/inputs/{artifact.Id}", InputResponse.From(artifact)));
    }

    private static async Task<Result<IFormCollection>> ReadFormAsync(HttpRequest request, long maxBytes, CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxBytes)
        {
            return TooLarge(maxBytes);
        }

        if (!request.HasFormContentType)
        {
            return Result<IFormCollection>.Failure(ErrorCodes.InputMissingField, "Upload multipart/form-data with 'points' and 'manifest' files.");
        }

        try
        {
            return Result<IFormCollection>.Success(await request.ReadFormAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidDataException)
        {
            return TooLarge(maxBytes);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return TooLarge(maxBytes);
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static Result<IFormCollection> TooLarge(long maxBytes) =>
        Result<IFormCollection>.Failure(ErrorCodes.InputLimitExceeded, $"Request body exceeds {maxBytes} bytes.");

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : ProblemResults.From(result.Outcome);
}
