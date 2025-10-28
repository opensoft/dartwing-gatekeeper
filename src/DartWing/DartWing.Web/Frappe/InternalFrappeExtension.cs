using DartWing.DomainModel.Extensions;
using DartWing.Frappe;
using DartWing.Frappe.Erp;
using DartWing.Frappe.Healthcare;
using DartWing.KeyCloak;

namespace DartWing.Web.Frappe;

internal static class InternalFrappeExtension
{
    public static IServiceCollection AddFrappeInternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<FrappeSiteHostedService>();
        services.AddSingleton<IFrappeSiteStorage, FrappeSiteMemoryStorage>();
        
        var settings = new FrappeSiteSettings();
        configuration.Bind("FrappeSite", settings);
        services.AddSingleton(settings);
        services.AddHttpClient<FrappeSiteService>((p, c) =>
        {
            var sett = p.GetService<FrappeSiteSettings>()!;
            c.BaseAddress = new Uri(sett.SetupUrl);
            c.Timeout = TimeSpan.FromSeconds(sett.TimeoutSec);
        });
        
        services.AddScoped<Task<IErpCompanyService?>>(async x => await GetFrappeService<FrappeCompanyService>(x));
        services.AddScoped<Task<IErpUserService?>>(async x => await GetFrappeService<FrappeUserService>(x));
        services.AddScoped<Task<IErpFileService?>>(async x => await GetFrappeService<FrappeFileService>(x));
        services.AddScoped<Task<FrappePatientService?>>(async x => await GetFrappeService<FrappePatientService>(x));
        services.AddScoped<Task<IFrappeUser?>>(async x =>
        {
            var contextAccessor = x.GetService<IHttpContextAccessor>();
            var email = contextAccessor?.GetEmail();
            if (string.IsNullOrEmpty(email)) return null;
            var userService = await x.GetFrappeUserService();
            if (userService is null) return null;
            return await userService.Get(email, contextAccessor!.HttpContext!.RequestAborted);
        });
        services.AddScoped<Task<IFrappeCompany?>>(async x =>
        {
            var contextAccessor = x.GetService<IHttpContextAccessor>()!;
            var company = contextAccessor.HttpContext!.GetRouteData().Values["company"]?.ToString();
            if (string.IsNullOrEmpty(company)) return null;
            var service = await x.GetFrappeCompanyService(company)!;
            if (service is null) return null;
            return await service.Get(company, contextAccessor!.HttpContext!.RequestAborted);
        });

        return services;
    }

    public static async Task<IErpCompanyService?> GetFrappeCompanyService(this IServiceProvider x,
        string? company = null, string? site = null)
    {
        return await x.GetFrappeService<FrappeCompanyService>(company, site, serviceAccount: true);
    }

    public static async Task<IErpUserService?> GetFrappeUserService(this IServiceProvider x, string? company = null)
    {
        return await x.GetFrappeService<FrappeUserService>(company, serviceAccount: true);
    }
    
    private static async ValueTask<T?> GetFrappeService<T>(this IServiceProvider x, string? company = null, string? siteUrl = null,
        bool serviceAccount = false) where T : class, IFrappeBaseService
    {
        var context = x.GetService<IHttpContextAccessor>()!;
        if (string.IsNullOrEmpty(siteUrl))
        {
            siteUrl = context.HttpContext!.GetRouteData().Values["site"]?.ToString();
            if (string.IsNullOrEmpty(siteUrl))
            {
                if (string.IsNullOrEmpty(company))
                    company = context.HttpContext!.GetRouteData().Values["company"]?.ToString();
                if (string.IsNullOrEmpty(company)) return null;
            }
            else
            {
                siteUrl = "https://" + siteUrl;
            }
        }

        var site = siteUrl ?? await context.GetFrappeSite(company);
        if (string.IsNullOrWhiteSpace(site)) return null;
        var userEmail = serviceAccount || context.IsClientToken() ? "" : context.GetEmail();
        var keyCloakProvider = x.GetService<KeyCloakProvider>()!;
        var user = string.IsNullOrEmpty(userEmail)
            ? await keyCloakProvider.GetDefaultUser(context.HttpContext!.RequestAborted)
            : await keyCloakProvider.GetUserByEmail(userEmail, context.HttpContext!.RequestAborted);
        var secret = user?.GetFrappeSecret(new Uri(site).Host);
        if (string.IsNullOrWhiteSpace(secret))
        {
            user = await keyCloakProvider.GetDefaultUser(context.HttpContext!.RequestAborted);
            secret = user!.GetFrappeSecret(new Uri(site).Host);
        }

        return x.GetService<IFrappeService>()!.GetService<T>(site, $"token {secret}");
    }
    
    internal static async ValueTask<T?> GetFrappeServiceForBackground<T>(this IServiceProvider x, string siteUrl, CancellationToken ct) where T : class, IFrappeBaseService
    {
        var keyCloakProvider = x.GetService<KeyCloakProvider>()!;
        var user = await keyCloakProvider.GetDefaultUser(ct);
        var secret = user?.GetFrappeSecret(new Uri(siteUrl).Host);
        if (string.IsNullOrWhiteSpace(secret))
        {
            x.GetService<ILogger<Program>>()!.LogWarning("Frappe secret not found for {s}", new Uri(siteUrl).Host);
        }
        return x.GetService<IFrappeService>()!.GetService<T>(siteUrl, $"token {secret}");
    }

    private static async ValueTask<string?> GetFrappeSite(this IHttpContextAccessor context, string? company = null)
    {
        var c = company ?? context.HttpContext!.GetRouteData().Values["company"]?.ToString();
        if (string.IsNullOrEmpty(c)) return "";
        var userId = context.GetUserId();
        var keyCloakProvider = context.HttpContext!.RequestServices.GetService<KeyCloakProvider>()!;
        var userOrganizations = await keyCloakProvider.GetUserOrganizations(userId, context.HttpContext!.RequestAborted);
        var org = userOrganizations?.FirstOrDefault(x => x.Name.EndsWith("__" + c));
        return (org?.Name)[..org.Name.IndexOf("__", StringComparison.OrdinalIgnoreCase)];
    }
}