namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// A project with its inputs and runs (the export view; report bytes are fetched separately per run).
/// </summary>
public sealed class ProjectExport
{
    /// <summary>Creates an export.</summary>
    /// <param name="project">Exported project.</param>
    /// <param name="inputs">The project's inputs in upload order.</param>
    /// <param name="runs">The project's runs in creation order.</param>
    public ProjectExport(Project project, IReadOnlyList<InputArtifact> inputs, IReadOnlyList<AnalysisRun> runs)
    {
        Project = project;
        Inputs = inputs.ToArray();
        Runs = runs.ToArray();
    }

    /// <summary>Gets the exported project.</summary>
    public Project Project { get; }

    /// <summary>Gets the project's inputs in upload order.</summary>
    public IReadOnlyList<InputArtifact> Inputs { get; }

    /// <summary>Gets the project's runs in creation order.</summary>
    public IReadOnlyList<AnalysisRun> Runs { get; }
}
