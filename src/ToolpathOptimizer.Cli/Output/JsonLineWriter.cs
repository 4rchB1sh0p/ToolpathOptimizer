using System.Text;
using System.Text.Json;
using ToolpathOptimizer.Application.Contracts.V1;

namespace ToolpathOptimizer.Cli.Output;

/// <summary>
/// Writes one compact JSON value followed by LF to a text writer (stdout), then flushes.
/// </summary>
public static class JsonLineWriter
{
    /// <summary>Writes a JSON line.</summary>
    /// <param name="target">Destination, normally stdout.</param>
    /// <param name="writeContent">Writes exactly one JSON value.</param>
    public static void WriteLine(TextWriter target, Action<Utf8JsonWriter> writeContent)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Write(Encoding.UTF8.GetString(ContractJson.ToUtf8Bytes(writeContent, ContractJson.Compact)));
        target.Write('\n');
        target.Flush();
    }
}
