using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Reads and writes a project's input and report bytes in the <see cref="IArtifactStore"/>, owning the key layout.
/// </summary>
public sealed class ProjectArtifacts
{
    private readonly IArtifactStore _store;

    /// <summary>Creates the helper.</summary>
    /// <param name="store">Tenant-scoped artifact store.</param>
    public ProjectArtifacts(IArtifactStore store)
    {
        _store = store;
    }

    /// <summary>Stores both input files of an artifact.</summary>
    /// <param name="artifact">Input record that supplies tenant and keys.</param>
    /// <param name="input">Raw input bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when both files are stored.</returns>
    public async Task SaveInputAsync(InputArtifact artifact, JobInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(input);
        await _store.PutAsync(artifact.TenantId, artifact.PointsKey, input.PointsCsv, cancellationToken).ConfigureAwait(false);
        await _store.PutAsync(artifact.TenantId, artifact.ManifestKey, input.ManifestJson, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads both input files of an artifact.</summary>
    /// <param name="artifact">Input record that supplies tenant and keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw inputs, or <c>IO_READ_FAILED</c> when either file is missing.</returns>
    public async Task<Result<JobInput>> LoadInputAsync(InputArtifact artifact, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        Result<byte[]> points = await _store.GetAsync(artifact.TenantId, artifact.PointsKey, cancellationToken).ConfigureAwait(false);
        Result<byte[]> manifest = await _store.GetAsync(artifact.TenantId, artifact.ManifestKey, cancellationToken).ConfigureAwait(false);
        return points.IsSuccess && manifest.IsSuccess
            ? Result<JobInput>.Success(new JobInput(points.Value, manifest.Value))
            : Result<JobInput>.Failure(ErrorCodes.IoReadFailed, "Stored input artifact is missing.");
    }

    /// <summary>Stores a run's report.</summary>
    /// <param name="run">Run record that supplies tenant and key.</param>
    /// <param name="content">Report bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when stored.</returns>
    public Task SaveReportAsync(AnalysisRun run, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        return _store.PutAsync(run.TenantId, run.ReportKey, content, cancellationToken);
    }

    /// <summary>Loads a run's report.</summary>
    /// <param name="run">Run record that supplies tenant and key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The report bytes, or <c>RESOURCE_NOT_FOUND</c>.</returns>
    public Task<Result<byte[]>> LoadReportAsync(AnalysisRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        return _store.GetAsync(run.TenantId, run.ReportKey, cancellationToken);
    }

    /// <summary>Deletes every artifact of a project.</summary>
    /// <param name="project">Project whose inputs and reports are deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deleted.</returns>
    public Task DeleteProjectAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        return _store.DeletePrefixAsync(project.TenantId, $"projects/{project.Id:N}/", cancellationToken);
    }
}
