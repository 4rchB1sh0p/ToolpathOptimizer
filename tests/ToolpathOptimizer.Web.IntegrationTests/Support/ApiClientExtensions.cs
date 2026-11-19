using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ToolpathOptimizer.Web.IntegrationTests.Support;

/// <summary>Typed helpers for calling the project API from tests.</summary>
internal static class ApiClientExtensions
{
    /// <summary>Gets the path of a synthetic sample copied next to the test assembly.</summary>
    /// <param name="name">Sample file name.</param>
    /// <returns>The full path.</returns>
    public static string SamplePath(string name) => Path.Combine(AppContext.BaseDirectory, "samples", name);

    /// <summary>Reads a response body as JSON.</summary>
    /// <param name="response">HTTP response.</param>
    /// <returns>The root element.</returns>
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>Reads the stable <c>code</c> of a problem-details response.</summary>
    /// <param name="response">HTTP response.</param>
    /// <returns>The code.</returns>
    public static async Task<string> ReadProblemCodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("code").GetString()!;

    /// <summary>Creates a project and returns its identifier.</summary>
    /// <param name="client">Authenticated client.</param>
    /// <returns>The project identifier.</returns>
    public static async Task<string> CreateProjectAsync(this HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/projects", new { name = "Plate" });
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("id").GetString()!;
    }

    /// <summary>Uploads points and manifest bytes as multipart form data.</summary>
    /// <param name="client">Authenticated client.</param>
    /// <param name="projectId">Target project.</param>
    /// <param name="points">Points CSV bytes.</param>
    /// <param name="manifest">Manifest JSON bytes.</param>
    /// <returns>The response.</returns>
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

    /// <summary>Uploads the plate sample and returns the input identifier.</summary>
    /// <param name="client">Authenticated client.</param>
    /// <param name="projectId">Target project.</param>
    /// <returns>The input identifier.</returns>
    public static async Task<string> UploadPlateSampleAsync(this HttpClient client, string projectId)
    {
        HttpResponseMessage response = await client.UploadAsync(projectId, await File.ReadAllBytesAsync(SamplePath("plate.csv")), await File.ReadAllBytesAsync(SamplePath("plate-job.json")));
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("id").GetString()!;
    }

    /// <summary>Runs an analysis without an idempotency key.</summary>
    /// <param name="client">Authenticated client.</param>
    /// <param name="projectId">Project of the input.</param>
    /// <param name="inputId">Input to analyse.</param>
    /// <returns>The response.</returns>
    public static Task<HttpResponseMessage> RunAnalysisAsync(this HttpClient client, string projectId, string inputId) =>
        client.PostAsJsonAsync($"/api/v1/projects/{projectId}/analyses", new { input_id = inputId });

    /// <summary>Runs an analysis with an idempotency key.</summary>
    /// <param name="client">Authenticated client.</param>
    /// <param name="projectId">Project of the input.</param>
    /// <param name="inputId">Input to analyse.</param>
    /// <param name="idempotencyKey">Idempotency key header value.</param>
    /// <returns>The response.</returns>
    public static Task<HttpResponseMessage> RunAnalysisAsync(this HttpClient client, string projectId, string inputId, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/analyses") { Content = JsonContent.Create(new { input_id = inputId }) };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }
}
