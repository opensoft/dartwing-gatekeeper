using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.DomainModel.Extensions;

public static class HttpContextExtensions
{
    public static string? GetEmail(this ClaimsPrincipal  user)
    {
        var email = user.Claims
            .FirstOrDefault(c => c.Type is "email" or ClaimTypes.Email or "upn" or "preferred_username")?.Value;

        return string.IsNullOrWhiteSpace(email) ? "" : email;
    }
    
    public static string? GetUserId(this ClaimsPrincipal  user)
    {
        return user.FindFirst("sub")?.Value;
    }

    public static string? GetUserId(this IHttpContextAccessor context)
    {
        return context.HttpContext?.User?.GetUserId();
    }
    
    public static string? GetEmail(this IHttpContextAccessor context)
    {
        return context.HttpContext?.User?.GetEmail();
    }
    
    public static string? GetName(this IHttpContextAccessor context)
    {
        return context.HttpContext?.User?.Claims
            .FirstOrDefault(c => c.Type == "name")?.Value;
    }
    
    public static bool IsClientToken(this IHttpContextAccessor context)
    {
        return context.HttpContext?.User?.IsClientToken() ?? false;
    }
    
    public static bool IsClientToken(this ClaimsPrincipal user)
    {
        return !string.IsNullOrEmpty(user?.FindFirst("client_id")?.Value);
    }

    public static string GetToken(this IHttpContextAccessor context)
    {
        var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
        {
            return authorizationHeader["Bearer ".Length..].Trim();
        }
        return "";
    }
    
   
    public static T Create<T>(this IHttpContextAccessor context, params object[] args)
    {
        return ActivatorUtilities.CreateInstance<T>(context.HttpContext!.RequestServices, args);
    }
}