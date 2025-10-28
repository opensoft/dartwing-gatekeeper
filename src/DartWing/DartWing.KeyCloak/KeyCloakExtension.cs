using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using DartWing.KeyCloak.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.KeyCloak;

public static class KeyCloakExtension
{
    public static IServiceCollection AddKeyCloak(this IServiceCollection services,
        ConfigurationManager configuration, string keyCloakSection = "KeyCloak")
    {
        var settings = new KeyCloakSettings();
        configuration.Bind(keyCloakSection, settings);

        return services.AddKeyCloak(settings);
    }

    public static IServiceCollection AddKeyCloak(this IServiceCollection services,
        KeyCloakSettings settings)
    {
        services.AddHttpClient("KeyCloak");
        services.AddSingleton(settings);
        services.AddSingleton<KeyCloakSecurityKeysProvider>();
        services.AddMemoryCache();
        services.AddSingleton<KeyCloakProvider>();
        services.AddSingleton<KeyCloakTokenProvider>();
        services.AddSingleton<KeyCloakClient>();
        services.AddScoped<Task<IKeyCloakUser?>>(async x =>
        {
            var contextAccessor = x.GetService<IHttpContextAccessor>();
            var userId = contextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;
            if (userId == null) return null;
            var keyCloakProvider = x.GetService<KeyCloakProvider>()!;
            return await keyCloakProvider.GetUserById(userId, contextAccessor!.HttpContext.RequestAborted);
        });
        return services;
    }

    public static bool IsExists(this KeyCloakOrganization organization, string email)
    {
        return organization.Attributes.TryGetValue(email.Trim().ToLowerInvariant(), out var value) && value.Length > 0;
    }
    
    public static bool IsAdmin(this KeyCloakOrganization organization, string email, string adminPermission)
    {
        return organization.Attributes.TryGetValue(email.Trim().ToLowerInvariant(), out var value) && value.Length > 0 && value[0].Contains(adminPermission);
    }

    public static KeyCloakOrganization AddSite(this KeyCloakOrganization organization, string site)
    {
        organization.Attributes["frappe__site"] = [site.Trim()];
        return organization;
    }

    public static string GetSite(this KeyCloakOrganization organization)
    {
        return organization.Attributes.TryGetValue("frappe__site", out var value) && value?.Length > 0 ? value[0] : "";
    }

    public static KeyCloakOrganization AddAddress(this KeyCloakOrganization organization,
        KeyCloakOrganizationAddress address)
    {
        const string key = "address";
        organization.Attributes[key] = [JsonSerializer.Serialize(address)];
        return organization;
    }
    
    public static KeyCloakOrganizationAddress GetAddress(this KeyCloakOrganization organization)
    {
        const string key = "address";
        if (!organization.Attributes.TryGetValue(key, out var value) || value.Length == 0 || string.IsNullOrWhiteSpace(value[0]))
            return new KeyCloakOrganizationAddress();
        try
        {
            return JsonSerializer.Deserialize<KeyCloakOrganizationAddress>(value[0]) ??
                   new KeyCloakOrganizationAddress();
        }
        catch
        {
            // ignored
        }

        return new KeyCloakOrganizationAddress();
    }

    public static KeyCloakOrganization AddUserPermissions(this KeyCloakOrganization organization, string email,
        string[] permissions)
    {
        var key = email.Trim().ToLowerInvariant();
        if (!organization.Attributes.TryGetValue(key, out var value) || value?.Length == 0)
        {
            organization.Attributes[key] = [string.Join(',', permissions.Select(x => x.Trim()))];
            return organization;
        }

        organization.Attributes[key] =
        [
            string.Join(',',
                value![0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Union(permissions.Select(x => x.Trim())))
        ];

        return organization;
    }

    public static KeyCloakOrganization RemoveUserPermissions(this KeyCloakOrganization organization, string email,
        string[] permissions)
    {
        var key = email.Trim().ToLowerInvariant();
        if (!organization.Attributes.TryGetValue(key, out var value) || value.Length == 0) return organization;
        var newPermissions = value[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Except(permissions.Select(x => x.Trim())).ToArray();
        if (newPermissions.Length == 0)
        {
            organization.Attributes.Remove(key);
            return organization;
        }

        organization.Attributes[key] = [string.Join(',', newPermissions)];
        return organization;
    }

    public static string[] GetInvitations(this IKeyCloakUser user)
    {
        return user.GetUsr("ledgerLincInvitation");
    }
    
    public static IKeyCloakUser SetInvitations(this IKeyCloakUser user, string[] invitations)
    {
        return user.SetUsr("ledgerLincInvitation", invitations);
    }
    
    private static string[] GetUsr(this IKeyCloakUser user, string key)
    {
        if (!user.Attributes.TryGetValue(key, out var value) || value.Length == 0) return [];
        return value;
    }
    
    private static IKeyCloakUser AddUsr(this IKeyCloakUser user, string key, string path)
    {
        var arr = user.Attributes[key];
        user.Attributes[key] = arr.Union([path]).ToArray();
        return user;
    }
    
    private static IKeyCloakUser SetUsr(this IKeyCloakUser user, string key, string[] path)
    {
        user.Attributes[key] = path;
        return user;
    }

    
    public static string[] GetUserPermissions(this KeyCloakOrganization organization, string email)
    {
        var key = email.Trim().ToLowerInvariant();
        if (!organization.Attributes.TryGetValue(key, out var value) || value.Length == 0) return [];
        return value[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static string? GetFrappeSecret(this IKeyCloakUser keyCloakUser, string host)
    {
        return GetFrappeFullSecret(keyCloakUser, host)?[(host.Length + 1)..];
    }
    
    public static string? GetFrappeFullSecret(this IKeyCloakUser keyCloakUser, string host)
    {
        if (!keyCloakUser.Attributes.TryGetValue("frappeSecrets", out var value) || value!.Length == 0) return null;
        return value.FirstOrDefault(x => x.StartsWith(host));
    }
    
    public static void AddFrappeSecret(this IKeyCloakUser keyCloakUser, string value)
    {
        if (!keyCloakUser.Attributes.TryGetValue("frappeSecrets", out var values) || values.Length == 0)
            keyCloakUser.Attributes["frappeSecrets"] = [value];
        else 
            keyCloakUser.Attributes["frappeSecrets"] = values.Union([value]).ToArray();
    }

    public static void RemoveFrappeSecret(this IKeyCloakUser keyCloakUser, string value)
    {
        if (keyCloakUser.Attributes.TryGetValue("frappeSecrets", out var values) && values?.Length > 0)
            keyCloakUser.Attributes["frappeSecrets"] = values.Where(x => x != value).ToArray();
    }

    public static string GetUseServiceToken(this KeyCloakOrganization organization) => organization.Get("storageTokenType");

    public static KeyCloakOrganization AddUseServiceToken(this KeyCloakOrganization org, string email) =>
        org.Add("storageTokenType", email);
    
    public static string GetCompanyOwner(this KeyCloakOrganization organization) => organization.Get("owner");

    public static KeyCloakOrganization AddCompanyOwner(this KeyCloakOrganization org, string email) =>
        org.Add("owner", email);
    
    public static string GetPath(this KeyCloakOrganization organization) => organization.Get("path");

    public static KeyCloakOrganization AddPath(this KeyCloakOrganization org, string path) => org.Add("path", path);

    public static string GetMsTenantId(this KeyCloakOrganization organization) => organization.Get("msTenantId");
    public static KeyCloakOrganization AddMsTenantId(this KeyCloakOrganization organization, string path) =>
        organization.Add("msTenantId", path);

    public static string? GetCreatedDate(this KeyCloakOrganization organization) =>
        organization.Get("dateCreated", DateTimeOffset.UtcNow.ToString("O"));
    public static KeyCloakOrganization AddCreatedDate(this KeyCloakOrganization organization, DateTimeOffset dateTime) =>
        organization.Add("dateCreated", dateTime.ToString("O"));
    
    private static string Get(this KeyCloakOrganization organization, string key, string defaultValue = "")
    {
        if (!organization.Attributes.TryGetValue(key, out var value) || value.Length == 0) return defaultValue;
        return value[0];
    }
    
    private static KeyCloakOrganization Add(this KeyCloakOrganization organization, string key, string path)
    {
        organization.Attributes[key] = [path];
        return organization;
    }

    public static string CompanyName(string site, string company) => $"{site}__{company}";

    public static (string? UserId, string? Email) GetUserIdAndEmail(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

// Get the "sub" claim
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        return (sub, email);
    }

    public static KeyCloakOrganization AddStorageType(this KeyCloakOrganization org, string storageType) =>
        org.Add("storageType", storageType);
    
    public static string GetStorageType(this KeyCloakOrganization organization) => organization.Get("storageType");
}