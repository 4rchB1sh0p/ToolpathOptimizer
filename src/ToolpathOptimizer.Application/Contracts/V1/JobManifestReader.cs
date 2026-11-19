using System.Text.Json;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Strict reader for the v1 job manifest (ADR-0010): rejects unknown or duplicate properties, comments,
/// trailing commas, depth &gt; 16, wrong types and unsupported versions, units or constraints.
/// </summary>
public static class JobManifestReader
{
    private const int MaxDepth = 16;
    private static readonly string[] s_rootFields =
        ["schema_version", "job_id", "coordinate_system", "units", "start_x_mm", "start_y_mm", "end_x_mm", "end_y_mm", "constraints", "algorithm"];

    private static readonly string[] s_constraintFields = ["precedence", "locked_groups", "allowed_transitions"];
    private static readonly string[] s_edgeFields = ["before", "after"];
    private static readonly string[] s_algorithmFields = ["name", "seed", "max_passes", "max_move_evaluations", "min_improvement_mm"];

    /// <summary>Parses and validates manifest bytes.</summary>
    /// <param name="content">Raw file bytes.</param>
    /// <param name="fileLabel">File name used in messages.</param>
    /// <param name="limits">Configured limits and budget defaults.</param>
    /// <returns>The manifest, or the first violation.</returns>
    public static Result<JobManifest> Read(ReadOnlyMemory<byte> content, string fileLabel, AnalysisLimitsOptions limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (content.Length > limits.MaxManifestFileBytes)
        {
            return Result<JobManifest>.Failure(ErrorCodes.InputLimitExceeded, $"{fileLabel}: file exceeds {limits.MaxManifestFileBytes} bytes.");
        }

        try
        {
            using JsonDocument document = Parse(content, fileLabel);
            return Result<JobManifest>.Success(new Walker(fileLabel, limits).ReadRoot(document.RootElement));
        }
        catch (ManifestException ex)
        {
            return Result<JobManifest>.Failure(ex.Code, ex.Message);
        }
    }

    private static JsonDocument Parse(ReadOnlyMemory<byte> content, string fileLabel)
    {
        ReadOnlySpan<byte> bom = [0xEF, 0xBB, 0xBF];
        if (content.Span.StartsWith(bom))
        {
            content = content[bom.Length..];
        }

        var options = new JsonDocumentOptions { MaxDepth = MaxDepth, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false };
        try
        {
            return JsonDocument.Parse(content, options);
        }
        catch (JsonException ex)
        {
            string position = ex.LineNumber is long line ? $" near line {line + 1}" : string.Empty;
            throw new ManifestException(ErrorCodes.InputMalformedJson, $"{fileLabel}: invalid JSON{position} (comments, trailing commas and depth > {MaxDepth} are rejected).");
        }
    }

    private sealed class Walker(string fileLabel, AnalysisLimitsOptions limits)
    {
        public JobManifest ReadRoot(JsonElement root)
        {
            Dictionary<string, JsonElement> fields = Fields(root, "$");
            string schema = RequiredString(fields, "schema_version", "$");
            if (schema != ContractVersions.ManifestSchemaVersion)
            {
                throw Error(ErrorCodes.InputUnsupportedSchemaVersion, "$.schema_version", $"{InputText.Quote(schema)} is not supported; expected '{ContractVersions.ManifestSchemaVersion}'");
            }

            RejectUnknown(fields, s_rootFields, "$");
            string coordinateSystem = RequiredString(fields, "coordinate_system", "$");
            if (coordinateSystem != ContractVersions.CoordinateSystem)
            {
                throw Error(ErrorCodes.InputUnsupportedCoordinateSystem, "$.coordinate_system", $"expected '{ContractVersions.CoordinateSystem}'");
            }

            string units = RequiredString(fields, "units", "$");
            if (units != ContractVersions.LengthUnit)
            {
                throw Error(ErrorCodes.InputUnsupportedUnit, "$.units", $"{InputText.Quote(units)} is not supported; expected 'mm'");
            }

            string jobId = OptionalIdentifier(fields, "job_id", "$");
            if (!fields.ContainsKey("start_x_mm") || !fields.ContainsKey("start_y_mm"))
            {
                throw Error(ErrorCodes.InputMissingStart, "$", "start_x_mm and start_y_mm are required");
            }

            double startX = Coordinate(fields["start_x_mm"], "$.start_x_mm");
            double startY = Coordinate(fields["start_y_mm"], "$.start_y_mm");
            bool hasEndX = fields.TryGetValue("end_x_mm", out JsonElement endXElement);
            bool hasEndY = fields.TryGetValue("end_y_mm", out JsonElement endYElement);
            if (hasEndX != hasEndY)
            {
                throw Error(ErrorCodes.InputPartialEnd, "$", "end_x_mm and end_y_mm must be given together or not at all");
            }

            double? endX = hasEndX ? Coordinate(endXElement, "$.end_x_mm") : null;
            double? endY = hasEndY ? Coordinate(endYElement, "$.end_y_mm") : null;

            var precedence = new List<PrecedenceEntry>();
            var lockedGroups = new List<string>();
            if (fields.TryGetValue("constraints", out JsonElement constraints))
            {
                ReadConstraints(constraints, precedence, lockedGroups);
            }

            if (!fields.TryGetValue("algorithm", out JsonElement algorithm))
            {
                throw Error(ErrorCodes.InputMissingField, "$.algorithm", "is required");
            }

            return new JobManifest(jobId, startX, startY, endX, endY, precedence, lockedGroups, ReadAlgorithm(algorithm));
        }

        private void ReadConstraints(JsonElement element, List<PrecedenceEntry> precedence, List<string> lockedGroups)
        {
            const string path = "$.constraints";
            Dictionary<string, JsonElement> fields = Fields(element, path);
            RejectUnknown(fields, s_constraintFields, path);
            if (fields.TryGetValue("allowed_transitions", out JsonElement transitions) && Array(transitions, path + ".allowed_transitions").GetArrayLength() > 0)
            {
                throw Error(ErrorCodes.InputUnsupportedConstraint, path + ".allowed_transitions", "allowed transitions are not supported in v1");
            }

            if (fields.TryGetValue("precedence", out JsonElement edges))
            {
                int count = Array(edges, path + ".precedence").GetArrayLength();
                if (count > limits.MaxPrecedenceEdges)
                {
                    throw Error(ErrorCodes.InputLimitExceeded, path + ".precedence", $"{count} edges exceed the limit of {limits.MaxPrecedenceEdges}");
                }

                int index = 0;
                foreach (JsonElement edge in edges.EnumerateArray())
                {
                    string edgePath = $"{path}.precedence[{index++}]";
                    Dictionary<string, JsonElement> edgeFields = Fields(edge, edgePath);
                    RejectUnknown(edgeFields, s_edgeFields, edgePath);
                    precedence.Add(new PrecedenceEntry(RequiredIdentifier(edgeFields, "before", edgePath), RequiredIdentifier(edgeFields, "after", edgePath)));
                }
            }

            if (fields.TryGetValue("locked_groups", out JsonElement groups))
            {
                int index = 0;
                foreach (JsonElement group in Array(groups, path + ".locked_groups").EnumerateArray())
                {
                    lockedGroups.Add(Identifier(group, $"{path}.locked_groups[{index++}]"));
                }
            }
        }

        private AlgorithmParameters ReadAlgorithm(JsonElement element)
        {
            const string path = "$.algorithm";
            Dictionary<string, JsonElement> fields = Fields(element, path);
            RejectUnknown(fields, s_algorithmFields, path);
            string name = RequiredString(fields, "name", path);
            if (name != RouteOptimiser.AlgorithmName)
            {
                throw Error(ErrorCodes.InputInvalidParameter, path + ".name", $"{InputText.Quote(name)} is not supported; expected '{RouteOptimiser.AlgorithmName}'");
            }

            long seed = OptionalInteger(fields, "seed", path, 0, 0, 0);
            long maxPasses = OptionalInteger(fields, "max_passes", path, limits.DefaultMaxPasses, 0, limits.MaxPassesCap);
            long maxEvaluations = OptionalInteger(fields, "max_move_evaluations", path, limits.DefaultMaxMoveEvaluations, 0, limits.MaxMoveEvaluationsCap);
            double minImprovement = limits.DefaultMinImprovementMm;
            if (fields.TryGetValue("min_improvement_mm", out JsonElement minElement))
            {
                minImprovement = Number(minElement, path + ".min_improvement_mm", ErrorCodes.InputInvalidParameter);
                if (minImprovement < 0 || minImprovement > limits.MinImprovementCapMm)
                {
                    throw Error(ErrorCodes.InputInvalidParameter, path + ".min_improvement_mm", $"must be within 0..{limits.MinImprovementCapMm}");
                }
            }

            return new AlgorithmParameters(seed, maxPasses, maxEvaluations, minImprovement);
        }

        private Dictionary<string, JsonElement> Fields(JsonElement element, string path)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw Error(ErrorCodes.InputInvalidType, path, "must be an object");
            }

            var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!fields.TryAdd(property.Name, property.Value))
                {
                    throw Error(ErrorCodes.InputDuplicateField, $"{path}.{Truncate(property.Name)}", "appears more than once");
                }
            }

            return fields;
        }

        private void RejectUnknown(Dictionary<string, JsonElement> fields, string[] allowed, string path)
        {
            foreach (string name in fields.Keys)
            {
                if (System.Array.IndexOf(allowed, name) < 0)
                {
                    throw Error(ErrorCodes.InputUnknownField, $"{path}.{Truncate(name)}", "is not a recognised field");
                }
            }
        }

        private JsonElement Array(JsonElement element, string path) =>
            element.ValueKind == JsonValueKind.Array ? element : throw Error(ErrorCodes.InputInvalidType, path, "must be an array");

        private string RequiredString(Dictionary<string, JsonElement> fields, string name, string path)
        {
            if (!fields.TryGetValue(name, out JsonElement element))
            {
                throw Error(ErrorCodes.InputMissingField, $"{path}.{name}", "is required");
            }

            return element.ValueKind == JsonValueKind.String
                ? element.GetString()!
                : throw Error(ErrorCodes.InputInvalidType, $"{path}.{name}", "must be a string");
        }

        private string RequiredIdentifier(Dictionary<string, JsonElement> fields, string name, string path)
        {
            if (!fields.TryGetValue(name, out JsonElement element))
            {
                throw Error(ErrorCodes.InputMissingField, $"{path}.{name}", "is required");
            }

            return Identifier(element, $"{path}.{name}");
        }

        private string OptionalIdentifier(Dictionary<string, JsonElement> fields, string name, string path) =>
            fields.TryGetValue(name, out JsonElement element) ? Identifier(element, $"{path}.{name}") : string.Empty;

        private string Identifier(JsonElement element, string path)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw Error(ErrorCodes.InputInvalidType, path, "must be a string");
            }

            string value = element.GetString()!;
            return IdentifierRules.IsValid(value) ? value : throw Error(ErrorCodes.InputInvalidIdentifier, path, $"{InputText.Quote(value)} is not a valid identifier");
        }

        private double Coordinate(JsonElement element, string path)
        {
            double value = Number(element, path, ErrorCodes.InputNonFiniteCoordinate);
            return Math.Abs(value) <= limits.MaxCoordinateMagnitudeMm
                ? value
                : throw Error(ErrorCodes.InputLimitExceeded, path, $"|value| exceeds {limits.MaxCoordinateMagnitudeMm} mm");
        }

        private double Number(JsonElement element, string path, string nonFiniteCode)
        {
            if (element.ValueKind != JsonValueKind.Number)
            {
                throw Error(ErrorCodes.InputInvalidType, path, "must be a number");
            }

            return element.TryGetDouble(out double value) && double.IsFinite(value)
                ? value
                : throw Error(nonFiniteCode, path, "must be a finite number");
        }

        private long OptionalInteger(Dictionary<string, JsonElement> fields, string name, string path, long defaultValue, long min, long max)
        {
            if (!fields.TryGetValue(name, out JsonElement element))
            {
                return defaultValue;
            }

            string fieldPath = $"{path}.{name}";
            if (element.ValueKind != JsonValueKind.Number)
            {
                throw Error(ErrorCodes.InputInvalidType, fieldPath, "must be a number");
            }

            if (!element.TryGetInt64(out long value) || value < min || value > max)
            {
                throw Error(ErrorCodes.InputInvalidParameter, fieldPath, $"must be an integer within {min}..{max}");
            }

            return value;
        }

        private ManifestException Error(string code, string path, string detail) => new(code, $"{fileLabel}, field '{path}': {detail}.");

        private static string Truncate(string name) => name.Length > InputText.MaxEchoLength ? name[..InputText.MaxEchoLength] + "…" : name;
    }

    private sealed class ManifestException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}
