using System.Reflection;

namespace ToolpathOptimizer.Application.Provenance;

/// <summary>
/// Identifies the tool that produced a report.
/// </summary>
public sealed class ToolInfo
{
    /// <summary>Creates tool information.</summary>
    /// <param name="toolName">Public command name.</param>
    /// <param name="toolVersion">Build version, including the source revision when known.</param>
    public ToolInfo(string toolName, string toolVersion)
    {
        ToolName = toolName;
        ToolVersion = toolVersion;
    }

    /// <summary>Gets the public command name.</summary>
    public string ToolName { get; }

    /// <summary>Gets the build version (excluded from determinism comparisons).</summary>
    public string ToolVersion { get; }

    /// <summary>Creates tool information from this assembly's informational version.</summary>
    /// <returns>The tool information.</returns>
    public static ToolInfo FromAssembly()
    {
        string version = typeof(ToolInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        return new ToolInfo("toolpath-optimize", version);
    }
}
