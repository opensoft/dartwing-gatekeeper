using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using DartWing.DomainModel.Extensions;
using DartWing.Frappe.Erp;
using DartWing.KeyCloak;
using DartWing.KeyCloak.Dto;
using DartWing.Web.Auth;
using DartWing.Web.Users.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DartWing.Web.Users;

public static class UserApiEndpoints
{
    private static readonly EmailAddressAttribute EmailAttribute = new();

    public static void RegisterUserApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/user/").WithTags("User").RequireAuthorization();

        group.MapGet("me", async (
            [FromServices] ILogger<Program> logger,
            [FromServices] Task<IKeyCloakUser?> keyCloakUserTask,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakProvider,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            var keyCloakUser = await keyCloakUserTask;
            if (keyCloakUser == null) return Results.BadRequest("KeyCloak user not found");

            var companies = await keyCloakProvider.GetUserOrganizationsWithPermissions(keyCloakUser.Id, ct);
            var result = companies
                .Select(x => new CompanyRoleResponse(x, x.GetUserPermissions(keyCloakUser.Email)))
                .Where(x => !string.IsNullOrEmpty(x.Site)).ToArray();
            
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Get companies {email}: OK orgs={cnt} {sw}", keyCloakUser.Email, companies.Count, sw.Sw());
            else if (sw.Sw().TotalMilliseconds > 100)
                logger.LogInformation("Get companies {email}: OK orgs={cnt} {sw}", keyCloakUser.Email, companies.Count, sw.Sw());
            
            return Results.Ok(new UserWithCompaniesResponse(result)
            {
                Id = httpContextAccessor.GetUserId()!,
                Email = httpContextAccessor.GetEmail()!,
                Name = httpContextAccessor.GetName()!
            });
        }).WithName("User").WithSummary("Get user").Produces<UserWithCompaniesResponse>();
        
        group.MapGet("{email}", async (
            [FromServices] ILogger<Program> logger,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] KeyCloakProvider keyCloakProvider,
            string email,
            CancellationToken ct) =>
        {
            if (!httpContextAccessor.IsClientToken()) return Results.Forbid();
            var sw = Stopwatch.GetTimestamp();
            var keyCloakUser = await keyCloakProvider.GetUserByEmail(email, ct);
            if (keyCloakUser == null) return Results.BadRequest("KeyCloak user not found");

            var companies = await keyCloakProvider.GetUserOrganizationsWithPermissions(keyCloakUser.Id, ct);
            var result = companies
                .Select(x => new CompanyRoleResponse(x, x.GetUserPermissions(keyCloakUser.Email)))
                .Where(x => !string.IsNullOrEmpty(x.Site)).ToArray();
            
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Get companies by client {email}: OK orgs={cnt} {sw}", keyCloakUser.Email, companies.Count, sw.Sw());
            else if (sw.Sw().TotalMilliseconds > 100)
                logger.LogInformation("Get companies by client {email}: OK orgs={cnt} {sw}", keyCloakUser.Email, companies.Count, sw.Sw());
            
            return Results.Ok(new UserWithCompaniesResponse(result)
            {
                Id = httpContextAccessor.GetUserId()!,
                Email = httpContextAccessor.GetEmail()!,
                Name = httpContextAccessor.GetName()!
            });
        }).WithName("UserByClient").WithSummary("Get user by client").Produces<UserWithCompaniesResponse>();
        
        
        // group.MapPost("", async ([FromBody] UserRequest user,
        //     [FromServices] ILogger<Program> logger,
        //     [FromServices] IHttpContextAccessor httpContextAccessor,
        //     [FromServices] KeyCloakProvider keyCloakProvider,
        //     CancellationToken ct) =>
        // {
        //     var sw = Stopwatch.GetTimestamp();
        //     var userId = httpContextAccessor.GetUserId()!;
        //     var email = httpContextAccessor.GetEmail()!;
        //     if (!EmailAttribute.IsValid(email)) return Results.BadRequest("Email is invalid");
        //     logger.LogInformation("API Create user {email}", email);
        //     var provider = httpContextAccessor.HttpContext!.RequestServices;
        //     
        //     //TODO: fix after discussion with Brett
        //     var orgs = await keyCloakProvider.GetUserOrganizations(httpContextAccessor.GetUserId()!, ct);
        //     if (orgs == null || orgs.Length == 0) return Results.BadRequest("KeyCloak user organization not found");
        //     var userService = await provider.GetFrappeUserService(orgs[0].Id);
        //     var existErpUser = await userService!.Get(email, ct);
        //     var dto = new ErpUserData
        //     {
        //         Email = email,
        //         FirstName = user.FirstName,
        //         LastName = user.LastName,
        //         MiddleName = user.MiddleName,
        //         FullName = user.FullName,
        //         Phone = user.PhoneNumber,
        //         Location = user.Location,
        //         TimeZone = user.TimeZone,
        //         Language = user.Language,
        //         Gender = user.Gender,
        //         BirthDate = user.BirthDate,
        //         Interests = user.Interests,
        //         Bio = user.Bio,
        //         MobileNo = user.MobileNumber
        //     };
        //     if (existErpUser != null)
        //     {
        //         var updErpUser = await userService!.Update(email, dto, ct);
        //         return Results.Ok(updErpUser);
        //     }
        //
        //     var erpUser = await userService!.CreateUser(dto, ct);
        //     if (erpUser == null) return Results.BadRequest("User creation failed");
        //     if (erpUser.ApiSecret == null)  logger.LogWarning("API Create user {email} - api secret is null", email);
        //     
        //     var siteUrl = await keyCloakProvider.GetUserDefaultCompanyFrappeSite(orgs[0].Name, ct);
        //     var updated = await keyCloakProvider.AddFrappeKeys(httpContextAccessor.GetUserId()!,
        //         $"{new Uri(siteUrl).Host}={erpUser.ApiKey}:{erpUser.ApiSecret}", ct);
        //     if (!updated) logger.LogWarning("Create user {email} - error while update keys in keycloak for {s} {sw}", email, siteUrl, sw.Sw());
        //     
        //     UserResponse response = new(erpUser);
        //     
        //     logger.LogInformation("Create user {id} {email} {site} OK {sw}", userId, email, siteUrl, sw.Sw());
        //
        //     return Results.Ok(response);
        // }).WithName("CreateOrUpdateUser").WithSummary("Create or Update user").Produces<UserResponse>();

        // group.MapPut("", async ([FromBody] UserRequest user,
        //     [FromServices] ILogger<Program> logger,
        //     [FromServices] Task<IFrappeUser?> frappeUserTask,
        //     [FromServices] Task<IErpUserService?> frappeUserServiceTask,
        //     CancellationToken ct) =>
        // {
        //     logger.LogInformation("API Update user {email}", user.Email);
        //     var existErpUser = await frappeUserTask;
        //     if (existErpUser == null) return Results.Conflict("erpNext user not found");
        //
        //     var dto = new ErpUserData
        //     {
        //         Email = user.Email,
        //         FirstName = user.FirstName,
        //         LastName = user.LastName,
        //         MiddleName = user.MiddleName,
        //         FullName = user.FullName,
        //         Phone = user.PhoneNumber,
        //         Location = user.Location,
        //         TimeZone = user.TimeZone,
        //         Language = user.Language,
        //         Gender = user.Gender,
        //         BirthDate = user.BirthDate,
        //         Interests = user.Interests,
        //         Bio = user.Bio,
        //         MobileNo = user.MobileNumber
        //     };
        //     var erpUserService = await frappeUserServiceTask;
        //     var erpUser = await erpUserService!.Update(user.Email, dto, ct);
        //
        //     if (erpUser == null) return Results.BadRequest("User update failed");
        //     UserResponse response = new(erpUser);
        //     return Results.Ok(response);
        // }).WithName("UpdateUser").WithSummary("Update user").Produces<UserResponse>();

        
        group.MapPost("{email}/{alias}/permission", async (
                string email,
                string alias,
                [FromQuery] string[] permissions,
                [FromServices] ILogger<Program> logger,
                [FromServices] KeyCloakProvider keyCloakHelper,
                [FromServices] Task<IKeyCloakUser?> keyCloakUserTask,
                CancellationToken ct) =>
            {
                if (permissions.Length == 0) return Results.BadRequest("Permissions are empty");
                if (permissions.Any(x => x.AsSpan().Trim().Length > 100))
                    return Results.BadRequest("Permission name is too long");
                var sw = Stopwatch.GetTimestamp();
                var keyCloakUser = await keyCloakUserTask;
                if (string.IsNullOrEmpty(keyCloakUser?.Email)) return Results.BadRequest("User email is null");
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Add permission {c} {u} for user {usr}", alias, keyCloakUser.Email, email);

                var org = await keyCloakHelper.GetOrganization(alias, ct);
                if (org == null) return Results.BadRequest("Organization not found");
                var organizationUsers = await keyCloakHelper.GetOrganizationUsers(org, ct);
                if (organizationUsers.Count == 0 || organizationUsers.All(x =>
                        !email.Equals(x.Email, StringComparison.InvariantCultureIgnoreCase)))
                    await keyCloakHelper.AddUserToOrganization(org, email, ct);

                org.AddUserPermissions(email, permissions);
                if (!await keyCloakHelper.UpdateOrganization(org, ct))
                    return Results.Conflict("Cannot update organization");

                logger.LogInformation("Add permissions {email} {cmp} {perm} by {usr} OK {sw}", email, alias,
                    string.Join(',', permissions), keyCloakUser.Email, sw.Sw());
                return Results.Ok();
            }).WithName("AddCompanyPermissions").WithSummary("Add permission for company")
            .RequireAuthorization(AuthConstants.AdminPolicy);


        group.MapDelete("{email}/{alias}/permission", async (
                string email,
                string alias,
                [FromQuery] string[] permissions,
                [FromServices] ILogger<Program> logger,
                [FromServices] Task<IKeyCloakUser?> keyCloakUserTask,
                [FromServices] KeyCloakProvider keyCloakHelper,
                [FromServices] IFrappeBaseService frappeBaseService,
                CancellationToken ct) =>
            {
                if (permissions.Length == 0) return Results.BadRequest("Permissions are empty");
                var sw = Stopwatch.GetTimestamp();
                var keyCloakUser = await keyCloakUserTask;
                if (keyCloakUser?.Email == null) return Results.BadRequest("User email is null");
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Remove permissions {c} {u} for user {usr} {perm}", alias, keyCloakUser.Email,
                        email,
                        string.Join(',', permissions));

                var org = await keyCloakHelper.GetOrganization(alias, ct);
                if (org == null) return Results.BadRequest("Organization not found");

                var organizationUsers = await keyCloakHelper.GetOrganizationUsers(org, ct);
                if (organizationUsers.All(x => !email.Equals(x.Email, StringComparison.InvariantCultureIgnoreCase)))
                    return Results.BadRequest("User not found");

                if (!org.IsExists(email))
                {
                    await keyCloakHelper.RemoveUserFromOrganization(org, email, ct);
                    return Results.Ok();
                }

                org.RemoveUserPermissions(email, permissions);
                if (!await keyCloakHelper.UpdateOrganization(org, ct))
                    return Results.Conflict("Cannot update organization");
                if (!org.IsExists(email))
                {
                    await keyCloakHelper.RemoveUserFromOrganization(org, email, ct);
                }

                logger.LogInformation("Remove permission {email} {cmp} {perm} by {usr} OK {sw}", email, alias,
                    string.Join(',', permissions), keyCloakUser.Email, sw.Sw());
                return Results.Ok();
            }).WithName("RemoveCompanyPermissions").WithSummary("Remove permission from company")
            .RequireAuthorization(AuthConstants.AdminPolicy);
    }
}