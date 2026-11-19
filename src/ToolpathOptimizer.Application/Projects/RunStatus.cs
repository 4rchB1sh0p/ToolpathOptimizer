namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Terminal state of an analysis run (runs are synchronous in the MVP).
/// </summary>
public enum RunStatus
{
    /// <summary>A report was produced.</summary>
    Succeeded,

    /// <summary>The run failed; see the outcome code.</summary>
    Failed,
}
