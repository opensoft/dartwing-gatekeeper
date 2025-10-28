using System.Diagnostics;
using DartWing.DomainModel.Extensions;
using DartWing.KeyCloak;
using DartWing.KeyCloak.Dto;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static class TokenApiEndpoints
{
    public static void RegisterTokenApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/token/").WithTags("Token").RequireAuthorization();

        group.MapPost("{alias}/exchange/{type}", async (
            string alias,
            string type,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakProvider,
            [FromServices] Task<IKeyCloakUser?> keyCloakUserTask,
            [FromServices] KeyCloakTokenProvider keyCloakTokenProvider,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            var keyCloakUser = await keyCloakUserTask;
            if (keyCloakUser?.Id == null) return Results.BadRequest("User id or email is null");
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Token exchange {c} {t}", alias, type);

            var c = await keyCloakProvider.GetOrganization(alias, ct);
            if (c == null) return Results.NotFound();
            var ownerEmail = httpContextAccessor.IsClientToken() ? c.GetCompanyOwner() : keyCloakUser?.Email;
            if (string.IsNullOrEmpty(ownerEmail)) return Results.BadRequest("User id or email is null");
            var token = await keyCloakTokenProvider.TokenExchangeProviderAccessToken(ownerEmail, type, false, ct);
            
            return Results.Ok(token);
        }).WithName("TokenExchange").WithSummary("Exchange token").Produces<AuthTokenResponse>();
    }
}