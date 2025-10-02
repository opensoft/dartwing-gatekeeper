using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Logging;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var logger = context.RequestServices.GetService<ILogger<ExceptionHandlingMiddleware>>()!;
            logger.LogError(
                exception, "Exception occurred: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = logger.IsEnabled(LogLevel.Debug) ? exception.ToString() : null
            };

            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}