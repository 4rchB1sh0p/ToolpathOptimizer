namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Formats untrusted input for error messages: never more than 64 characters, control characters replaced.
/// </summary>
public static class InputText
{
    /// <summary>Maximum characters of input echoed in a message.</summary>
    public const int MaxEchoLength = 64;

    /// <summary>Quotes a truncated, sanitised copy of an input value.</summary>
    /// <param name="value">Untrusted value.</param>
    /// <returns>The quoted value, ending in an ellipsis when truncated.</returns>
    public static string Quote(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string head = value.Length > MaxEchoLength ? value[..MaxEchoLength] : value;
        var chars = head.Select(c => char.IsControl(c) ? '?' : c).ToArray();
        return "'" + new string(chars) + (value.Length > MaxEchoLength ? "…'" : "'");
    }
}
