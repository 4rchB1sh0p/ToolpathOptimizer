namespace ToolpathOptimizer.Application.Outcomes;

/// <summary>
/// Stable error codes owned by the Application layer (public contract).
/// Core codes live in <c>ValidationCodes</c> and <c>AnalysisCodes</c>.
/// </summary>
public static class ErrorCodes
{
    /// <summary>A required command-line option is missing.</summary>
    public const string ArgsMissingOption = "ARGS_MISSING_OPTION";

    /// <summary>An option was given without a value.</summary>
    public const string ArgsMissingValue = "ARGS_MISSING_VALUE";

    /// <summary>An option is not recognised.</summary>
    public const string ArgsUnknownOption = "ARGS_UNKNOWN_OPTION";

    /// <summary>An option was given more than once.</summary>
    public const string ArgsDuplicateOption = "ARGS_DUPLICATE_OPTION";

    /// <summary>An option value is not supported.</summary>
    public const string ArgsInvalidValue = "ARGS_INVALID_VALUE";

    /// <summary>No command was given.</summary>
    public const string ArgsMissingCommand = "ARGS_MISSING_COMMAND";

    /// <summary>The command is not recognised.</summary>
    public const string ArgsUnknownCommand = "ARGS_UNKNOWN_COMMAND";

    /// <summary>The output path resolves to an input file.</summary>
    public const string ArgsOutputIsInput = "ARGS_OUTPUT_IS_INPUT";

    /// <summary>Configuration failed validation.</summary>
    public const string ConfigInvalid = "CONFIG_INVALID";

    /// <summary>Input bytes are not valid UTF-8.</summary>
    public const string InputInvalidEncoding = "INPUT_INVALID_ENCODING";

    /// <summary>CSV structure is malformed (columns, quoting, empty rows).</summary>
    public const string InputMalformedCsv = "INPUT_MALFORMED_CSV";

    /// <summary>JSON is malformed, has comments, trailing commas or exceeds depth.</summary>
    public const string InputMalformedJson = "INPUT_MALFORMED_JSON";

    /// <summary>A required field is missing.</summary>
    public const string InputMissingField = "INPUT_MISSING_FIELD";

    /// <summary>A field has the wrong JSON type.</summary>
    public const string InputInvalidType = "INPUT_INVALID_TYPE";

    /// <summary>An unknown field is present.</summary>
    public const string InputUnknownField = "INPUT_UNKNOWN_FIELD";

    /// <summary>A field appears more than once.</summary>
    public const string InputDuplicateField = "INPUT_DUPLICATE_FIELD";

    /// <summary>An identifier does not match the identifier pattern.</summary>
    public const string InputInvalidIdentifier = "INPUT_INVALID_IDENTIFIER";

    /// <summary>A number is not a valid invariant decimal.</summary>
    public const string InputInvalidNumber = "INPUT_INVALID_NUMBER";

    /// <summary>A coordinate is NaN or infinite.</summary>
    public const string InputNonFiniteCoordinate = "INPUT_NON_FINITE_COORDINATE";

    /// <summary>Point identifiers are not unique.</summary>
    public const string InputDuplicatePointId = "INPUT_DUPLICATE_POINT_ID";

    /// <summary>Sequence values do not form exactly 1..n.</summary>
    public const string InputSequenceInvalid = "INPUT_SEQUENCE_INVALID";

    /// <summary>Two distinct points are closer than 0.001 mm.</summary>
    public const string InputCoincidentPoints = "INPUT_COINCIDENT_POINTS";

    /// <summary>A configured limit is exceeded.</summary>
    public const string InputLimitExceeded = "INPUT_LIMIT_EXCEEDED";

    /// <summary>The file contains no points.</summary>
    public const string InputNoPoints = "INPUT_NO_POINTS";

    /// <summary>The length unit is not supported.</summary>
    public const string InputUnsupportedUnit = "INPUT_UNSUPPORTED_UNIT";

    /// <summary>Manifest units do not match the CSV header suffix.</summary>
    public const string InputUnitMismatch = "INPUT_UNIT_MISMATCH";

    /// <summary>The coordinate system is not supported.</summary>
    public const string InputUnsupportedCoordinateSystem = "INPUT_UNSUPPORTED_COORDINATE_SYSTEM";

    /// <summary>The schema version is not supported.</summary>
    public const string InputUnsupportedSchemaVersion = "INPUT_UNSUPPORTED_SCHEMA_VERSION";

    /// <summary>A constraint kind is not supported in v1.</summary>
    public const string InputUnsupportedConstraint = "INPUT_UNSUPPORTED_CONSTRAINT";

    /// <summary>An algorithm parameter is invalid.</summary>
    public const string InputInvalidParameter = "INPUT_INVALID_PARAMETER";

    /// <summary>The start position is missing.</summary>
    public const string InputMissingStart = "INPUT_MISSING_START";

    /// <summary>Only one end coordinate was supplied.</summary>
    public const string InputPartialEnd = "INPUT_PARTIAL_END";

    /// <summary>Unexpected defect.</summary>
    public const string InternalError = "INTERNAL_ERROR";

    /// <summary>The caller cancelled.</summary>
    public const string Cancelled = "CANCELLED";

    /// <summary>A file could not be read.</summary>
    public const string IoReadFailed = "IO_READ_FAILED";

    /// <summary>A file could not be written.</summary>
    public const string IoWriteFailed = "IO_WRITE_FAILED";

    /// <summary>The output exists and overwrite was not requested.</summary>
    public const string IoOutputExists = "IO_OUTPUT_EXISTS";

    /// <summary>The wall-clock safety deadline was reached.</summary>
    public const string OptDeadlineExceeded = "OPT_DEADLINE_EXCEEDED";

    /// <summary>The resource does not exist for this tenant.</summary>
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    /// <summary>The caller's role does not permit the action.</summary>
    public const string AccessDenied = "ACCESS_DENIED";

    /// <summary>The caller is not authenticated.</summary>
    public const string Unauthenticated = "UNAUTHENTICATED";

    /// <summary>The tenant's plan quota is exhausted.</summary>
    public const string QuotaExceeded = "QUOTA_EXCEEDED";

    /// <summary>An idempotency key was reused with a different request.</summary>
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
}
