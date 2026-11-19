namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// A coded error whose message names the file, row or field and never echoes more than 64 characters of input.
/// </summary>
public sealed class OutcomeError
{
    /// <summary>Creates an error.</summary>
    /// <param name="code">Stable code from <see cref="ErrorCodes"/> or a Core code catalogue.</param>
    /// <param name="message">Human-readable explanation with location and expected value.</param>
    public OutcomeError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable code from <see cref="ErrorCodes"/> or a Core code catalogue.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable explanation with location and expected value.</summary>
    public string Message { get; }
}
