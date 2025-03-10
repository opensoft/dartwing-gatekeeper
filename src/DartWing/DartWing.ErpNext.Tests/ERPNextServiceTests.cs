using DartWing.ErpNext.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.ErpNext.Tests;

[TestClass]
public sealed class ERPNextServiceTests
{
    [TestMethod]
    public async Task UserTest()
    {
        var provider = CreateServiceProvider();

        var service = provider.GetService<ERPNextService>();

        UserCreateRequestDto u = new()
        {
            Address = "123 Main Street",
            Country = "US",
            Email = $"test.{Guid.NewGuid().ToString()[..6]}@test.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = Random.Shared.NextInt64().ToString(),
            MobileNo = Random.Shared.NextInt64().ToString(),
            ZipCode = "12345",
            Roles = [new UserRoleDto { Role = "Guest" }],
            SendWelcomeEmail = 0
        };

        var newUserData = await service.CreateUserAsync(u, CancellationToken.None);
        var newUser = newUserData.Data;
        Assert.IsNotNull(newUser);

        Assert.AreEqual(u.Email, newUser.Email);
        Assert.AreEqual(u.FirstName, newUser.FirstName);

        
        var getUser = await service.GetUserAsync(u.Email, CancellationToken.None);
        Assert.IsNotNull(getUser);
        
    }
    
    private static ServiceProvider CreateServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigurationManager configurationManager = new();
        configurationManager.AddJsonFile("appsettings.json");
        services.AddDartWing(configurationManager);
        return services.BuildServiceProvider();
    }
}