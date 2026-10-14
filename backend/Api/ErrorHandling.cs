namespace SmartAgenda.Api.Endpoints;

public record ErrorResponse(string Code, string Message);

public static class ApiErrors
{
    public static IResult BadRequest(string message, string code = "VALIDATION_ERROR") =>
        Results.Json(new ErrorResponse(code, message), statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFound(string message = "No se encontró el recurso.") =>
        Results.Json(new ErrorResponse("NOT_FOUND", message), statusCode: StatusCodes.Status404NotFound);

    public static IResult Conflict(string message) =>
        Results.Json(new ErrorResponse("CONFLICT", message), statusCode: StatusCodes.Status409Conflict);

    public static IResult Unprocessable(string message, string code = "NOTHING_RECOGNIZED") =>
        Results.Json(new ErrorResponse(code, message), statusCode: StatusCodes.Status422UnprocessableEntity);

    public static IResult GatewayTimeout(string message) =>
        Results.Json(new ErrorResponse("AI_UNAVAILABLE", message), statusCode: StatusCodes.Status504GatewayTimeout);

    public static void UseApiErrorHandling(this WebApplication app) =>
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponse("INTERNAL_ERROR", "Ocurrió un error inesperado."));
        }));
}
