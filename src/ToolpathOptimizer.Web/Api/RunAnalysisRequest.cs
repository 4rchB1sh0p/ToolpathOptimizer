namespace ToolpathOptimizer.Web.Api;

/// <summary>Body of <c>POST /api/v1/projects/{projectId}/analyses</c>.</summary>
public sealed class RunAnalysisRequest
{
    /// <summary>Gets or sets the uploaded input to analyse.</summary>
    public Guid InputId { get; set; }
}
