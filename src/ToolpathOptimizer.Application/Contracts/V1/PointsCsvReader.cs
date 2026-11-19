using System.Globalization;
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
    private const int MaxSequenceDigits = 9;
    private const double MinPointSeparationMm = 0.001;
    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Parses and validates CSV bytes.</summary>
    /// <param name="content">Raw file bytes (an optional UTF-8 BOM is ignored).</param>
    /// <param name="fileLabel">File name used in messages.</param>
    /// <param name="limits">Configured limits.</param>
    /// <returns>Rows in file order, or the first violation.</returns>
    public static Result<IReadOnlyList<PointRow>> Read(ReadOnlyMemory<byte> content, string fileLabel, AnalysisLimitsOptions limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (content.Length > limits.MaxPointsFileBytes)
        {
            return Fail(ErrorCodes.InputLimitExceeded, $"{fileLabel}: file exceeds {limits.MaxPointsFileBytes} bytes.");
        }

        Result<string> text = Decode(ByteOrderMark.TrimUtf8(content), fileLabel);
        if (!text.IsSuccess)
        {
            return text.FailAs<IReadOnlyList<PointRow>>();
        }

        List<string> lines = SplitLines(text.Value);
        OperationOutcome structure = FirstFailure(() => CheckHeader(lines[0], fileLabel), () => CheckRowCount(lines.Count - 1, fileLabel, limits));
        if (!structure.IsSuccess)
        {
            return Result<IReadOnlyList<PointRow>>.Failure(structure);
        }

        Result<List<PointRow>> rows = ParseRows(lines, fileLabel, limits);
        if (!rows.IsSuccess)
        {
            return rows.FailAs<IReadOnlyList<PointRow>>();
        }

        OperationOutcome consistency = FirstFailure(() => CheckSequence(rows.Value, fileLabel), () => CheckCoincident(rows.Value, fileLabel));
        return consistency.IsSuccess
            ? Result<IReadOnlyList<PointRow>>.Success(rows.Value)
            : Result<IReadOnlyList<PointRow>>.Failure(consistency);
    }

    private static Result<string> Decode(ReadOnlyMemory<byte> content, string fileLabel)
    {
        try
        {
            return Result<string>.Success(_strictUtf8.GetString(content.Span));
        }
        catch (DecoderFallbackException)
        {
            return Result<string>.Failure(ErrorCodes.InputInvalidEncoding, $"{fileLabel}: file is not valid UTF-8.");
        }
    }

    /// <summary>Splits on LF, strips a trailing CR per line, and drops one final empty line (trailing newline).</summary>
    private static List<string> SplitLines(string text)
    {
        List<string> lines = text.Split('\n').Select(line => line.EndsWith('\r') ? line[..^1] : line).ToList();
        if (lines.Count > 1 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static OperationOutcome CheckHeader(string header, string fileLabel)
    {
        if (string.Equals(header, ContractVersions.PointsCsvHeader, StringComparison.Ordinal))
        {
            return OperationOutcome.Success;
        }

        string[] columns = header.Split(',');
        bool usesOtherLengthUnit = columns.Length == FieldCount && columns[0] == "point_id" && columns[3] == "sequence" && columns[4] == "operation_group"
            && columns[1].StartsWith("x_", StringComparison.Ordinal) && columns[2].StartsWith("y_", StringComparison.Ordinal);
        return usesOtherLengthUnit
            ? OperationOutcome.Failure(ErrorCodes.InputUnitMismatch, $"{fileLabel} line 1: coordinate columns must use the '_mm' suffix to match manifest units 'mm'.")
            : OperationOutcome.Failure(ErrorCodes.InputMalformedCsv, $"{fileLabel} line 1: header must be exactly '{ContractVersions.PointsCsvHeader}'.");
    }

    private static OperationOutcome CheckRowCount(int rowCount, string fileLabel, AnalysisLimitsOptions limits)
    {
        if (rowCount == 0)
        {
            return OperationOutcome.Failure(ErrorCodes.InputNoPoints, $"{fileLabel}: no point rows.");
        }

        return rowCount > limits.MaxPoints
            ? OperationOutcome.Failure(ErrorCodes.InputLimitExceeded, $"{fileLabel}: {rowCount} points exceed the limit of {limits.MaxPoints}.")
            : OperationOutcome.Success;
    }

    private static Result<List<PointRow>> ParseRows(List<string> lines, string fileLabel, AnalysisLimitsOptions limits)
    {
        var rows = new List<PointRow>(lines.Count - 1);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 1; i < lines.Count; i++)
        {
            string location = $"{fileLabel} line {i + 1}";
            Result<PointRow> row = ParseRow(lines[i], location, limits);
            if (!row.IsSuccess)
            {
                return row.FailAs<List<PointRow>>();
            }

            if (!seenIds.Add(row.Value.PointId))
            {
                return Result<List<PointRow>>.Failure(ErrorCodes.InputDuplicatePointId, $"{location}: duplicate point_id {InputText.Quote(row.Value.PointId)}.");
            }

            rows.Add(row.Value);
        }

        return Result<List<PointRow>>.Success(rows);
    }

    private static Result<PointRow> ParseRow(string line, string location, AnalysisLimitsOptions limits)
    {
        Result<string[]> fields = SplitFields(line, location);
        if (!fields.IsSuccess)
        {
            return fields.FailAs<PointRow>();
        }

        string[] values = fields.Value;
        Result<double> x = ParseCoordinate(values[1], $"{location}, field 'x_mm'", limits);
        Result<double> y = ParseCoordinate(values[2], $"{location}, field 'y_mm'", limits);
        OperationOutcome failure = FirstFailure(
            () => CheckIdentifier(values[0], $"{location}, field 'point_id'"),
            () => x.Outcome,
            () => y.Outcome,
            () => CheckSequenceToken(values[3], $"{location}, field 'sequence'"),
            () => values[4].Length == 0 ? OperationOutcome.Success : CheckIdentifier(values[4], $"{location}, field 'operation_group'"));
        if (!failure.IsSuccess)
        {
            return Result<PointRow>.Failure(failure);
        }

        int sequence = int.Parse(values[3], CultureInfo.InvariantCulture);
        return Result<PointRow>.Success(new PointRow(values[0], x.Value, y.Value, sequence, values[4]));
    }

    private static Result<string[]> SplitFields(string line, string location)
    {
        if (line.Length == 0)
        {
            return Result<string[]>.Failure(ErrorCodes.InputMalformedCsv, $"{location}: empty row.");
        }

        if (line.Contains('"') || line.Contains('\r'))
        {
            return Result<string[]>.Failure(ErrorCodes.InputMalformedCsv, $"{location}: quoted fields and stray carriage returns are not supported.");
        }

        string[] fields = line.Split(',');
        return fields.Length == FieldCount
            ? Result<string[]>.Success(fields)
            : Result<string[]>.Failure(ErrorCodes.InputMalformedCsv, $"{location}: expected {FieldCount} fields, found {fields.Length}.");
    }

    private static OperationOutcome CheckIdentifier(string value, string location) =>
        IdentifierRules.IsValid(value)
            ? OperationOutcome.Success
            : OperationOutcome.Failure(ErrorCodes.InputInvalidIdentifier, $"{location}: {InputText.Quote(value)} is not a valid identifier.");

    private static OperationOutcome CheckSequenceToken(string token, string location) =>
        token.Length is > 0 and <= MaxSequenceDigits && token.All(char.IsAsciiDigit)
            ? OperationOutcome.Success
            : OperationOutcome.Failure(ErrorCodes.InputSequenceInvalid, $"{location}: {InputText.Quote(token)} is not a positive integer.");

    private static Result<double> ParseCoordinate(string token, string location, AnalysisLimitsOptions limits)
    {
        if (InvariantNumber.IsNonFiniteLiteral(token))
        {
            return Result<double>.Failure(ErrorCodes.InputNonFiniteCoordinate, $"{location}: {InputText.Quote(token)} is not finite.");
        }

        if (!InvariantNumber.TryParse(token, out double value))
        {
            return Result<double>.Failure(ErrorCodes.InputInvalidNumber, $"{location}: {InputText.Quote(token)} is not an invariant decimal number.");
        }

        if (!double.IsFinite(value))
        {
            return Result<double>.Failure(ErrorCodes.InputNonFiniteCoordinate, $"{location}: value overflows to infinity.");
        }

        return Math.Abs(value) > limits.MaxCoordinateMagnitudeMm
            ? Result<double>.Failure(ErrorCodes.InputLimitExceeded, $"{location}: |value| exceeds {limits.MaxCoordinateMagnitudeMm} mm.")
            : Result<double>.Success(value);
    }

    private static OperationOutcome CheckSequence(List<PointRow> rows, string fileLabel)
    {
        bool[] isSequenceUsed = new bool[rows.Count + 1];
        foreach (PointRow row in rows)
        {
            if (row.Sequence < 1 || row.Sequence > rows.Count || isSequenceUsed[row.Sequence])
            {
                return OperationOutcome.Failure(ErrorCodes.InputSequenceInvalid, $"{fileLabel}: sequence values must be exactly 1..{rows.Count}; {row.Sequence} is out of range or repeated (point {InputText.Quote(row.PointId)}).");
            }

            isSequenceUsed[row.Sequence] = true;
        }

        return OperationOutcome.Success;
    }

    /// <summary>Sweeps points sorted by X so only nearby pairs are compared; ties sort by ordinal ID for determinism.</summary>
    private static OperationOutcome CheckCoincident(List<PointRow> rows, string fileLabel)
    {
        PointRow[] sorted = rows.OrderBy(row => row.XMm).ThenBy(row => row.PointId, StringComparer.Ordinal).ToArray();
        const double minSquaredSeparation = MinPointSeparationMm * MinPointSeparationMm;
        for (int i = 0; i < sorted.Length; i++)
        {
            for (int j = i + 1; j < sorted.Length && sorted[j].XMm - sorted[i].XMm < MinPointSeparationMm; j++)
            {
                double dx = sorted[j].XMm - sorted[i].XMm;
                double dy = sorted[j].YMm - sorted[i].YMm;
                if ((dx * dx) + (dy * dy) < minSquaredSeparation)
                {
                    return OperationOutcome.Failure(ErrorCodes.InputCoincidentPoints, $"{fileLabel}: points {InputText.Quote(sorted[i].PointId)} and {InputText.Quote(sorted[j].PointId)} are closer than {MinPointSeparationMm} mm.");
                }
            }
        }

        return OperationOutcome.Success;
    }

    /// <summary>Runs checks in order and returns the first failure, skipping later checks.</summary>
    private static OperationOutcome FirstFailure(params Func<OperationOutcome>[] checks)
    {
        foreach (Func<OperationOutcome> check in checks)
        {
            OperationOutcome outcome = check();
            if (!outcome.IsSuccess)
            {
                return outcome;
            }
        }

        return OperationOutcome.Success;
    }

    private static Result<IReadOnlyList<PointRow>> Fail(string code, string message) => Result<IReadOnlyList<PointRow>>.Failure(code, message);
}
