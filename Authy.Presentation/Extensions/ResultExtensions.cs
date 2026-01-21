using Authy.Presentation.Domain;
using Authy.Presentation.Shared;

namespace Authy.Presentation.Extensions;

public static class ResultExtensions
{
    private static readonly Dictionary<string, int> ErrorCodes = new()
    {
        { DomainErrors.User.UnauthorizedIp.Code, StatusCodes.Status403Forbidden }
    };
    
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Can't convert success result to problem");
        }

        if (result.Errors.Length > 1)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Multiple errors occurred",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", result.Errors }
                });
        }

        var statusCode = ErrorCodes.TryGetValue(result.Error.Code, out var code)
            ? code
            : StatusCodes.Status400BadRequest;

        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(statusCode),
            detail: result.Error.Description,
            extensions: new Dictionary<string, object?>
            {
                { "code", result.Error.Code }
            }
        );
    }
    
    private static string GetTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            _ => "An error occurred"
        };
}
