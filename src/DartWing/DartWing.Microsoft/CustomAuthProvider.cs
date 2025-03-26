using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace DartWing.Microsoft;

public sealed class CustomAuthProvider : IAuthenticationProvider
{
    private readonly string _accessToken;

    public CustomAuthProvider(string accessToken)
    {
        _accessToken = "Bearer " + accessToken;
    }

    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken ct = default)
    {
        request.Headers.Add("Authorization", _accessToken);
        return Task.CompletedTask;
    }
}