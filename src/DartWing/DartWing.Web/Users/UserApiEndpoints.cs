using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static  class UserApiEndpoints
{
    public static void RegisterUserApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/user/").WithTags("User");

        group.MapPost("", async ([FromBody] UserInfoRequest user,
            [FromServices] IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            
            return Results.Ok();
        }).WithName("CreateUser").WithSummary("Create user");
        
        group.MapGet("", async ([FromServices] IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            
            return Results.Json(new UserInfoRequest());
        }).WithName("User").WithSummary("Get user").Produces<UserInfoRequest>();
    }
}