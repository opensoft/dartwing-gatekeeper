using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DartWing.KeyCloak;

internal static class KeyCloakHelpers
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
        {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

    private static readonly MediaTypeHeaderValue JsonMediaType = MediaTypeHeaderValue.Parse("application/json");
    
    public static string? GetEmailFromJwtToken(string jwtToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);

        var email = jwt.Claims
            .FirstOrDefault(c => c.Type is "email" or ClaimTypes.Email or "upn" or "preferred_username")?.Value;

        return email;
    }

    public static Task<(bool success, string location)> Post<T>(this HttpClient client, string url, string accessToken, T body,
        ILogger logger, CancellationToken ct) where T : class
    {
        return client.Send<T, bool>(HttpMethod.Post, url, accessToken, body, logger, ct);
    }
    
    public static async Task<bool> Put<T>(this HttpClient client, string url, string accessToken, T body,
        ILogger logger, CancellationToken ct) where T : class
    {
        var (success, _) = await client.Send<T, bool>(HttpMethod.Put, url, accessToken, body, logger, ct);

        return success;
    }
    
    public static async Task<T?> Get<T>(this HttpClient client, string url, string accessToken, ILogger logger, CancellationToken ct) where T : class
    {
        var (response, _) = await client.Send<object, T>(HttpMethod.Get, url, accessToken, null, logger, ct);

        return response;
    }
    
    public static async Task<IReadOnlyList<T>> GetAll<T>(this HttpClient client, string url, string accessToken, ILogger logger, CancellationToken ct) where T : class
    {
        const int max = 101;
        var fullUrl = $"{url}?first=0&max={max}";

        var (response, _) = await client.Send<object, T[]>(HttpMethod.Get, fullUrl, accessToken, null, logger, ct);
        if (response == null) return [];
        if (response.Length < max) return response;
        
        var result = new List<T>(response);
        var i = 1;
        
        do
        {
            fullUrl = $"{url}?first={result.Count}&max={max}";
            var (r, _) = await client.Send<object, T[]>(HttpMethod.Get, fullUrl, accessToken, null, logger, ct);
            if (r == null) return [];
            result.AddRange(r);
            i++;
            if (i > 1000) break;
        }
        while (response.Length == max) ;

        return response;
    }
    
    public static async Task<bool> Delete(this HttpClient client, string url, string accessToken, ILogger logger, CancellationToken ct)
    {
        var (response, _) = await client.Send<object, bool>(HttpMethod.Delete, url, accessToken, null, logger, ct);

        return response;
    }

    private static async Task<(TOut?, string location)> Send<TIn, TOut>(this HttpClient client, HttpMethod method, string url,
        string accessToken, TIn? body, ILogger logger, CancellationToken ct) where TIn : class
    {
        var sw = Stopwatch.GetTimestamp();

        using var requestMessage = new HttpRequestMessage(method, url);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body != null)
        {
            requestMessage.Content = typeof(TIn) == typeof(string)
                ? new StringContent((string)(object)body) { Headers = { ContentType = JsonMediaType } }
                : JsonContent.Create(body, options: SerializerOptions);
        }

        var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var req = body == null ? "" : JsonSerializer.Serialize(body, SerializerOptions);
            var resp = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            logger.LogWarning("KeyCloak {r} {url} request={req} response={body} {sw}", method.Method, url, req, resp,
                Stopwatch.GetElapsedTime(sw));

            return (default, "");
        }

        var location = "";
        if (method == HttpMethod.Post && response.Headers.TryGetValues("location", out var loc))
        {
            location = loc.FirstOrDefault() ?? "";
            var ind = location.LastIndexOf('/');
            if (ind > 0) location = location[(ind + 1)..];
        }
        
        var jsonResponse = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("KeyCloak {r} {url} response={body} {sw}", method.Method, url,
                System.Text.Encoding.UTF8.GetString(jsonResponse), Stopwatch.GetElapsedTime(sw));
        }
        else if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("KeyCloak {r} {url} {sw}", method.Method, url, Stopwatch.GetElapsedTime(sw));
        }

        return typeof(TOut) == typeof(bool)
            ? ((TOut)(object)true, location)
            : (JsonSerializer.Deserialize<TOut>(jsonResponse, SerializerOptions)!, location);
    }
}