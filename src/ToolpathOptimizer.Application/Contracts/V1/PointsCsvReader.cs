using System.Text;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Strict reader for the v1 points CSV (input-output-contracts.md). Fails on the first violation.
/// </summary>
public static class PointsCsvReader
{
    private const int FieldCount = 5;
    private const double MinPointSeparationMm = 0.001;
    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Parses and validates CSV bytes.</summary>
    /// <param name="content">Raw file bytes.</param>
    /// <param name="fileLabel">File name used in messages.</param>
    /// <param name="limits">Configured limits.</param>
    /// <returns>Rows in file order, or the first violation.</returns>
    public static Result<IReadOnlyList<PointRow>> Read(ReadOnlySpan<byte> content, string fileLabel, AnalysisLimitsOptions limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (content.Length > limits.MaxPointsFileBytes)
        {
            return Fail(ErrorCodes.InputLimitExceeded, $"{fileLabel}: file exceeds {limits.MaxPointsFileBytes} bytes.");
        }

        if (!TryDecode(content, out string text))
        {
            return Fail(ErrorCodes.InputInvalidEncoding, $"{fileLabel}: file is not valid UTF-8.");
        }

        List<string> lines = SplitLines(text);
        string? headerError = CheckHeader(lines[0], fileLabel, out string headerCode);
        if (headerError is not null)
        {
            return Fail(headerCode, headerError);
        }

        int rowCount = lines.Count - 1;
        if (rowCount == 0)
        {
            return Fail(ErrorCodes.InputNoPoints, $"{fileLabel}: no point rows.");
        }

        if (rowCount > limits.MaxPoints)
        {
            return Fail(ErrorCodes.InputLimitExceeded, $"{fileLabel}: {rowCount} points exceed the limit of {limits.MaxPoints}.");
        }

        var rows = new List<PointRow>(rowCount);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int r = 1; r < lines.Count; r++)
        {
            Result<PointRow> row = ParseRow(lines[r], r + 1, fileLabel, limits);
            if (!row.IsSuccess)
            {
                return Result<IReadOnlyList<PointRow>>.Failure(row.Outcome);
            }

            if (!ids.Add(row.Value.PointId))
            {
                return Fail(ErrorCodes.InputDuplicatePointId, $"{fileLabel} line {r + 1}: duplicate point_id {InputText.Quote(row.Value.PointId)}.");
            }

            rows.Add(row.Value);
        }

        return CheckSequence(rows, fileLabel) ?? CheckCoincident(rows, fileLabel) ?? Result<IReadOnlyList<PointRow>>.Success(rows);
    }

    private static bool TryDecode(ReadOnlySpan<byte> content, out string text)
    {
        ReadOnlySpan<byte> bom = [0xEF, 0xBB, 0xBF];
        if (content.StartsWith(bom))
        {
            content = content[bom.Length..];
        }

        try
        {
            text = s_strictUtf8.GetString(content);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static List<string> SplitLines(string text)
    {
        var lines = text.Split('\n').Select(l => l.EndsWith('\r') ? l[..^1] : l).ToList();
        if (lines.Count > 1 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static string? CheckHeader(string header, string fileLabel, out string code)
    {
        code = ErrorCodes.InputMalformedCsv;
        if (string.Equals(header, ContractVersions.PointsCsvHeader, StringComparison.Ordinal))
        {
            return null;
        }

        string[] columns = header.Split(',');
        bool sameShape = columns.Length == FieldCount && columns[0] == "point_id" && columns[3] == "sequence" && columns[4] == "operation_group"
            && columns[1].StartsWith("x_", StringComparison.Ordinal) && columns[2].StartsWith("y_", StringComparison.Ordinal);
        if (sameShape)
        {
            code = ErrorCodes.InputUnitMismatch;
            return $"{fileLabel} line 1: coordinate columns must use the '_mm' suffix to match manifest units 'mm'.";
        }

        return $"{fileLabel} line 1: header must be exactly '{ContractVersions.PointsCsvHeader}'.";
    }

    private static Result<PointRow> ParseRow(string line, int lineNumber, string fileLabel, AnalysisLimitsOptions limits)
    {
        string where = $"{fileLabel} line {lineNumber}";
        if (line.Length == 0)
        {
            return Result<PointRow>.Failure(ErrorCodes.InputMalformedCsv, $"{where}: empty row.");
        }

        if (line.Contains('"') || line.Contains('\r'))
        {
            return Result<PointRow>.Failure(ErrorCodes.InputMalformedCsv, $"{where}: quoted fields and stray carriage returns are not supported.");
        }

        string[] fields = line.Split(',');
        if (fields.Length != FieldCount)
        {
            return Result<PointRow>.Failure(ErrorCodes.InputMalformedCsv, $"{where}: expected {FieldCount} fields, found {fields.Length}.");
        }

        if (!IdentifierRules.IsValid(fields[0]))
        {
            return Result<PointRow>.Failure(ErrorCodes.InputInvalidIdentifier, $"{where}, field 'point_id': {InputText.Quote(fields[0])} is not a valid identifier.");
        }

        Result<double> x = ParseCoordinate(fields[1], $"{where}, field 'x_mm'", limits);
        if (!x.IsSuccess)
        {
            return Result<PointRow>.Failure(x.Outcome);
        }

        Result<double> y = ParseCoordinate(fields[2], $"{where}, field 'y_mm'", limits);
        if (!y.IsSuccess)
        {
            return Result<PointRow>.Failure(y.Outcome);
        }

        if (!IsSequenceToken(fields[3]))
        {
            return Result<PointRow>.Failure(ErrorCodes.InputSequenceInvalid, $"{where}, field 'sequence': {InputText.Quote(fields[3])} is not a positive integer.");
        }

        if (fields[4].Length > 0 && !IdentifierRules.IsValid(fields[4]))
        {
            return Result<PointRow>.Failure(ErrorCodes.InputInvalidIdentifier, $"{where}, field 'operation_group': {InputText.Quote(fields[4])} is not a valid identifier.");
        }

        return Result<PointRow>.Success(new PointRow(fields[0], x.Value, y.Value, int.Parse(fields[3], System.Globalization.CultureInfo.InvariantCulture), fields[4]));
    }

    private static Result<double> ParseCoordinate(string token, string where, AnalysisLimitsOptions limits)
    {
        if (InvariantNumber.IsNonFiniteLiteral(token))
        {
            return Result<double>.Failure(ErrorCodes.InputNonFiniteCoordinate, $"{where}: {InputText.Quote(token)} is not finite.");
        }

        if (!InvariantNumber.TryParse(token, out double value))
        {
            return Result<double>.Failure(ErrorCodes.InputInvalidNumber, $"{where}: {InputText.Quote(token)} is not an invariant decimal number.");
        }

        if (!double.IsFinite(value))
        {
            return Result<double>.Failure(ErrorCodes.InputNonFiniteCoordinate, $"{where}: value overflows to infinity.");
        }

        if (Math.Abs(value) > limits.MaxCoordinateMagnitudeMm)
        {
            return Result<double>.Failure(ErrorCodes.InputLimitExceeded, $"{where}: |value| exceeds {limits.MaxCoordinateMagnitudeMm} mm.");
        }

        return Result<double>.Success(value);
    }

    private static bool IsSequenceToken(string token) =>
        token.Length is > 0 and <= 9 && token.All(char.IsAsciiDigit);

    private static Result<IReadOnlyList<PointRow>>? CheckSequence(List<PointRow> rows, string fileLabel)
    {
        bool[] seen = new bool[rows.Count + 1];
        foreach (PointRow row in rows)
        {
            if (row.Sequence < 1 || row.Sequence > rows.Count || seen[row.Sequence])
            {
                return Fail(ErrorCodes.InputSequenceInvalid, $"{fileLabel}: sequence values must be exactly 1..{rows.Count}; {row.Sequence} is out of range or repeated (point {InputText.Quote(row.PointId)}).");
            }

            seen[row.Sequence] = true;
        }

        return null;
    }

    private static Result<IReadOnlyList<PointRow>>? CheckCoincident(List<PointRow> rows, string fileLabel)
    {
        // Sweep by X so only nearby pairs are compared; deterministic because ties sort by ordinal ID.
        PointRow[] sorted = rows.OrderBy(r => r.XMm).ThenBy(r => r.PointId, StringComparer.Ordinal).ToArray();
        const double minSquared = MinPointSeparationMm * MinPointSeparationMm;
        for (int i = 0; i < sorted.Length; i++)
        {
            for (int j = i + 1; j < sorted.Length && sorted[j].XMm - sorted[i].XMm < MinPointSeparationMm; j++)
            {
                double dx = sorted[j].XMm - sorted[i].XMm;
                double dy = sorted[j].YMm - sorted[i].YMm;
                if ((dx * dx) + (dy * dy) < minSquared)
                {
                    return Fail(ErrorCodes.InputCoincidentPoints, $"{fileLabel}: points {InputText.Quote(sorted[i].PointId)} and {InputText.Quote(sorted[j].PointId)} are closer than {MinPointSeparationMm} mm.");
                }
            }
        }

        return null;
    }

    private static Result<IReadOnlyList<PointRow>> Fail(string code, string message) => Result<IReadOnlyList<PointRow>>.Failure(code, message);
}
