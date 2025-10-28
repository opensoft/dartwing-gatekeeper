using DartWing.DomainModel.Extensions;
using DartWing.KeyCloak;
using Microsoft.AspNetCore.Authorization;

namespace DartWing.Web.Auth;

internal sealed class AuthPolicyHandler : AuthorizationHandler<AuthPolicyRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthPolicyHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AuthPolicyRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var httpContext = _httpContextAccessor.HttpContext!;
        var routeData = httpContext.GetRouteData();

        var alias = routeData?.Values["alias"]?.ToString();
        if (string.IsNullOrWhiteSpace(alias))
        {
            context.Succeed(requirement);
            return;
        }
        
        if (context.User.IsClientToken())
        {
            if (requirement.AdminOnly())
            {
                context.Fail();
                return;
            }

            context.Succeed(requirement);
            return;
        }

        var email = context.User.GetEmail();
        if (string.IsNullOrWhiteSpace(email))
        {
            context.Fail();
            return;
        }

        var keyCloakProvider = httpContext.RequestServices.GetService<KeyCloakProvider>()!;

        var kcOrg = await keyCloakProvider.GetUserOrganization(_httpContextAccessor.GetUserId()!, alias,
            httpContext.RequestAborted);
        if (kcOrg == null)
        {
            context.Fail();
            return;
        }
        if (!requirement.AdminOnly() || kcOrg.IsAdmin(email, AuthConstants.LedgerCompanyAdminPermission))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}