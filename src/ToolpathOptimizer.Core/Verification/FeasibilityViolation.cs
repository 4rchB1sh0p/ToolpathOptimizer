namespace ToolpathOptimizer.Core.Verification;

/// <summary>
/// A single constraint violation found in a route.
/// </summary>
public sealed class FeasibilityViolation
{
    /// <summary>Creates a violation.</summary>
    /// <param name="code">Stable code from <see cref="FeasibilityCodes"/>.</param>
    /// <param name="message">Explanation naming the points or group involved.</param>
    public FeasibilityViolation(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable code from <see cref="FeasibilityCodes"/>.</summary>
    public string Code { get; }

    /// <summary>Gets the explanation naming the points or group involved.</summary>
    public string Message { get; }
}
