using Microsoft.AspNetCore.Authorization;

namespace DartWing.Web.Auth;

internal sealed class AuthPolicyRequirement : IAuthorizationRequirement
{
    private readonly bool _admin;
    
    public AuthPolicyRequirement(bool admin)
    {
        _admin = admin;
    }

    public bool AdminOnly() => _admin;
}