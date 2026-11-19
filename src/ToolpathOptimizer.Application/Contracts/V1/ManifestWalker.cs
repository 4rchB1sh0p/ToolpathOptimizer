using System.Text.Json;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Walks a parsed manifest field by field, returning the first violation as a <see cref="Result{T}"/>.
/// Field paths in messages use JSONPath-like notation (<c>$.constraints.precedence[0].before</c>).
/// </summary>
internal sealed class ManifestWalker
{
    private const string RootPath = "$";
    private const string ConstraintsPath = "$.constraints";
    private const string AlgorithmPath = "$.algorithm";

    private static readonly string[] _rootFields =
        ["schema_version", "job_id", "coordinate_system", "units", "start_x_mm", "start_y_mm", "end_x_mm", "end_y_mm", "constraints", "algorithm"];

    private static readonly string[] _constraintFields = ["precedence", "locked_groups", "allowed_transitions"];
    private static readonly string[] _edgeFields = ["before", "after"];
    private static readonly string[] _algorithmFields = ["name", "seed", "max_passes", "max_move_evaluations", "min_improvement_mm"];

    private readonly string _fileLabel;
    private readonly AnalysisLimitsOptions _limits;

    /// <summary>Creates a walker.</summary>
    /// <param name="fileLabel">File name used in messages.</param>
    /// <param name="limits">Configured limits and budget defaults.</param>
    public ManifestWalker(string fileLabel, AnalysisLimitsOptions limits)
    {
        _fileLabel = fileLabel;
        _limits = limits;
    }

    /// <summary>Reads the whole manifest.</summary>
    /// <param name="root">Root JSON element.</param>
    /// <returns>The manifest, or the first violation.</returns>
    public Result<JobManifest> ReadRoot(JsonElement root)
    {
        Result<Dictionary<string, JsonElement>> fields = ReadObject(root, RootPath);
        if (!fields.IsSuccess)
        {
            return fields.FailAs<JobManifest>();
        }

        Dictionary<string, JsonElement> rootFields = fields.Value;
        OperationOutcome settings = CheckFixedSettings(rootFields);
        if (!settings.IsSuccess)
        {
            return Result<JobManifest>.Failure(settings);
        }

        Result<string> jobId = rootFields.TryGetValue("job_id", out JsonElement jobIdElement) ? ReadIdentifier(jobIdElement, "$.job_id") : Result<string>.Success(string.Empty);
        Result<(double XMm, double YMm)> start = ReadStart(rootFields);
        Result<(double XMm, double YMm)?> end = ReadEnd(rootFields);
        Result<ManifestConstraints> constraints = rootFields.TryGetValue("constraints", out JsonElement constraintsElement)
            ? ReadConstraints(constraintsElement)
            : Result<ManifestConstraints>.Success(ManifestConstraints.Empty);
        Result<AlgorithmParameters> algorithm = rootFields.TryGetValue("algorithm", out JsonElement algorithmElement)
            ? ReadAlgorithm(algorithmElement)
            : Fail<AlgorithmParameters>(ErrorCodes.InputMissingField, AlgorithmPath, "is required");

        OperationOutcome firstFailure = FirstFailure(jobId.Outcome, start.Outcome, end.Outcome, constraints.Outcome, algorithm.Outcome);
        if (!firstFailure.IsSuccess)
        {
            return Result<JobManifest>.Failure(firstFailure);
        }

        return Result<JobManifest>.Success(new JobManifest(
            jobId.Value, start.Value.XMm, start.Value.YMm, end.Value, constraints.Value.Precedence, constraints.Value.LockedGroups, algorithm.Value));
    }

    /// <summary>Checks schema version first (newer minors are rejected before unknown fields), then coordinate system and units.</summary>
    private OperationOutcome CheckFixedSettings(Dictionary<string, JsonElement> fields)
    {
        Result<string> schema = ReadRequiredString(fields, "schema_version", RootPath);
        if (!schema.IsSuccess)
        {
            return schema.Outcome;
        }

        if (schema.Value != ContractVersions.ManifestSchemaVersion)
        {
            return Error(ErrorCodes.InputUnsupportedSchemaVersion, "$.schema_version", $"{InputText.Quote(schema.Value)} is not supported; expected '{ContractVersions.ManifestSchemaVersion}'");
        }

        OperationOutcome unknown = RejectUnknownFields(fields, _rootFields, RootPath);
        if (!unknown.IsSuccess)
        {
            return unknown;
        }

        OperationOutcome coordinateSystem = RequireExactString(fields, "coordinate_system", ContractVersions.CoordinateSystem, ErrorCodes.InputUnsupportedCoordinateSystem);
        return coordinateSystem.IsSuccess
            ? RequireExactString(fields, "units", ContractVersions.LengthUnit, ErrorCodes.InputUnsupportedUnit)
            : coordinateSystem;
    }

    private Result<(double XMm, double YMm)> ReadStart(Dictionary<string, JsonElement> fields)
    {
        if (!fields.TryGetValue("start_x_mm", out JsonElement x) || !fields.TryGetValue("start_y_mm", out JsonElement y))
        {
            return Fail<(double, double)>(ErrorCodes.InputMissingStart, RootPath, "start_x_mm and start_y_mm are required");
        }

        return ReadPosition(x, y, "$.start_x_mm", "$.start_y_mm");
    }

    private Result<(double XMm, double YMm)?> ReadEnd(Dictionary<string, JsonElement> fields)
    {
        bool hasEndX = fields.TryGetValue("end_x_mm", out JsonElement x);
        bool hasEndY = fields.TryGetValue("end_y_mm", out JsonElement y);
        if (hasEndX != hasEndY)
        {
            return Fail<(double, double)?>(ErrorCodes.InputPartialEnd, RootPath, "end_x_mm and end_y_mm must be given together or not at all");
        }

        if (!hasEndX)
        {
            return Result<(double XMm, double YMm)?>.Success(null);
        }

        Result<(double XMm, double YMm)> end = ReadPosition(x, y, "$.end_x_mm", "$.end_y_mm");
        return end.IsSuccess ? Result<(double XMm, double YMm)?>.Success(end.Value) : end.FailAs<(double XMm, double YMm)?>();
    }

    private Result<(double XMm, double YMm)> ReadPosition(JsonElement x, JsonElement y, string xPath, string yPath)
    {
        Result<double> xValue = ReadCoordinate(x, xPath);
        Result<double> yValue = ReadCoordinate(y, yPath);
        OperationOutcome failure = FirstFailure(xValue.Outcome, yValue.Outcome);
        return failure.IsSuccess ? Result<(double, double)>.Success((xValue.Value, yValue.Value)) : Result<(double, double)>.Failure(failure);
    }

    private Result<ManifestConstraints> ReadConstraints(JsonElement element)
    {
        Result<Dictionary<string, JsonElement>> fields = ReadObject(element, ConstraintsPath);
        if (!fields.IsSuccess)
        {
            return fields.FailAs<ManifestConstraints>();
        }

        OperationOutcome unknown = RejectUnknownFields(fields.Value, _constraintFields, ConstraintsPath);
        OperationOutcome transitions = fields.Value.TryGetValue("allowed_transitions", out JsonElement transitionsElement)
            ? RejectAllowedTransitions(transitionsElement)
            : OperationOutcome.Success;
        Result<List<PrecedenceEntry>> precedence = fields.Value.TryGetValue("precedence", out JsonElement edges)
            ? ReadPrecedence(edges)
            : Result<List<PrecedenceEntry>>.Success([]);
        Result<List<string>> lockedGroups = fields.Value.TryGetValue("locked_groups", out JsonElement groups)
            ? ReadLockedGroups(groups)
            : Result<List<string>>.Success([]);

        OperationOutcome failure = FirstFailure(unknown, transitions, precedence.Outcome, lockedGroups.Outcome);
        return failure.IsSuccess
            ? Result<ManifestConstraints>.Success(new ManifestConstraints(precedence.Value, lockedGroups.Value))
            : Result<ManifestConstraints>.Failure(failure);
    }

    private OperationOutcome RejectAllowedTransitions(JsonElement element)
    {
        const string path = ConstraintsPath + ".allowed_transitions";
        if (element.ValueKind != JsonValueKind.Array)
        {
            return Error(ErrorCodes.InputInvalidType, path, "must be an array");
        }

        return element.GetArrayLength() == 0
            ? OperationOutcome.Success
            : Error(ErrorCodes.InputUnsupportedConstraint, path, "allowed transitions are not supported in v1");
    }

    private Result<List<PrecedenceEntry>> ReadPrecedence(JsonElement element)
    {
        const string path = ConstraintsPath + ".precedence";
        if (element.ValueKind != JsonValueKind.Array)
        {
            return Fail<List<PrecedenceEntry>>(ErrorCodes.InputInvalidType, path, "must be an array");
        }

        int edgeCount = element.GetArrayLength();
        if (edgeCount > _limits.MaxPrecedenceEdges)
        {
            return Fail<List<PrecedenceEntry>>(ErrorCodes.InputLimitExceeded, path, $"{edgeCount} edges exceed the limit of {_limits.MaxPrecedenceEdges}");
        }

        var entries = new List<PrecedenceEntry>(edgeCount);
        int index = 0;
        foreach (JsonElement edge in element.EnumerateArray())
        {
            Result<PrecedenceEntry> entry = ReadPrecedenceEntry(edge, $"{path}[{index++}]");
            if (!entry.IsSuccess)
            {
                return entry.FailAs<List<PrecedenceEntry>>();
            }

            entries.Add(entry.Value);
        }

        return Result<List<PrecedenceEntry>>.Success(entries);
    }

    private Result<PrecedenceEntry> ReadPrecedenceEntry(JsonElement element, string path)
    {
        Result<Dictionary<string, JsonElement>> fields = ReadObject(element, path);
        if (!fields.IsSuccess)
        {
            return fields.FailAs<PrecedenceEntry>();
        }

        OperationOutcome unknown = RejectUnknownFields(fields.Value, _edgeFields, path);
        Result<string> before = ReadRequiredIdentifier(fields.Value, "before", path);
        Result<string> after = ReadRequiredIdentifier(fields.Value, "after", path);
        OperationOutcome failure = FirstFailure(unknown, before.Outcome, after.Outcome);
        return failure.IsSuccess ? Result<PrecedenceEntry>.Success(new PrecedenceEntry(before.Value, after.Value)) : Result<PrecedenceEntry>.Failure(failure);
    }

    private Result<List<string>> ReadLockedGroups(JsonElement element)
    {
        const string path = ConstraintsPath + ".locked_groups";
        if (element.ValueKind != JsonValueKind.Array)
        {
            return Fail<List<string>>(ErrorCodes.InputInvalidType, path, "must be an array");
        }

        var groups = new List<string>();
        int index = 0;
        foreach (JsonElement group in element.EnumerateArray())
        {
            Result<string> name = ReadIdentifier(group, $"{path}[{index++}]");
            if (!name.IsSuccess)
            {
                return name.FailAs<List<string>>();
            }

            groups.Add(name.Value);
        }

        return Result<List<string>>.Success(groups);
    }

    private Result<AlgorithmParameters> ReadAlgorithm(JsonElement element)
    {
        Result<Dictionary<string, JsonElement>> fields = ReadObject(element, AlgorithmPath);
        if (!fields.IsSuccess)
        {
            return fields.FailAs<AlgorithmParameters>();
        }

        OperationOutcome unknown = RejectUnknownFields(fields.Value, _algorithmFields, AlgorithmPath);
        OperationOutcome name = RequireAlgorithmName(fields.Value);
        Result<long> seed = ReadOptionalInteger(fields.Value, "seed", AlgorithmParameters.ReservedSeed, new IntegerRange(AlgorithmParameters.ReservedSeed, AlgorithmParameters.ReservedSeed));
        Result<long> passes = ReadOptionalInteger(fields.Value, "max_passes", _limits.DefaultMaxPasses, new IntegerRange(0, _limits.MaxPassesCap));
        Result<long> evaluations = ReadOptionalInteger(fields.Value, "max_move_evaluations", _limits.DefaultMaxMoveEvaluations, new IntegerRange(0, _limits.MaxMoveEvaluationsCap));
        Result<double> minImprovement = ReadMinImprovement(fields.Value);

        OperationOutcome failure = FirstFailure(unknown, name, seed.Outcome, passes.Outcome, evaluations.Outcome, minImprovement.Outcome);
        return failure.IsSuccess
            ? Result<AlgorithmParameters>.Success(new AlgorithmParameters(seed.Value, passes.Value, evaluations.Value, minImprovement.Value))
            : Result<AlgorithmParameters>.Failure(failure);
    }

    private OperationOutcome RequireAlgorithmName(Dictionary<string, JsonElement> fields)
    {
        Result<string> name = ReadRequiredString(fields, "name", AlgorithmPath);
        if (!name.IsSuccess)
        {
            return name.Outcome;
        }

        return name.Value == RouteOptimiser.AlgorithmName
            ? OperationOutcome.Success
            : Error(ErrorCodes.InputInvalidParameter, AlgorithmPath + ".name", $"{InputText.Quote(name.Value)} is not supported; expected '{RouteOptimiser.AlgorithmName}'");
    }

    private Result<double> ReadMinImprovement(Dictionary<string, JsonElement> fields)
    {
        const string path = AlgorithmPath + ".min_improvement_mm";
        if (!fields.TryGetValue("min_improvement_mm", out JsonElement element))
        {
            return Result<double>.Success(_limits.DefaultMinImprovementMm);
        }

        Result<double> value = ReadFiniteNumber(element, path, ErrorCodes.InputInvalidParameter);
        if (!value.IsSuccess || (value.Value >= 0 && value.Value <= _limits.MinImprovementCapMm))
        {
            return value;
        }

        return Fail<double>(ErrorCodes.InputInvalidParameter, path, $"must be within 0..{_limits.MinImprovementCapMm}");
    }

    private Result<Dictionary<string, JsonElement>> ReadObject(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return Fail<Dictionary<string, JsonElement>>(ErrorCodes.InputInvalidType, path, "must be an object");
        }

        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!fields.TryAdd(property.Name, property.Value))
            {
                return Fail<Dictionary<string, JsonElement>>(ErrorCodes.InputDuplicateField, $"{path}.{TruncateName(property.Name)}", "appears more than once");
            }
        }

        return Result<Dictionary<string, JsonElement>>.Success(fields);
    }

    private OperationOutcome RejectUnknownFields(Dictionary<string, JsonElement> fields, string[] allowedFields, string path)
    {
        string? unknown = fields.Keys.FirstOrDefault(name => Array.IndexOf(allowedFields, name) < 0);
        return unknown is null
            ? OperationOutcome.Success
            : Error(ErrorCodes.InputUnknownField, $"{path}.{TruncateName(unknown)}", "is not a recognised field");
    }

    private OperationOutcome RequireExactString(Dictionary<string, JsonElement> fields, string name, string expected, string mismatchCode)
    {
        Result<string> value = ReadRequiredString(fields, name, RootPath);
        if (!value.IsSuccess)
        {
            return value.Outcome;
        }

        return value.Value == expected
            ? OperationOutcome.Success
            : Error(mismatchCode, $"$.{name}", $"{InputText.Quote(value.Value)} is not supported; expected '{expected}'");
    }

    private Result<string> ReadRequiredString(Dictionary<string, JsonElement> fields, string name, string path)
    {
        if (!fields.TryGetValue(name, out JsonElement element))
        {
            return Fail<string>(ErrorCodes.InputMissingField, $"{path}.{name}", "is required");
        }

        return element.ValueKind == JsonValueKind.String
            ? Result<string>.Success(element.GetString()!)
            : Fail<string>(ErrorCodes.InputInvalidType, $"{path}.{name}", "must be a string");
    }

    private Result<string> ReadRequiredIdentifier(Dictionary<string, JsonElement> fields, string name, string path) =>
        fields.TryGetValue(name, out JsonElement element)
            ? ReadIdentifier(element, $"{path}.{name}")
            : Fail<string>(ErrorCodes.InputMissingField, $"{path}.{name}", "is required");

    private Result<string> ReadIdentifier(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            return Fail<string>(ErrorCodes.InputInvalidType, path, "must be a string");
        }

        string value = element.GetString()!;
        return IdentifierRules.IsValid(value)
            ? Result<string>.Success(value)
            : Fail<string>(ErrorCodes.InputInvalidIdentifier, path, $"{InputText.Quote(value)} is not a valid identifier");
    }

    private Result<double> ReadCoordinate(JsonElement element, string path)
    {
        Result<double> value = ReadFiniteNumber(element, path, ErrorCodes.InputNonFiniteCoordinate);
        if (!value.IsSuccess || Math.Abs(value.Value) <= _limits.MaxCoordinateMagnitudeMm)
        {
            return value;
        }

        return Fail<double>(ErrorCodes.InputLimitExceeded, path, $"|value| exceeds {_limits.MaxCoordinateMagnitudeMm} mm");
    }

    private Result<double> ReadFiniteNumber(JsonElement element, string path, string nonFiniteCode)
    {
        if (element.ValueKind != JsonValueKind.Number)
        {
            return Fail<double>(ErrorCodes.InputInvalidType, path, "must be a number");
        }

        return element.TryGetDouble(out double value) && double.IsFinite(value)
            ? Result<double>.Success(value)
            : Fail<double>(nonFiniteCode, path, "must be a finite number");
    }

    private Result<long> ReadOptionalInteger(Dictionary<string, JsonElement> fields, string name, long defaultValue, IntegerRange allowed)
    {
        string path = $"{AlgorithmPath}.{name}";
        if (!fields.TryGetValue(name, out JsonElement element))
        {
            return Result<long>.Success(defaultValue);
        }

        if (element.ValueKind != JsonValueKind.Number)
        {
            return Fail<long>(ErrorCodes.InputInvalidType, path, "must be a number");
        }

        return element.TryGetInt64(out long value) && allowed.Contains(value)
            ? Result<long>.Success(value)
            : Fail<long>(ErrorCodes.InputInvalidParameter, path, $"must be an integer within {allowed.Minimum}..{allowed.Maximum}");
    }

    private static OperationOutcome FirstFailure(params OperationOutcome[] outcomes) =>
        outcomes.FirstOrDefault(outcome => !outcome.IsSuccess) ?? OperationOutcome.Success;

    private OperationOutcome Error(string code, string path, string detail) =>
        OperationOutcome.Failure(code, $"{_fileLabel}, field '{path}': {detail}.");

    private Result<T> Fail<T>(string code, string path, string detail) => Result<T>.Failure(Error(code, path, detail));

    private static string TruncateName(string name) =>
        name.Length > InputText.MaxEchoLength ? name[..InputText.MaxEchoLength] + "…" : name;

    /// <summary>An inclusive integer range for parameter checks.</summary>
    private sealed class IntegerRange(long minimum, long maximum)
    {
        /// <summary>Gets the smallest allowed value.</summary>
        public long Minimum { get; } = minimum;

        /// <summary>Gets the largest allowed value.</summary>
        public long Maximum { get; } = maximum;

        /// <summary>Determines whether a value lies within the range.</summary>
        /// <param name="value">Candidate value.</param>
        /// <returns><c>true</c> when allowed.</returns>
        public bool Contains(long value) => value >= Minimum && value <= Maximum;
    }

    /// <summary>The constraint lists read from <c>$.constraints</c>.</summary>
    private sealed class ManifestConstraints(IReadOnlyList<PrecedenceEntry> precedence, IReadOnlyList<string> lockedGroups)
    {
        /// <summary>Gets a constraint set with no precedence edges or locked groups.</summary>
        public static ManifestConstraints Empty { get; } = new([], []);

        /// <summary>Gets the precedence entries in input order.</summary>
        public IReadOnlyList<PrecedenceEntry> Precedence { get; } = precedence;

        /// <summary>Gets the locked group names in input order.</summary>
        public IReadOnlyList<string> LockedGroups { get; } = lockedGroups;
    }
}
