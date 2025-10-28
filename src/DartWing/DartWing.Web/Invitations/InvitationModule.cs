using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.DomainModel.Extensions;
using DartWing.KeyCloak;
using DartWing.Web.Auth;
using DartWing.Web.Emails;
using DartWing.Web.Users;
using DartWing.Web.Users.Dto;
using LedgerLinc.ApiService.Users.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DartWing.Web.Invitations;

public static class InvitationModule
{
    private static readonly JsonSerializerOptions SerOpt = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };
    
    public static void RegisterInvitationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/invitations/{alias}").WithTags("Invitation")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapPost("", async (
                [FromBody] CreateInvitationRequest request,
                [FromServices] KeyCloakProvider keyCloakProvider,
                [FromServices] InvitationSettings invitationSettings,
                [FromServices] LoopsAdapter emailAdapter,
                [FromServices] ILogger<Program> logger,
                [FromServices] IMemoryCache memoryCache,
                ClaimsPrincipal principal,
                string alias,
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();

                var org = await keyCloakProvider.GetOrganization(alias, ct);
                if (org == null) return Results.Conflict();

                request.Email = request.Email.Trim().ToLowerInvariant();
                var now = DateTime.UtcNow;
                var invitedUser = await keyCloakProvider.GetUserByEmail(request.Email, ct);
                if (invitedUser != null)
                {
                    var invitedUserCompanies = await keyCloakProvider.GetUserOrganizations(invitedUser.Id, ct);
                    if (invitedUserCompanies.Any(x => x.Id == org.Id))
                    {
                        logger.LogInformation("User {userId} already added to company {companyId}", invitedUser.Id, org.Id);
                        return Results.Accepted("User already added");
                    }

                    await keyCloakProvider.AddUserToOrganization(org, invitedUser.Id, ct);

                    org.AddUserPermissions(invitedUser.Email, AuthConstants.LedgerCompanyMangePermissions);
                    await keyCloakProvider.UpdateOrganization(org, ct);
                    
                    memoryCache.Remove(invitedUser.Id);
                    
                    var addedEmail = EmailBuilder.Added(request.Email, org.Name);
                    await emailAdapter.SendTransactionalEmailAsync(addedEmail, ct);

                    logger.LogInformation("User {userId} added to company {companyId}", invitedUser.Id, org.Id);
                    return Results.Accepted("User added");
                }

                
                var defaultUser = await keyCloakProvider.GetDefaultUser(ct);
                var invitations = defaultUser.GetInvitations().Select(x => JsonSerializer.Deserialize<Invitation>(x, SerOpt))
                    .ToList();
                var invitationCount =invitations.Count(
                    x => x.Email == request.Email && x.ExpireDate > now && (x.InvitationResult == EInvitationResult.None ||
                                                                       x.InvitationResult == EInvitationResult.Renew));

                if (invitationCount > 10) 
                    return Results.Conflict("Too many invitations");

                var activeInvitation = invitations.FirstOrDefault(x =>
                    x.OrgId == org.Id && x.ExpireDate > now && x.InvitedUserEmail == request.Email &&
                    x.InvitationResult == EInvitationResult.None);

                if (activeInvitation != null)
                {
                    activeInvitation.Used = now;
                    activeInvitation.InvitationResult = EInvitationResult.Renew;

                    await keyCloakProvider.UpdateUserInvitations(defaultUser.Id,
                        invitations.Select(x => JsonSerializer.Serialize(x, SerOpt)).ToArray(), ct);
                }

                var code = VerificationCodeHelper.GenerateCode(invitationSettings.VerificationCodeLength);

                var inv = new Invitation
                {
                    Email = principal.GetEmail(),
                    OrgId = org.Id,
                    Created = now,
                    InvitedUserEmail = request.Email,
                    InvitedUserName = request.UserName,
                    InvitationType = EInvitationType.Email,
                    VerificationCode = code,
                    ExpireDate = now.AddHours(invitationSettings.TtlHours)
                };

                invitations.Add(inv);
                await keyCloakProvider.UpdateUserInvitations(defaultUser.Id,
                    invitations.Select(x => JsonSerializer.Serialize(x, SerOpt)).ToArray(), ct);
                var email = EmailBuilder.Invite(request.Email, org.Name, code);
                await emailAdapter.SendTransactionalEmailAsync(email, ct);

                logger.LogInformation("Invitation to company {companyId} sent to email {email}", org.Id, request.Email);
                
                return Results.Ok();
            }).WithName("Create invitation").WithSummary("Create invitation");

        group.MapDelete("", async (
            [FromBody] RevokeInvitationRequest request,
            [FromServices] KeyCloakProvider keyCloakProvider,
            ClaimsPrincipal principal,
            string alias,
            CancellationToken ct) =>
        {
            var sw = Stopwatch.GetTimestamp();
            var userId = principal.GetUserId();
            var org = await keyCloakProvider.GetOrganization(alias, ct);
            if (org == null) return Results.Conflict();
            var now = DateTime.UtcNow;

            request.Email = request.Email.Trim().ToLowerInvariant();
            var defaultUser = await keyCloakProvider.GetDefaultUser(ct);
            var invitations = defaultUser.GetInvitations().Select(x => JsonSerializer.Deserialize<Invitation>(x, SerOpt))
                .ToList();

            var activeInvitation = invitations.FirstOrDefault(
                x => x.OrgId == org.Id && x.VerificationCode == request.VerificationCode &&
                     x.InvitedUserEmail == request.Email && x.InvitationResult == EInvitationResult.None);

            if (activeInvitation == null) return Results.Conflict();
            activeInvitation.Used = now;
            activeInvitation.InvitationResult = EInvitationResult.Revoked;

            await keyCloakProvider.UpdateUserInvitations(defaultUser.Id,
                invitations.Select(x => JsonSerializer.Serialize(x)).ToArray(), ct);

            return Results.Ok();
        }).WithName("Revoke invitation").WithSummary("Revoke invitation").WithOpenApi();

        group.MapGet("", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromServices] KeyCloakProvider keyCloakProvider,
                ClaimsPrincipal principal,
                string alias,
                CancellationToken ct) =>
            {
                var userId = principal.GetUserId();
                var org = await keyCloakProvider.GetOrganization(alias, ct);
                if (org == null) return Results.Conflict();

                var defaultUser = await keyCloakProvider.GetDefaultUser(ct);
                var invitations = defaultUser.GetInvitations().Select(x => JsonSerializer.Deserialize<Invitation>(x, SerOpt))
                    .ToList();

                page = page > 0 ? page - 1 : 0;
                pageSize = pageSize > 100 ? 100 : pageSize;
                if (pageSize <= 0) pageSize = 10;

                var result = invitations.OrderByDescending(x => x.Created).Skip(page * pageSize).Take(pageSize)
                    .Select(x => new InvitationModel
                {
                    UserEmail = x.Email,
                    InvitationResult = x.InvitationResult,
                    InvitationType = x.InvitationType,
                    VerificationCode = x.VerificationCode,
                    InvitedUserEmail = x.InvitedUserEmail,
                    CreatedUtc = x.Created,
                    UsedUtc = x.Used,
                    ExpireDateUtc = x.ExpireDate
                }).ToList();

                return Results.Ok(Wrapper<InvitationModel>.Create(result, page + 1, pageSize));
            }).WithName("Get invitations").WithSummary("Get invitations for company").WithOpenApi()
            .Produces<Wrapper<InvitationModel>>();

        endpoints.MapPost("api/invitations/verify", async (
                [FromBody] InvitationRequest request,
                [FromServices] KeyCloakProvider keyCloakProvider,
                IMemoryCache memoryCache,
                ClaimsPrincipal principal,
                IHttpContextAccessor httpContextAccessor,
                CancellationToken ct) =>
            {
                var sw = Stopwatch.GetTimestamp();
                var userId = principal.GetUserId();
                var memoryCacheKey = "invitation@@" + userId;
                var now = DateTime.UtcNow;
                if (memoryCache.TryGetValue(memoryCacheKey, out List<DateTime>? dateTimes) && dateTimes != null)
                {
                    if (dateTimes.Count(x => x > now.AddMinutes(-10)) > 3)
                        return Results.StatusCode(429);
                }

                var defaultUser = await keyCloakProvider.GetDefaultUser(ct);
                var invitations = defaultUser.GetInvitations().Select(x => JsonSerializer.Deserialize<Invitation>(x, SerOpt))
                    .ToList();
                
                var activeInvitation = invitations.FirstOrDefault(x =>
                    x.ExpireDate > now && x.VerificationCode == request.VerificationCode &&
                    x.InvitationResult == EInvitationResult.None);

                if (activeInvitation == null)
                {
                    if (dateTimes == null)
                    {
                        dateTimes = [DateTime.UtcNow];
                        memoryCache.Set(memoryCacheKey, dateTimes, TimeSpan.FromMinutes(20));
                    }
                    else
                        dateTimes.Add(DateTime.UtcNow);

                    return Results.Conflict();
                }

                var user = await keyCloakProvider.GetUserByEmail(activeInvitation.Email, ct);

                activeInvitation.Used = now;
                activeInvitation.InvitationResult =
                    request.Accepted ? EInvitationResult.Accepted : EInvitationResult.Rejected;

                var comp = await keyCloakProvider.GetOrganization(activeInvitation.OrgId, ct);
                if (request.Accepted)
                {
                    await keyCloakProvider.AddUserToOrganization(comp, user.Id, ct);
                }

                if (!request.Accepted)
                {
                    return Results.Json(new { });
                }
                comp.AddUserPermissions(user.Email, AuthConstants.LedgerCompanyMangePermissions);
                await keyCloakProvider.UpdateOrganization(comp, ct);
                
                var companies = await keyCloakProvider.GetUserOrganizationsWithPermissions(user.Id, ct);
                var result = companies
                    .Select(x => new CompanyRoleResponse(x, x.GetUserPermissions(user.Email)))
                    .Where(x => !string.IsNullOrEmpty(x.Site)).ToArray();

                return Results.Ok(new UserWithCompaniesResponse(result)
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Username,
                    Company = comp.Name,
                    Site = comp.GetSite()
                });
            }).WithTags("Invitation").WithName("Accept/reject invitation").WithSummary("Accept/reject invitation")
            .WithOpenApi().RequireAuthorization().Produces<UserWithCompaniesResponse>();
    }
}