namespace Authy.Application.Extensions;

public static class OpenApiExtensions
{
    public static RouteGroupBuilder WithDefaultApiResponses(this RouteGroupBuilder builder)
    {
        return builder
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

