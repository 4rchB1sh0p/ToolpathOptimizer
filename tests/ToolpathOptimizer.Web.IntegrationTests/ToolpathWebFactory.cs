using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ToolpathOptimizer.Web.IntegrationTests;

/// <summary>Development-environment host with temp artifact storage and a fixed clock.</summary>
public sealed class ToolpathWebFactory : WebApplicationFactory<Program>
{
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "tpo-web-tests", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _settings;

    public ToolpathWebFactory()
        : this(new Dictionary<string, string>())
    {
    }

    internal ToolpathWebFactory(Dictionary<string, string> settings)
    {
        _settings = settings;
    }

    public static DateTimeOffset Now { get; } = new(2026, 11, 19, 9, 30, 0, TimeSpan.Zero);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ArtifactStorage:RootPath", _storageRoot);
        foreach ((string key, string value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now)));
    }

    public HttpClient ClientFor(string tenant, string user, string role)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-Tenant", tenant);
        client.DefaultRequestHeaders.Add("X-Dev-User", user);
        client.DefaultRequestHeaders.Add("X-Dev-Role", role);
        return client;
    }

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
            // Best effort cleanup.
        }
    }
}

internal static class Api
{
    public static string Sample(string name) => Path.Combine(AppContext.BaseDirectory, "samples", name);

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    public static async Task<string> CreateProjectAsync(this HttpClient client, string name = "Plate")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/projects", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.JsonAsync()).GetProperty("id").GetString()!;
    }

    public static Task<HttpResponseMessage> UploadAsync(this HttpClient client, string projectId, byte[] points, byte[] manifest)
    {
        var form = new MultipartFormDataContent();
        var pointsContent = new ByteArrayContent(points);
        pointsContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        var manifestContent = new ByteArrayContent(manifest);
        manifestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        form.Add(pointsContent, "points", "points.csv");
        form.Add(manifestContent, "manifest", "job.json");
        return client.PostAsync($"/api/v1/projects/{projectId}/inputs", form);
    }

    public static async Task<string> UploadSampleAsync(this HttpClient client, string projectId)
    {
        HttpResponseMessage response = await client.UploadAsync(projectId, await File.ReadAllBytesAsync(Sample("plate.csv")), await File.ReadAllBytesAsync(Sample("plate-job.json")));
        response.EnsureSuccessStatusCode();
        return (await response.JsonAsync()).GetProperty("id").GetString()!;
    }

    public static Task<HttpResponseMessage> RunAsync(this HttpClient client, string projectId, string inputId, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/analyses") { Content = JsonContent.Create(new { input_id = inputId }) };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return client.SendAsync(request);
    }
}
