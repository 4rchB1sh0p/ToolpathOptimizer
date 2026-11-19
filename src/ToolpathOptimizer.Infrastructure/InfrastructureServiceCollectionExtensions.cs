using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Infrastructure.Files;
using ToolpathOptimizer.Infrastructure.Identity;
using ToolpathOptimizer.Infrastructure.Persistence;
using ToolpathOptimizer.Infrastructure.Storage;

namespace ToolpathOptimizer.Infrastructure;

/// <summary>
/// Registers Infrastructure adapters and binds configuration sections.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds file, storage, persistence and development identity/billing adapters.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration root.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddToolpathInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<AnalysisLimitsOptions>().Bind(configuration.GetSection(AnalysisLimitsOptions.SectionName));
        services.AddOptions<ArtifactStorageOptions>().Bind(configuration.GetSection(ArtifactStorageOptions.SectionName));
        services.AddOptions<EntitlementOptions>().Bind(configuration.GetSection(EntitlementOptions.SectionName));

        services.TryAddSingleton<IReportWriter, AtomicFileReportWriter>();
        services.TryAddSingleton<IInputFileReader, BoundedFileReader>();
        services.TryAddSingleton<IFileAliasDetector, FileAliasDetector>();
        services.TryAddSingleton<IArtifactStore, LocalArtifactStore>();
        services.TryAddSingleton<IProjectRepository, InMemoryProjectRepository>();
        services.TryAddSingleton<IEntitlementService, StaticEntitlementService>();
        services.TryAddSingleton<IAuditLog, LoggingAuditLog>();
        return services;
    }
}
