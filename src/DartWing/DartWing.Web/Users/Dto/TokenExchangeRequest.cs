namespace DartWing.Web.Users.Dto;

public sealed class TokenExchangeRequest
{
    public string ClientId {get; set;}
    public string? ClientSecret {get; set;}
    public string SubjectToken {get; set;}
    public string? Audience {get; set;}
    public string? RequestedIssuer {get; set;}
}