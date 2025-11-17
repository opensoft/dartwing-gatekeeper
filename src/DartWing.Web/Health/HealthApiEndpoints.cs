namespace DartWing.Web.Health;

public static class HealthApiEndpoints
{
    public static void RegisterHealthApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/health", () =>
        {
            return Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow.ToString("o")
            });
        })
        .WithName("HealthCheck")
        .WithSummary("Health check endpoint for mobile app detection")
        .WithDescription("Returns 200 OK when the service is running. No authentication required.")
        .AllowAnonymous()
        .Produces(200)
        .WithTags("Health");
    }
}
