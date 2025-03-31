using System.Diagnostics;
using DartWing.ErpNext;
using DartWing.ErpNext.Dto;
using DartWing.KeyCloak;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static class UserApiEndpoints
{
    public static void RegisterUserApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/user/").WithTags("User").RequireAuthorization();;

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
            
            if (erpUser?.Data == null) return Results.BadRequest("User creation failed"); 
            
            UserInfoResponse response = new(erpUser.Data);
            
            return Results.Ok(response);
        }).WithName("CreateOrUpdateUser").WithSummary("Create or Update user").Produces<UserInfoResponse>();
        
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
            var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser == null) return Results.Conflict("KeyCloak user not found");
            var existErpUser = await erpNextService.GetUserAsync(keyCloakUser.Email, ct);
            if (existErpUser == null) return Results.Conflict("erpNext user not found");

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
            
            if (erpUser == null) return Results.BadRequest("User update failed"); 
            UserInfoResponse response = new(erpUser);
            return Results.Ok(response);
        }).WithName("UpdateUser").WithSummary("Update user").Produces<UserInfoResponse>();

        group.MapGet("", async ([FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get user");
            var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            var userEmail = httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var existErpUser = await erpNextService.GetUserAsync(userEmail, ct);
            if (existErpUser?.Data == null)
            {
                logger.LogInformation("Get user {uId} {email}: not found in erpNext {sw}", userId, userEmail, Stopwatch.GetElapsedTime(sw));
                return Results.NotFound();
            }
            logger.LogInformation("Get user {uId} {email}: OK {sw}", userId, userEmail, Stopwatch.GetElapsedTime(sw));
            UserInfoResponse response = new(existErpUser.Data);
            
            return Results.Ok(response);
        }).WithName("User").WithSummary("Get user").Produces<UserInfoResponse>();
        
        group.MapGet("companies", async ([FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Get user companies");
            var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            var userEmail = httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
            if (userEmail == null) return Results.BadRequest("User email is null");
            var companies = await erpNextService.GetUserCompaniesAsync(userEmail, ct);
            if (companies?.Data == null)
            {
                logger.LogInformation("Get user companies {uId} {email}: not found in erpNext {sw}", userId, userEmail, Stopwatch.GetElapsedTime(sw));
                return Results.NotFound();
            }
            logger.LogInformation("Get user companies {uId} {email}: {cnt} OK {sw}", userId, userEmail, companies.Data.Length, Stopwatch.GetElapsedTime(sw));
            var response = companies.Data.Select(x => new UserCompanyResponse
                { CompanyName = x.ForValue, Email = userEmail, UserId = userId! }).ToArray();
            
            return Results.Ok(response);
        }).WithName("UserCompanies").WithSummary("Get user companies").Produces<UserCompanyResponse[]>();
    }
}