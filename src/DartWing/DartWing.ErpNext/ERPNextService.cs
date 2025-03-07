using System.Text.Json;
using DartWing.ErpNext.Dto;

namespace DartWing.ErpNext;

internal sealed class ERPNextService
{
    private readonly HttpClient _httpClient;
    private readonly ERPNextSettings _settings;

    public ERPNextService(HttpClient httpClient, ERPNextSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {settings.ApiKey}:{settings.ApiSecret}");
    }

    #region User CRUD

    public async Task<UserResponseDto> CreateUserAsync(UserCreateRequestDto user)
    {
        var url = $"{_settings.Url}/api/resource/User";
        var content = SerializeJson(user);

        using var response = await _httpClient.PostAsync(url, content);
        return await HandleResponse<UserResponseDto>(response);
    }

    public async Task<UserResponseDto> GetUserAsync(string email)
    {
        var url = $"{_settings.Url}/api/resource/User/{Uri.EscapeDataString(email)}";
        using var response = await _httpClient.GetAsync(url);
        return await HandleResponse<UserResponseDto>(response);
    }

    public async Task<UserResponseDto> UpdateUserAsync(string email, object updateData)
    {
        var url = $"{_settings.Url}/api/resource/User/{Uri.EscapeDataString(email)}";
        var content = SerializeJson(updateData);

        using var response = await _httpClient.PutAsync(url, content);
        return await HandleResponse<UserResponseDto>(response);
    }

    public async Task<bool> DeleteUserAsync(string email)
    {
        var url = $"{_settings.Url}/api/resource/User/{Uri.EscapeDataString(email)}";
        using var response = await _httpClient.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Role CRUD

    public async Task<RolesResponseDto> GetAllRolesAsync()
    {
        var url = $"{_settings.Url}/api/resource/Role";
        using var response = await _httpClient.GetAsync(url);
        return await HandleResponse<RolesResponseDto>(response);
    }

    public async Task<RoleDto> GetRoleAsync(string roleName)
    {
        var url = $"{_settings.Url}/api/resource/Role/{Uri.EscapeDataString(roleName)}";
        using var response = await _httpClient.GetAsync(url);
        return await HandleResponse<RoleDto>(response);
    }

    #endregion

    #region Helpers

    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static ByteArrayContent SerializeJson(object data)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data, JsonSerializerOptions);
        return new ByteArrayContent(json);
    }

    private static async Task<T> HandleResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsByteArrayAsync();

        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<T>(content)!;
        }

        throw new Exception($"API call failed: {response.StatusCode}\n{content}");
    }

    #endregion
}

