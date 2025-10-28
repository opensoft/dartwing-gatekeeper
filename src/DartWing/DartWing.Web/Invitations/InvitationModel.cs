using System.Text.Json.Serialization;

namespace DartWing.Web.Invitations;

internal sealed class InvitationModel
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;

    [JsonPropertyName("userEmail")]
    public string UserEmail { get; set; } = null!;

    [JsonPropertyName("invitedUserEmail")]
    public string InvitedUserEmail { get; set; } = null!;

    [JsonPropertyName("verificationCode")]
    public string VerificationCode { get; set; } = null!;

    [JsonPropertyName("createdUtc")]
    public DateTime CreatedUtc { get; set; }

    [JsonPropertyName("expireDateUtc")]
    public DateTime ExpireDateUtc { get; set; }

    [JsonPropertyName("usedUtc")]
    public DateTime? UsedUtc { get; set; }

    [JsonPropertyName("invitationType")]
    public EInvitationType InvitationType { get; set; }

    [JsonPropertyName("invitationResult")]
    public EInvitationResult InvitationResult { get; set; }
}