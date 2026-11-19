namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Whether an existing report file may be replaced.
/// </summary>
public enum ReportWriteMode
{
    /// <summary>Fail with <c>IO_OUTPUT_EXISTS</c> when the file exists.</summary>
    CreateNew,

    /// <summary>Atomically replace an existing file.</summary>
    Overwrite,
}
