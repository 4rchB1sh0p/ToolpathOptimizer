namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// A single expected validation failure with a stable code and a human-readable message.
/// </summary>
public sealed class ValidationIssue
{
    /// <summary>Creates an issue.</summary>
    /// <param name="code">Stable error code.</param>
    /// <param name="message">Explanation naming the offending constraint.</param>
    public ValidationIssue(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable error code.</summary>
    public string Code { get; }

    /// <summary>Gets the explanation.</summary>
    public string Message { get; }
}
