using DartWing.ErpNext.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
        IsNotNull(newUser);

        AreEqual(u.Email, newUser.Email);
        AreEqual(u.FirstName, newUser.FirstName);

        
        var getUser = await service.GetUserAsync(u.Email, CancellationToken.None);
        IsNotNull(getUser);
    }

    [TestMethod]
    public async Task CompanyTest()
    {
        var provider = CreateServiceProvider();

        var service = provider.GetService<ERPNextService>();

        CreateCompanyDto c = new()
        {
            CompanyName = "Test Company" + Random.Shared.Next(20000),
            Abbr = "Test Abbr" + Random.Shared.Next(10000),
            DefaultCurrency = "USD",
            Domain = "Test Domain" + Random.Shared.Next(10000),
            Country = "USA"
        };
        
        var erpC = await service.CreateCompanyAsync(c, CancellationToken.None);
        
        IsNotNull(erpC?.Data);
        AreEqual(c.Abbr, erpC.Data.Abbr);
        AreEqual(c.DefaultCurrency, erpC.Data.DefaultCurrency);
        AreEqual(c.Domain, erpC.Data.Domain);
        AreEqual(c.CompanyName, erpC.Data.Name);
        
        UpdateCompanyDto uc = new()
        {
            DefaultCurrency = "USD",
            Domain = "Test Domain" + Random.Shared.Next(10000),
            CustomMicrosoftSharepointFolderPath = "root:",
            CustomMicrosoftDelegatedUser = "service.dartwing@opensoft.one",
            CustomMicrosoftTenantId = Guid.NewGuid().ToString(),
            CustomMicrosoftTenantName = "defaultPermissions",
            Country = "USA"
        };
        
        var erpuC = await service.UpdateCompanyAsync(c.CompanyName, uc, CancellationToken.None);
        
        IsNotNull(erpuC?.Data);
        AreEqual(uc.DefaultCurrency, erpuC.Data.DefaultCurrency);
        AreEqual(uc.Domain, erpuC.Data.Domain);
        
        var success = await service.DeleteCompanyAsync(c.CompanyName, CancellationToken.None);
        IsTrue(success);
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