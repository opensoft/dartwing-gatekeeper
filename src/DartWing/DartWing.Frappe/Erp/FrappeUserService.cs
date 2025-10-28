using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DartWing.Frappe.Erp;

public interface IErpUserService : IFrappeBaseService
{
    Task<FrappeUserData[]?> GetList(int? page = null, int? pageSize = null, CancellationToken ct = default);
    ValueTask<List<FrappeUserData>?> GetAll(CancellationToken ct);
    ValueTask<FrappeUserData?> Get(string name, CancellationToken ct);
    Task<FrappeUserData?> Update(string name, FrappeUserData request, CancellationToken ct);
    Task<bool> Delete(string name, CancellationToken ct);
    Task<FrappeUserData?> CreateUser(FrappeUserData request, CancellationToken ct);
}

public sealed class FrappeUserService : FrappeBaseService<FrappeUserData>, IErpUserService
{
    public FrappeUserService(ILogger<FrappeUserService> logger, HttpClient httpClient, IMemoryCache memoryCache)
        : base(logger, httpClient, memoryCache)
    {
    }

    public async Task<FrappeUserData?> CreateUser(FrappeUserData request, CancellationToken ct)
    {
        var user = await Create(request, ct);
        if (user is null) return null;
        MemoryCache.Remove(CacheKey("all"));
        var response = await Client.Send<object, ErpUserApiKeyResponse>(HttpMethod.Post,
            "/api/method/frappe.core.doctype.user.user.generate_keys", "", [$"user={user.Email}"], null, Logger, ct);
        var u = await Get(user.Email, ct);
        if (u != null) u.ApiSecret = null;
        if (u != null && !string.IsNullOrEmpty(response?.Message?.ApiSecret)) u.ApiSecret = response.Message?.ApiSecret;
        return u;
    }
}

internal sealed class ErpUserApiKeyResponse
{
    public ErpUserMessage? Message { get; set; }
}

internal sealed class ErpUserMessage
{
    public string ApiSecret { get; set; }
}

public interface IFrappeUser
{
    string Name { get; }
    string Owner { get; }
    DateTime Creation { get; }
    DateTime Modified { get; }
    string ModifiedBy { get; }
    int Docstatus { get; }
    int Idx { get; }
    int Enabled { get; }
    string Email { get; }
    string FirstName { get; }
    string FullName { get; }
    string Username { get; }
    string Language { get; }
    string TimeZone { get; }
    string Phone { get; }
    string MobileNo { get; }
    int SendWelcomeEmail { get; }
    int Unsubscribed { get; }
    int MuteSounds { get; }
    string DeskTheme { get; }
    string NewPassword { get; }
    int LogoutAllSessions { get; }
    string ResetPasswordKey { get; }
    DateTime LastResetPasswordKeyGeneratedOn { get; }
    int DocumentFollowNotify { get; }
    string DocumentFollowFrequency { get; }
    int FollowCreatedDocuments { get; }
    int FollowCommentedDocuments { get; }
    int FollowLikedDocuments { get; }
    int FollowAssignedDocuments { get; }
    int FollowSharedDocuments { get; }
    int ThreadNotify { get; }
    int SendMeACopy { get; }
    int AllowedInMentions { get; }
    int SimultaneousSessions { get; }
    int LoginAfter { get; }
    string UserType { get; }
    int LoginBefore { get; }
    int BypassRestrictIpCheckIf2faEnabled { get; }
    string OnboardingStatus { get; }
    string Doctype { get; }
    List<object> Defaults { get; }
    List<object> UserEmails { get; }
    List<ErpSocialLogin> SocialLogins { get; }
    List<object> BlockModules { get; }
    List<ErpUserRoleDto> Roles { get; }
    string Location { get; }
    string LastName { get; }
    string MiddleName { get; }
    string Gender { get; }
    DateTime? BirthDate { get; }
    string? Interests { get; }
    string? Bio { get; }
}

public sealed class FrappeUserData : IFrappeBaseDto, IFrappeUser
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public int Enabled { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Language { get; set; }
    public string TimeZone { get; set; }
    public string Phone { get; set; }
    public string MobileNo {get; set;}
    public int SendWelcomeEmail { get; set; }
    public int Unsubscribed { get; set; }
    public int MuteSounds { get; set; }
    public string DeskTheme { get; set; }
    public string NewPassword { get; set; }
    public int LogoutAllSessions { get; set; }
    public string ResetPasswordKey { get; set; }
    public DateTime LastResetPasswordKeyGeneratedOn { get; set; }
    public int DocumentFollowNotify { get; set; }
    public string DocumentFollowFrequency { get; set; }
    public int FollowCreatedDocuments { get; set; }
    public int FollowCommentedDocuments { get; set; }
    public int FollowLikedDocuments { get; set; }
    public int FollowAssignedDocuments { get; set; }
    public int FollowSharedDocuments { get; set; }
    public int ThreadNotify { get; set; }
    public int SendMeACopy { get; set; }
    public int AllowedInMentions { get; set; }
    public int SimultaneousSessions { get; set; }
    public int LoginAfter { get; set; }
    public string UserType { get; set; }
    public int LoginBefore { get; set; }
    public int BypassRestrictIpCheckIf2faEnabled { get; set; }
    public string OnboardingStatus { get; set; }
    public string Doctype { get; set; }
    public List<object> Defaults { get; set; }
    public List<object> UserEmails { get; set; }
    public List<ErpSocialLogin> SocialLogins { get; set; }
    public List<object> BlockModules { get; set; }
    public List<ErpUserRoleDto> Roles { get; set; } = [];
    public string Location { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public string Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Interests { get; set; }
    public string? Bio { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
}

public sealed class ErpSocialLogin
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public DateTime Creation { get; set; }
    public DateTime Modified { get; set; }
    public string ModifiedBy { get; set; }
    public int Docstatus { get; set; }
    public int Idx { get; set; }
    public string Provider { get; set; }
    public string Userid { get; set; }
    public string Parent { get; set; }
    public string Parentfield { get; set; }
    public string Parenttype { get; set; }
    public string Doctype { get; set; }
}