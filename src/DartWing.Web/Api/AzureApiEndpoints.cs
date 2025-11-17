using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DartWing.Microsoft;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DartWing.Web.Api;

public static class AzureApiEndpoints
{
    public static void RegisterAzureApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("api/azure/auth/callback").WithTags("Azure");

        group.MapGet("", async ([FromQuery] string code,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IMemoryCache memoryCache,
            [FromServices] MicrosoftSettings settings,
            CancellationToken ct) =>
        {
            var client = httpClientFactory.CreateClient("Azure");

            var token = await GetAccessToken(client, "common", settings.ClientId, settings.ClientSecret, code, settings.RedirectUri, ct);
            await CallGraphApi(client, token);
            var folders = await new GraphApiAdapter(token, memoryCache).GetMyFolders(ct);

            return Results.Json(folders);
        }).WithName("AzureAuthCallback").WithSummary("Azure auth callback");
        
        group.MapGet("service", async ([FromQuery] string code,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IMemoryCache memoryCache,
            [FromServices] MicrosoftServiceSettings serviceSettings,
            CancellationToken ct) =>
        {
            var client = httpClientFactory.CreateClient("Azure");

            var userToken = await GetAccessToken(client, "common", serviceSettings.ClientId, serviceSettings.ClientSecret, code, serviceSettings.RedirectUri, ct);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(userToken);

            var tenantId = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

            var token = await GetAccessToken(client, tenantId, serviceSettings.ClientId, serviceSettings.ClientSecret, "", serviceSettings.RedirectUri, ct);
            await CallGraphApi(client, token);
            var folders = await new GraphApiAdapter(token, memoryCache).GetMyFolders(ct);

            return Results.Json(folders);
        }).WithName("AzureAuthServiceCallback").WithSummary("Azure service auth callback");
    }

    private static async Task<string?> GetAccessToken(HttpClient client, string tenantIdOrAccountType, string clientId,
        string clientSecret, string code, string redirectUri, CancellationToken ct)
    {
        var tokenUrl = $"https://login.microsoftonline.com/{tenantIdOrAccountType}/oauth2/v2.0/token";
        var dict = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            {
                "scope", "openid profile email Bookings.ReadWrite.All"
            } //Files.Read.All Bookings.ReadWrite.All offline_access
        };

        if (string.IsNullOrEmpty(code))
        {
            dict["grant_type"] = "client_credentials";
        }
        else
        {
            dict["grant_type"] = "authorization_code";
            dict["code"] = code;
        }

        var requestBody = new FormUrlEncodedContent(dict);

        var response = await client.PostAsync(tokenUrl, requestBody, ct);
        var responseBody = await response.Content.ReadAsByteArrayAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error getting token: {Encoding.UTF8.GetString(responseBody)}");
            return null;
        }

        var json = JsonDocument.Parse(responseBody);
        var accessToken = json.RootElement.GetProperty("access_token").GetString();
        return accessToken;
    }

    private static async Task CallGraphApi(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Graph API Response:");
            Console.WriteLine(responseBody);
        }
        else
        {
            Console.WriteLine($"Error calling Graph API: {responseBody}");
        }
    }
}