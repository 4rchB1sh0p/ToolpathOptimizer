using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Web.Api;

/// <summary>
/// Maps <see cref="OperationOutcome"/> to RFC 9457 problem details carrying the same stable codes as the CLI.
/// </summary>
public static class ProblemResults
{
    /// <summary>Creates a problem result.</summary>
    /// <param name="outcome">Failure outcome.</param>
    /// <returns>The HTTP result.</returns>
    public static IResult From(OperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        OutcomeError first = outcome.Errors[0];
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = first.Code,
            ["errors"] = outcome.Errors.Select(e => new { code = e.Code, message = e.Message }).ToArray(),
        };
        return Results.Problem(
            detail: first.Message,
            statusCode: StatusFor(outcome.Category, first.Code),
            title: TitleFor(outcome.Category),
            type: "urn:toolpath-optimizer:problem:" + first.Code.ToLowerInvariant(),
            extensions: extensions);
    }

    /// <summary>Maps a category and code to an HTTP status.</summary>
    /// <param name="category">Outcome category.</param>
    /// <param name="code">First error code.</param>
    /// <returns>The status code.</returns>
    public static int StatusFor(OutcomeCategory category, string code) => category switch
    {
        _ when code == ErrorCodes.InputLimitExceeded => StatusCodes.Status413PayloadTooLarge,
        _ when code == ErrorCodes.Unauthenticated => StatusCodes.Status401Unauthorized,
        OutcomeCategory.Arguments or OutcomeCategory.Input or OutcomeCategory.Cancelled => StatusCodes.Status400BadRequest,
        OutcomeCategory.Infeasible => StatusCodes.Status422UnprocessableEntity,
        OutcomeCategory.NotFound => StatusCodes.Status404NotFound,
        OutcomeCategory.Forbidden => StatusCodes.Status403Forbidden,
        OutcomeCategory.Quota => StatusCodes.Status429TooManyRequests,
        OutcomeCategory.Conflict => StatusCodes.Status409Conflict,
        OutcomeCategory.Budget => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string TitleFor(OutcomeCategory category) => category switch
    {
        OutcomeCategory.Input or OutcomeCategory.Arguments => "Invalid input",
        OutcomeCategory.Infeasible => "Infeasible constraints",
        OutcomeCategory.NotFound => "Not found",
        OutcomeCategory.Forbidden => "Access denied",
        OutcomeCategory.Quota => "Quota exceeded",
        OutcomeCategory.Conflict => "Conflict",
        OutcomeCategory.Budget => "Resource budget exceeded",
        OutcomeCategory.Cancelled => "Cancelled",
        _ => "Processing failure",
    };
}
