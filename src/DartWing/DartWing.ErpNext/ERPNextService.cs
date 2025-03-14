using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.ErpNext.Dto;

namespace DartWing.ErpNext;

public sealed class ERPNextService
{
    private readonly HttpClient _httpClient;

    public ERPNextService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region User CRUD

    public async Task<UserCreateResponseDto?> CreateUserAsync(UserCreateRequestDto user, CancellationToken ct)
    {
        const string url = "/api/resource/User";
        var content = SerializeJson(user);

        using var response = await _httpClient.PostAsync(url, content, ct);
        return await HandleResponse<UserCreateResponseDto>(response);
    }

    public async Task<UserCreateResponseDto?> GetUserAsync(string email, CancellationToken ct)
    {
        var url = $"/api/resource/User/{Uri.EscapeDataString(email)}";
        using var response = await _httpClient.GetAsync(url, ct);
        return await HandleResponse<UserCreateResponseDto>(response);
    }

    public async Task<UserResponseDto> UpdateUserAsync(string email, object updateData)
    {
        var url = $"/api/resource/User/{Uri.EscapeDataString(email)}";
        var content = SerializeJson(updateData);

        using var response = await _httpClient.PutAsync(url, content);
        return (await HandleResponse<UserResponseDto>(response))!;
    }

    public async Task<bool> DeleteUserAsync(string email)
    {
        var url = $"/api/resource/User/{Uri.EscapeDataString(email)}";
        using var response = await _httpClient.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Role CRUD

    public async Task<RolesResponseDto> GetAllRolesAsync()
    {
        const string url = "/api/resource/Role";
        using var response = await _httpClient.GetAsync(url);
        return await HandleResponse<RolesResponseDto>(response);
    }

    public async Task<RoleDto> GetRoleAsync(string roleName)
    {
        var url = $"/api/resource/Role/{Uri.EscapeDataString(roleName)}";
        using var response = await _httpClient.GetAsync(url);
        return await HandleResponse<RoleDto>(response);
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

    private static async Task<T?> HandleResponse<T>(HttpResponseMessage response) where T : class
    {
        var content = await response.Content.ReadAsStringAsync();//.ReadAsByteArrayAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<T>(content, JsonSerializerOptions)!;
            return result;
        }

        return null;
    }

    #endregion
}

