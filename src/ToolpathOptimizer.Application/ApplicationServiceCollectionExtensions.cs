using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Application.Provenance;

namespace ToolpathOptimizer.Application;

/// <summary>
/// Registers Application use cases and cross-cutting defaults.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Adds handlers, provenance and options validation. Hosts bind <see cref="AnalysisLimitsOptions"/>.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddToolpathApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(ToolInfo.FromAssembly());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AnalysisLimitsOptions>, AnalysisLimitsOptionsValidator>());
        services.AddOptions<AnalysisLimitsOptions>().ValidateOnStart();
        services.TryAddSingleton<ProvenanceFactory>();
        services.TryAddSingleton<JobPreparer>();
        services.TryAddSingleton<ValidateJobHandler>();
        services.TryAddSingleton<AnalyzeJobHandler>();
        services.TryAddScoped<ProjectAccess>();
        services.TryAddScoped<ProjectCommandService>();
        services.TryAddScoped<ProjectQueryService>();
        return services;
    }
}
