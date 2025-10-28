using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public interface IErpSocialLoginKeyService
{
    Task<FrappeSocialLoginKeyRequest[]?> GetList(int? page = null, int? pageSize = null, CancellationToken ct = default);
    ValueTask<List<FrappeSocialLoginKeyRequest>?> GetAll(CancellationToken ct);
    ValueTask<FrappeSocialLoginKeyRequest?> Get(string name, CancellationToken ct);
    Task<FrappeSocialLoginKeyRequest?> Create(FrappeSocialLoginKeyRequest request, CancellationToken ct);
    Task<FrappeSocialLoginKeyRequest?> Update(string name, FrappeSocialLoginKeyRequest request, CancellationToken ct);
    Task<bool> Delete(string name, CancellationToken ct);
}

public sealed class FrappeSocialLoginKeyService : FrappeBaseService<FrappeSocialLoginKeyRequest>, IErpSocialLoginKeyService
{
    public FrappeSocialLoginKeyService(ILogger<FrappeSocialLoginKeyService> logger, HttpClient httpClient, IMemoryCache memoryCache) : base(logger,
        httpClient, memoryCache)
    {
    }
}

public sealed class FrappeSocialLoginKeyRequest : IFrappeBaseDto
{
    public string Doctype { get; set; } = "Social Login Key";
    public string ProviderName { get; set; } = "Keycloak";
    public string SocialLoginProvider { get; set; } = "Keycloak";
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string AuthorizeUrl { get; set; } = "/protocol/openid-connect/auth";
    public string AccessTokenUrl { get; set; } = "/protocol/openid-connect/token";
    public string BaseUrl { get; set; }
    public string ApiEndpoint { get; set; } = "/protocol/openid-connect/userinfo";

    public string RedirectUrl { get; set; } =
        "/api/method/frappe.integrations.oauth2_logins.login_via_keycloak/keycloak";
    public int Enabled { get; set; } = 1;
    public int EnableSocialLogin {get; set; } = 1;
    public string AuthUrlData { get; set; } = "{\"response_type\": \"code\", \"scope\": \"openid\"}";
}