using System.ComponentModel.DataAnnotations;
using DartWing.Web.Invitations;
using Microsoft.Graph.Models;

namespace LedgerLinc.ApiService.Users.Entities;

internal sealed class Invitation
{
    public string OrgId { get; set; }
    public string Email { get; set; }
    public long? InvitedUserId { get; set; }
    public User? InvitedUser { get; set; }
    [MaxLength(250)]
    public string InvitedUserEmail { get; set; } = null!;
    [MaxLength(250)]
    public string? InvitedUserName { get; set; }
    [MaxLength(16)]
    public string VerificationCode { get; set; } = null!;
    public DateTime Created { get; set; }
    public DateTime ExpireDate { get; set; }
    public DateTime? Used { get; set; }
    public EInvitationType InvitationType { get; set; }
    public EInvitationResult InvitationResult { get; set; }
}