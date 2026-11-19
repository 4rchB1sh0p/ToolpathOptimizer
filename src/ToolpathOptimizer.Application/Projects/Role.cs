namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Tenant roles mapped from the identity provider (D-018).
/// </summary>
public enum Role
{
    /// <summary>Read only.</summary>
    Viewer,

    /// <summary>Read and annotate.</summary>
    Reviewer,

    /// <summary>Create projects, upload inputs and run analyses.</summary>
    Engineer,

    /// <summary>Everything, including delete, export and audit.</summary>
    Owner,
}
