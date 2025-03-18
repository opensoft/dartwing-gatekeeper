using System.Diagnostics;
using DartWing.ErpNext;
using DartWing.ErpNext.Dto;
using DartWing.KeyCloak;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static  class UserApiEndpoints
{
    public static void RegisterUserApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/user/").WithTags("User");

        group.MapPost("", async ([FromBody] UserInfoRequest user,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Create user {email}", user.Email);
            var u = httpContextAccessor.HttpContext?.User;
            var userId = u?.FindFirst("sub")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            var existErpUser = await erpNextService.GetUserAsync(keyCloakUser.Email, ct);

            var dto = new UserCreateRequestDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                Address = user.Address,
                Country = user.Country,
                ZipCode = user.PostalCode
            };
            if (existErpUser != null)
            {
                var updErpUser = await erpNextService.UpdateUserAsync(user.Email, dto, ct);
                return Results.Ok(updErpUser);
            }
            var erpUser = await erpNextService.CreateUserAsync(dto, ct);
            return Results.Ok(erpUser);
        }).WithName("CreateOrUpdateUser").WithSummary("Create or Update user");
        
        group.MapPut("", async ([FromBody] UserInfoRequest user,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            logger.LogInformation("API Update user {email}", user.Email);
            var u = httpContextAccessor.HttpContext?.User;
            var userId = u?.FindFirst("sub")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            var existErpUser = await erpNextService.GetUserAsync(keyCloakUser.Email, ct);
            if (existErpUser == null)
                return Results.Conflict("erpNext user not found");

            var dto = new UserCreateRequestDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                Address = user.Address,
                Country = user.Country,
                ZipCode = user.PostalCode
            };
            var erpUser = await erpNextService.UpdateUserAsync(user.Email, dto, ct);
            return Results.Ok(erpUser);
        }).WithName("UpdateUser").WithSummary("Update user");

        group.MapGet("", async ([FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get user");
            var u = httpContextAccessor.HttpContext!.User;
            var userId = u.FindFirst("sub")?.Value;
            if (userId == null) return Results.BadRequest("User id is null");
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            var existErpUser = await erpNextService.GetUserAsync(keyCloakUser.Email, ct);
            if (existErpUser == null)
            {
                logger.LogInformation("Get user {uId} {email}: not found in erpNext {sw}", userId, keyCloakUser.Email, Stopwatch.GetElapsedTime(sw));
                return Results.NotFound();
            }
            logger.LogInformation("Get user {uId} {email}: OK {sw}", userId, keyCloakUser.Email, Stopwatch.GetElapsedTime(sw));
            return Results.Ok(existErpUser);
        }).WithName("User").WithSummary("Get user").Produces<UserInfoRequest>();
    }
}