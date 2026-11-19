namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// Validates identifiers (point IDs and operation groups) against
/// <c>^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$</c>; the leading-character rule also prevents spreadsheet formula injection.
/// </summary>
public static class IdentifierRules
{
    /// <summary>Maximum identifier length in characters.</summary>
    public const int MaxLength = 64;

    /// <summary>Determines whether a value is a valid identifier.</summary>
    /// <param name="value">Candidate identifier.</param>
    /// <returns><c>true</c> when the value matches the identifier pattern.</returns>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || !char.IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            char c = value[i];
            if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '.' && c != '-')
            {
                return false;
            }
        }

        return true;
    }
}
