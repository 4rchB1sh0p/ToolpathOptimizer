using System.Globalization;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Strict invariant-culture decimal parsing: optional sign, digits with optional fraction, optional exponent.
/// Rejects thousands separators, hex, whitespace and NaN/Infinity literals.
/// </summary>
internal static class InvariantNumber
{
    public static bool IsNonFiniteLiteral(string token)
    {
        string body = token.TrimStart('+', '-');
        return body.Equals("nan", StringComparison.OrdinalIgnoreCase)
            || body.Equals("inf", StringComparison.OrdinalIgnoreCase)
            || body.Equals("infinity", StringComparison.OrdinalIgnoreCase)
            || body == "∞";
    }

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
