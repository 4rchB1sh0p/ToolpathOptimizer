namespace ToolpathOptimizer.Web.IntegrationTests.Support;

/// <summary>Identifiers of a project with one uploaded input and one succeeded run, owned by <see cref="Tenant"/>.</summary>
internal sealed class SeededRun
{
    /// <summary>Creates the seeded identifiers.</summary>
    /// <param name="tenant">Owning tenant.</param>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="inputId">Input identifier.</param>
    /// <param name="runId">Run identifier.</param>
    public SeededRun(string tenant, string projectId, string inputId, string runId)
    {
        Tenant = tenant;
        ProjectId = projectId;
        InputId = inputId;
        RunId = runId;
    }

    /// <summary>Gets the owning tenant.</summary>
    public string Tenant { get; }

    /// <summary>Gets the project identifier.</summary>
    public string ProjectId { get; }

    /// <summary>Gets the input identifier.</summary>
    public string InputId { get; }

    /// <summary>Gets the run identifier.</summary>
    public string RunId { get; }

    /// <summary>Creates a fresh tenant name so tests never share data.</summary>
    /// <returns>A unique, valid tenant identifier.</returns>
    public static string CreateUniqueTenant() => "t" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>Seeds a project, input and run as an Engineer in a new tenant.</summary>
    /// <param name="factory">Web host.</param>
    /// <returns>The seeded identifiers.</returns>
    public static async Task<SeededRun> CreateAsync(ToolpathWebFactory factory)
    {
        string tenant = CreateUniqueTenant();
        HttpClient engineer = factory.CreateClientFor(tenant, "eng", "Engineer");
        string projectId = await engineer.CreateProjectAsync();
        string inputId = await engineer.UploadPlateSampleAsync(projectId);
        HttpResponseMessage run = await engineer.RunAnalysisAsync(projectId, inputId);
        run.EnsureSuccessStatusCode();
        return new SeededRun(tenant, projectId, inputId, (await run.ReadJsonAsync()).GetProperty("id").GetString()!);
    }
}
