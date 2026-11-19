using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ToolpathOptimizer.Web.IntegrationTests.Support;

/// <summary>Development-environment host with temp artifact storage and a fixed clock.</summary>
public sealed class ToolpathWebFactory : WebApplicationFactory<Program>
{
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "tpo-web-tests", Guid.NewGuid().ToString("N"));
    private readonly IReadOnlyDictionary<string, string> _settings;

    /// <summary>Creates a factory with default settings (used as an xUnit class fixture).</summary>
    public ToolpathWebFactory()
        : this(new Dictionary<string, string>())
    {
    }

    /// <summary>Creates a factory with configuration overrides.</summary>
    /// <param name="settings">Configuration keys and values applied to the host.</param>
    internal ToolpathWebFactory(IReadOnlyDictionary<string, string> settings)
    {
        _settings = settings;
    }

    /// <summary>Gets the fixed time used by every host.</summary>
    public static DateTimeOffset FixedNow { get; } = new(2026, 11, 19, 9, 30, 0, TimeSpan.Zero);

    /// <summary>Creates a client that authenticates with development identity headers.</summary>
    /// <param name="tenant">Tenant header value.</param>
    /// <param name="user">User header value.</param>
    /// <param name="role">Role header value.</param>
    /// <returns>The client.</returns>
    public HttpClient CreateClientFor(string tenant, string user, string role)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-Tenant", tenant);
        client.DefaultRequestHeaders.Add("X-Dev-User", user);
        client.DefaultRequestHeaders.Add("X-Dev-Role", role);
        return client;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ArtifactStorage:RootPath", _storageRoot);
        foreach ((string key, string value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(FixedNow)));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; the directory name is unique.
        }
    }
}
