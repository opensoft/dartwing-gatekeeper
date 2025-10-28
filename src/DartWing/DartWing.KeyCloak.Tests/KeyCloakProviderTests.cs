using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.KeyCloak.Tests;

[TestClass]
public sealed class KeyCloakProviderTests
{
    [TestMethod]
    public async Task GetOrganizationsAndUsers()
    {
        var p = CreateServiceProvider();
        var keyCloakHelper = p.GetService<KeyCloakProvider>()!;
        var orgs = await keyCloakHelper.GetAllOrganizations(CancellationToken.None);
        Assert.IsNotNull(orgs);
        Assert.IsTrue(orgs.Count > 0);
        foreach (var org in orgs)
        {
           var users = await keyCloakHelper.GetOrganizationUsers(org, CancellationToken.None);
           Assert.IsNotNull(users);
           if (users.Count > 0)
           {
               var user = await keyCloakHelper.GetUserById(users[0].Id, CancellationToken.None);
               Assert.IsNotNull(user);
               return;
           }
        }
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