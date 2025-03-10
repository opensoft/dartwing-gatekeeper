using DartWing.ErpNext;
using DartWing.ErpNext.Dto;
using DartWing.Web.KeyCloak;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;
using HttpContextAccessor = Microsoft.AspNetCore.Http.HttpContextAccessor;

namespace DartWing.Web.Users;

public static  class UserApiEndpoints
{
    public static void RegisterUserApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/user/").WithTags("User");

        group.MapPost("", async ([FromBody] UserInfoRequest user,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] HttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var u = httpContextAccessor.HttpContext!.User;
            var userId = u.FindFirst("sub")?.Value;
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser.CrmId != null)
            {
                var existErpUser = await erpNextService.GetUserAsync(keyCloakUser.CrmId, ct);
                return Results.Ok(existErpUser);
            }
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
            var erpUser = await erpNextService.CreateUserAsync(dto, ct);
            
            return Results.Ok(erpUser);
        }).WithName("CreateUser").WithSummary("Create user");

        group.MapGet("", async ([FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] HttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakHelper keyCloakHelper,
            [FromServices] ERPNextService erpNextService,
            CancellationToken ct) =>
        {
            var u = httpContextAccessor.HttpContext!.User;
            var userId = u.FindFirst("sub")?.Value;
            var keyCloakUser = await keyCloakHelper.GetUserById(userId, ct);
            if (keyCloakUser.CrmId == null) return Results.NotFound();
            var existErpUser = await erpNextService.GetUserAsync(keyCloakUser.CrmId, ct);
            return Results.Ok(existErpUser);
        }).WithName("User").WithSummary("Get user").Produces<UserInfoRequest>();
    }
}