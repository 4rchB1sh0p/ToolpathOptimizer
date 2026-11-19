using System.Text.Encodings.Web;
using System.Text.Json;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Shared JSON writing for v1 contract outputs (reports, CLI envelopes): writer options, UTF-8 rendering and the
/// <c>[{ "code", "message" }]</c> array used for errors, warnings and violations.
/// </summary>
public static class ContractJson
{
    /// <summary>
    /// Gets options for human-readable outputs: indented, LF newlines, relaxed escaping. Relaxed escaping keeps
    /// apostrophes and <c>+</c> readable; outputs are served as <c>application/json</c>, never embedded in HTML.
    /// </summary>
    public static JsonWriterOptions Indented { get; } = new()
    {
        Indented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Gets options for single-line outputs such as the CLI envelope (relaxed escaping, no indentation).</summary>
    public static JsonWriterOptions Compact { get; } = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Gets options for the canonical hash input. Never change: it would change every <c>input_sha256</c>.</summary>
    public static JsonWriterOptions Canonical { get; } = default;

    /// <summary>Renders JSON to UTF-8 bytes.</summary>
    /// <param name="writeContent">Writes one JSON value.</param>
    /// <param name="options">Writer options.</param>
    /// <returns>The UTF-8 bytes.</returns>
    public static byte[] ToUtf8Bytes(Action<Utf8JsonWriter> writeContent, JsonWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(writeContent);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writeContent(writer);
        }

        return buffer.ToArray();
    }

    /// <summary>Writes a named array of <c>{ "code", "message" }</c> objects.</summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="writer">Target writer, positioned inside an object.</param>
    /// <param name="propertyName">Array property name.</param>
    /// <param name="items">Items in output order.</param>
    /// <param name="codeOf">Selects an item's stable code.</param>
    /// <param name="messageOf">Selects an item's message.</param>
    public static void WriteCodedMessages<T>(Utf8JsonWriter writer, string propertyName, IEnumerable<T> items, Func<T, string> codeOf, Func<T, string> messageOf)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(items);
        writer.WriteStartArray(propertyName);
        foreach (T item in items)
        {
            writer.WriteStartObject();
            writer.WriteString("code", codeOf(item));
            writer.WriteString("message", messageOf(item));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
