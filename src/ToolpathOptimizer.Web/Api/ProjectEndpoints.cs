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

    /// <summary>Maps the API routes.</summary>
    /// <param name="app">Route builder.</param>
    /// <returns>The route group.</returns>
    public static RouteGroupBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("/api/v1/projects").RequireAuthorization().RequireRateLimiting(RateLimitPolicy);

        api.MapGet("/", (ProjectQueryService queries) =>
            ToHttp(queries.ListProjects(), projects => Results.Ok(projects.Select(ProjectResponse.From).ToArray())));

        api.MapPost("/", (CreateProjectRequest request, ProjectCommandService commands) =>
            ToHttp(commands.CreateProject(request.Name), project => Results.Created($"/api/v1/projects/{project.Id}", ProjectResponse.From(project))));

        api.MapPost("/{projectId:guid}/inputs", UploadInputAsync);

        api.MapPost("/{projectId:guid}/analyses", async (Guid projectId, RunAnalysisRequest request, HttpRequest http, ProjectCommandService commands, CancellationToken cancellationToken) =>
        {
            string key = http.Headers["Idempotency-Key"].ToString();
            Result<AnalysisRun> run = await commands.RunAnalysisAsync(projectId, request.InputId, key, cancellationToken).ConfigureAwait(false);
            return ToHttp(run, r => Results.Created($"/api/v1/projects/{projectId}/analyses/{r.Id}", RunResponse.From(r)));
        });

        api.MapGet("/{projectId:guid}/analyses/{runId:guid}", (Guid projectId, Guid runId, ProjectQueryService queries) =>
            ToHttp(queries.GetRun(projectId, runId), run => Results.Ok(RunResponse.From(run))));

        api.MapGet("/{projectId:guid}/analyses/{runId:guid}/report", async (Guid projectId, Guid runId, string? format, ProjectQueryService queries, CancellationToken cancellationToken) =>
        {
            if (format is not (null or "json" or "csv"))
            {
                return ProblemResults.From(OperationOutcome.Failure(ErrorCodes.InputInvalidParameter, "format must be 'json' or 'csv'."));
            }

            Result<byte[]> report = await queries.GetReportAsync(projectId, runId, cancellationToken).ConfigureAwait(false);
            return ToHttp(report, bytes => format == "csv"
                ? Results.File(ReportCsvSummaryWriter.Write(bytes), "text/csv; charset=utf-8", $"run-{runId:N}-summary.csv")
                : Results.Bytes(bytes, "application/json"));
        });

        api.MapDelete("/{projectId:guid}", async (Guid projectId, ProjectCommandService commands, CancellationToken cancellationToken) =>
        {
            OperationOutcome deleted = await commands.DeleteProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
            return deleted.IsSuccess ? Results.NoContent() : ProblemResults.From(deleted);
        });

        api.MapGet("/{projectId:guid}/export", (Guid projectId, ProjectQueryService queries) =>
            ToHttp(queries.ExportProject(projectId), export => Results.Ok(new ExportResponse
            {
                Project = ProjectResponse.From(export.Project),
                Inputs = export.Inputs.Select(InputResponse.From).ToArray(),
                Runs = export.Runs.Select(RunResponse.From).ToArray(),
            })));

        return api;
    }

    // Reads the multipart form explicitly (no IFormFile binding): the API is header-authenticated, so there is
    // no ambient cookie credential for antiforgery to protect, and explicit reading lets limits map to 413.
    private static async Task<IResult> UploadInputAsync(Guid projectId, HttpContext context, ProjectCommandService commands, IOptions<WebLimitsOptions> limits, CancellationToken cancellationToken)
    {
        long maxBytes = limits.Value.MaxRequestBytes;
        if (context.Request.ContentLength > maxBytes)
        {
            return TooLarge(maxBytes);
        }

        if (!context.Request.HasFormContentType)
        {
            return ProblemResults.From(OperationOutcome.Failure(ErrorCodes.InputMissingField, "Upload multipart/form-data with 'points' and 'manifest' files."));
        }

        IFormCollection form;
        try
        {
            form = await context.Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            return TooLarge(maxBytes);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return TooLarge(maxBytes);
        }

        IFormFile? points = form.Files.GetFile("points");
        IFormFile? manifest = form.Files.GetFile("manifest");
        if (points is null || manifest is null)
        {
            return ProblemResults.From(OperationOutcome.Failure(ErrorCodes.InputMissingField, "Both 'points' and 'manifest' files are required."));
        }

        var input = new JobInput(await ReadAllAsync(points, cancellationToken).ConfigureAwait(false), await ReadAllAsync(manifest, cancellationToken).ConfigureAwait(false));
        Result<InputArtifact> stored = await commands.UploadInputAsync(projectId, input, cancellationToken).ConfigureAwait(false);
        return ToHttp(stored, artifact => Results.Created($"/api/v1/projects/{projectId}/inputs/{artifact.Id}", InputResponse.From(artifact)));
    }

    private static async Task<byte[]> ReadAllAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static IResult TooLarge(long maxBytes) =>
        ProblemResults.From(OperationOutcome.Failure(ErrorCodes.InputLimitExceeded, $"Request body exceeds {maxBytes} bytes."));

    private static IResult ToHttp<T>(Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : ProblemResults.From(result.Outcome);
}
