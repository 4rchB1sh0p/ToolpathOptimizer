using System.Globalization;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Strict invariant-culture decimal parsing: optional sign, digits with optional fraction, optional exponent.
/// Rejects thousands separators, hex, whitespace and NaN/Infinity literals.
/// </summary>
internal static class InvariantNumber
{
    /// <summary>Determines whether a token spells NaN or infinity (rejected as non-finite rather than malformed).</summary>
    /// <param name="token">Raw field text.</param>
    /// <returns><c>true</c> for NaN/Inf/Infinity/∞ with an optional sign.</returns>
    public static bool IsNonFiniteLiteral(string token)
    {
        string body = token.TrimStart('+', '-');
        return body.Equals("nan", StringComparison.OrdinalIgnoreCase)
            || body.Equals("inf", StringComparison.OrdinalIgnoreCase)
            || body.Equals("infinity", StringComparison.OrdinalIgnoreCase)
            || body == "∞";
    }

    /// <summary>Parses a token that has strict decimal syntax; overflow yields an infinite value for the caller to reject.</summary>
    /// <param name="token">Raw field text.</param>
    /// <param name="value">Parsed value when the syntax is valid.</param>
    /// <returns><c>true</c> when the token has valid decimal syntax.</returns>
    public static bool TryParse(string token, out double value)
    {
        value = 0;
        if (!HasDecimalSyntax(token))
        {
            return false;
        }

        value = double.Parse(token, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>Checks <c>[+-]?(digits[.digits]|.digits)([eE][+-]?digits)?</c> without regular expressions.</summary>
    private static bool HasDecimalSyntax(string token)
    {
        int i = 0;
        if (i < token.Length && (token[i] == '+' || token[i] == '-'))
        {
            i++;
        }

        int mantissaDigits = 0;
        while (i < token.Length && char.IsAsciiDigit(token[i]))
        {
            i++;
            mantissaDigits++;
        }

        if (i < token.Length && token[i] == '.')
        {
            i++;
            while (i < token.Length && char.IsAsciiDigit(token[i]))
            {
                i++;
                mantissaDigits++;
            }
        }

        if (mantissaDigits == 0)
        {
            return false;
        }

        if (i < token.Length && (token[i] == 'e' || token[i] == 'E'))
        {
            i++;
            if (i < token.Length && (token[i] == '+' || token[i] == '-'))
            {
                i++;
            }

            int exponentDigits = 0;
            while (i < token.Length && char.IsAsciiDigit(token[i]))
            {
                i++;
                exponentDigits++;
            }

            if (exponentDigits == 0)
            {
                return false;
            }
        }

        return i == token.Length;
    }
}
