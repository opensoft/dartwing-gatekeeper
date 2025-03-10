using DartWing.Web.KeyCloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.KeyCloak.Tests;

[TestClass]
public sealed class KeyCloakHelperTests
{
    [TestMethod]
    public async Task GetUserById()
    {
        const string userId = "e4f3b747-0262-4cdb-bb09-e2221256adbf";
        
        var p = CreateServiceProvider();
        var keyCloakHelper = p.GetService<KeyCloakHelper>();
        var user = await keyCloakHelper.GetUserById(userId, CancellationToken.None);
        
        Assert.IsNotNull(user);

        var id = Guid.NewGuid().ToString();
        var t = await keyCloakHelper.UpdateUserCrmId(userId, id, CancellationToken.None);
        user = await keyCloakHelper.GetUserById(userId, CancellationToken.None);

        Assert.AreEqual(user.CrmId, id);
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