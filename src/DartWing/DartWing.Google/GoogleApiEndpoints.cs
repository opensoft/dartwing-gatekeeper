using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DartWing.Google;

using Microsoft.AspNetCore.Routing;

public static class GoogleApiEndpoints
{
    public static void RegisterUserApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/google/").WithTags("Google");

        group.MapGet("files", async (ILogger<GoogleSettings> logger, CancellationToken ct) =>
        {
            await Task.CompletedTask;
            return Results.Conflict("");
        });
    }
}