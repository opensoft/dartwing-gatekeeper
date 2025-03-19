using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.ErpNext.Dto;
using Microsoft.Extensions.Logging;

namespace DartWing.ErpNext;

public sealed class ERPNextService
{
    private readonly ILogger<ERPNextService> _logger;
    private readonly HttpClient _httpClient;

    public ERPNextService(ILogger<ERPNextService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    #region User CRUD

    public async Task<UserCreateResponseDto?> CreateUserAsync(UserCreateRequestDto user, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        const string url = "/api/resource/User";
        var content = SerializeJson(user);

        using var response = await _httpClient.PostAsync(url, content, ct);
        return await HandleResponse<UserCreateResponseDto>(response, sw, ct);
    }

    public async Task<UserCreateResponseDto?> GetUserAsync(string email, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/User/{Uri.EscapeDataString(email)}";
        using var response = await _httpClient.GetAsync(url, ct);
        return await HandleResponse<UserCreateResponseDto>(response, sw, ct);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(string email, UserCreateRequestDto updateData, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/User/{Uri.EscapeDataString(email)}";
        var content = SerializeJson(updateData);

        using var response = await _httpClient.PutAsync(url, content, ct);
        return await HandleResponse<UserResponseDto>(response, sw, ct);
    }

    public async Task<bool> DeleteUserAsync(string email, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/User/{Uri.EscapeDataString(email)}";
        using var response = await _httpClient.DeleteAsync(url, ct);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Role CRUD

    public async Task<RolesResponseDto?> GetAllRolesAsync(CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        const string url = "/api/resource/Role";
        using var response = await _httpClient.GetAsync(url, ct);
        return await HandleResponse<RolesResponseDto>(response, sw, ct);
    }

    public async Task<RoleDto?> GetRoleAsync(string roleName, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/Role/{Uri.EscapeDataString(roleName)}";
        using var response = await _httpClient.GetAsync(url, ct);
        return await HandleResponse<RoleDto>(response, sw, ct);
    }

    #endregion
    
    #region Company CRUD
    
    public async Task<CompanyResponseDto<CompanyDto>?> CreateCompanyAsync(CreateCompanyDto companyDto, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        const string url = "/api/resource/Company";
        var content = SerializeJson(companyDto);
        var response = await _httpClient.PostAsync(url, content, ct);
        return await HandleResponse<CompanyResponseDto<CompanyDto>>(response, sw, ct);
    }

    public async Task<CompanyResponseDto<CompanyDto>?> GetCompanyAsync(string companyName, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/Company/{Uri.EscapeDataString(companyName)}";
        using var response = await _httpClient.GetAsync(url, ct);
        return await HandleResponse<CompanyResponseDto<CompanyDto>>(response, sw, ct);
    }

    public async Task<CompanyResponseDto<CompanyDto>?> UpdateCompanyAsync(string companyName, UpdateCompanyDto updateDto, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/Company/{Uri.EscapeDataString(companyName)}";
        var content = SerializeJson(updateDto);
        var response = await _httpClient.PutAsync(url, content, ct);
        return await HandleResponse<CompanyResponseDto<CompanyDto>>(response, sw, ct);
    }

    public async Task<bool> DeleteCompanyAsync(string companyName, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        var url = $"/api/resource/Company/{Uri.EscapeDataString(companyName)}";
        var response = await _httpClient.DeleteAsync(url, ct);
        return await HandleResponse<bool>(response, sw, ct);
    }

    #endregion

    #region Helpers

    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new CustomDateTimeConverter() }
        };
    private static ByteArrayContent SerializeJson(object data)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data, JsonSerializerOptions);
        return new ByteArrayContent(json);
    }

    private async Task<T?> HandleResponse<T>(HttpResponseMessage response, long timestamp, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync(ct);
            var request = response.RequestMessage?.Content != null
                ? await response.RequestMessage!.Content.ReadAsStringAsync(ct)
                : "";
            _logger.LogWarning("erpNext {type} {url} code={r} response={b} request={req} {sw}", response.RequestMessage?.Method.Method,
                response.RequestMessage?.RequestUri?.AbsoluteUri, response.StatusCode, contentString, request,
                Stopwatch.GetElapsedTime(timestamp));

            return default;
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var request = response.RequestMessage?.Content != null
                ? await response.RequestMessage!.Content.ReadAsStringAsync(ct)
                : "";
            _logger.LogDebug("erpNext {type} {url} code={r} response={b} request={req} {sw}", response.RequestMessage?.Method.Method,
                response.RequestMessage?.RequestUri?.AbsoluteUri, response.StatusCode, Encoding.UTF8.GetString(content),
                request, Stopwatch.GetElapsedTime(timestamp));
        }
        else
        {
            _logger.LogInformation("erpNext {type} {url} code={r} {sw}", response.RequestMessage?.Method.Method,
                response.RequestMessage?.RequestUri?.AbsoluteUri, response.StatusCode,
                Stopwatch.GetElapsedTime(timestamp));
        }

        if (typeof(T) == typeof(bool)) return (T)(object)response.IsSuccessStatusCode;
        
        var result = JsonSerializer.Deserialize<T>(content, JsonSerializerOptions)!;
        return result;
    }

    #endregion
}

