using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.KeyCloak.Tests;

[TestClass]
public class KeyCloakTokenProviderTests
{
    [TestMethod]
    public async Task GetClientToken()
    {
        var p = CreateServiceProvider();
        var provider = p.GetService<KeyCloakTokenProvider>();
        var token = await provider.GetClientAccessToken();
        
        Assert.IsNotNull(token);
        Assert.IsNotEmpty(token.AccessToken);
    }
    
    [TestMethod]
    public async Task TokenExchange()
    {
        var ut = "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJRZFFXdnhuX3dqTk9aOS1GVmFYcUtBVl93TVAzSVZmcGptNE1tN1p1eDdjIn0.eyJleHAiOjE3NDQ2MzU2MTQsImlhdCI6MTc0NDYzMzgxNCwiYXV0aF90aW1lIjoxNzQ0NjMzNzk1LCJqdGkiOiIyYjI0NDk1Yi1lMTRkLTQ1ZTktOGE0MS1iN2E2MmE3MmM2NGIiLCJpc3MiOiJodHRwczovL3FhLmtleWNsb2FrLnRlY2gtY29ycHMuY29tL3JlYWxtcy9EYXJ0V2luZyIsImF1ZCI6WyJicm9rZXIiLCJhY2NvdW50Il0sInN1YiI6IjE4OWRkNDc3LTJiYzUtNGI5OS1hODA5LTFmMmEyY2Y2ZTViYiIsInR5cCI6IkJlYXJlciIsImF6cCI6ImRhcnR3aW5nc2VydmljZSIsInNpZCI6IjEwZDk5N2ZkLTJkNjEtNDNjMS05NmZlLThjOGE5ZGFhOGY3OSIsImFjciI6IjEiLCJhbGxvd2VkLW9yaWdpbnMiOlsiaHR0cDovL2xvY2FsaG9zdCoiXSwicmVhbG1fYWNjZXNzIjp7InJvbGVzIjpbIm9mZmxpbmVfYWNjZXNzIiwidW1hX2F1dGhvcml6YXRpb24iLCJkZWZhdWx0LXJvbGVzLWRhcnR3aW5nIl19LCJyZXNvdXJjZV9hY2Nlc3MiOnsiYnJva2VyIjp7InJvbGVzIjpbInJlYWQtdG9rZW4iXX0sImFjY291bnQiOnsicm9sZXMiOlsibWFuYWdlLWFjY291bnQiLCJtYW5hZ2UtYWNjb3VudC1saW5rcyIsInZpZXctcHJvZmlsZSJdfX0sInNjb3BlIjoidG9rZW4tZXhjaGFuZ2UgcHJvZmlsZSBlbWFpbCIsImVtYWlsX3ZlcmlmaWVkIjp0cnVlLCJuYW1lIjoiTmlraXRhIEZsaW1ha292IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibmlraXRhLmZsaW1ha292QG9wZW5zb2Z0Lm9uZSIsImdpdmVuX25hbWUiOiJOaWtpdGEiLCJmYW1pbHlfbmFtZSI6IkZsaW1ha292IiwiZW1haWwiOiJuaWtpdGEuZmxpbWFrb3ZAb3BlbnNvZnQub25lIn0.jFE9l9vRBMBPSrTzW3f98PJCzy8IdenQFfzDMQ4ppnkouLtSHEko4c65jMjieN5m5ayGm90zcEAGuDLZKQT8KrO8-JhM4i6of_k9mT1sseCLKBxJq9z78Fia7n1UW6DoN4KQVAuI43f3uKlk2xyI5k7oWS_oT75Ze71ufWkEA3SLM6T6gvltsNqvuT5bl55D6_zdJIXcIxItl4EdlYqMyJ02lLk3MVBiZDxgFWKnnBW9gDacsci1D0dHN3iiPs844_Ob4TaCEWoH3N_jDyVu-9wlTcL6s1l1B0eApLZWqMy0alc7Ie8iZOBalUTTujxEn70DTLUSqOC0K1sN6vkjlA";
        var p = CreateServiceProvider();
        var provider = p.GetService<KeyCloakTokenProvider>();
        var token = await provider.TokenExchangeUserAccessToken(ut, "ledger-api");
        
        Assert.IsNotNull(token);
        Assert.IsNotEmpty(token.AccessToken);
        
        token = await provider.TokenExchangeUserAccessToken("nikita.flimakov@opensoft.one", "ledger-api");
        Assert.IsNotNull(token);
        Assert.IsNotEmpty(token.AccessToken);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigurationManager configurationManager = new();
        configurationManager.AddJsonFile("appsettings.json");
        services.AddKeyCloak(configurationManager);
        return services.BuildServiceProvider();
    }
}