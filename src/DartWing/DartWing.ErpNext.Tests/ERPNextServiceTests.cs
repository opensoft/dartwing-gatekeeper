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
        var (u, newUser) = await CreateUser(service);
        IsNotNull(newUser);

        AreEqual(u.Email, newUser.Email);
        AreEqual(u.FirstName, newUser.FirstName);

        
        var getUser = await service.GetUserAsync(u.Email, CancellationToken.None);
        IsNotNull(getUser);
    }

    private static async Task<(UserCreateRequestDto request, UserResponseDto response)> CreateUser(ERPNextService service)
    {
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
        return (u, newUserData!.Data);
    }

    private static async Task<(CreateCompanyDto request, CompanyDto? response)> CreateCompany(ERPNextService service)
    {
        CreateCompanyDto c = new()
        {
            CompanyName = "Test Company" + Random.Shared.Next(20000),
            Abbr = "Test Abbr" + Random.Shared.Next(10000),
            DefaultCurrency = "USD",
            Domain = "Test Domain" + Random.Shared.Next(10000),
            Country = "United States"
        };
        
        var erpC = await service.CreateCompanyAsync(c, CancellationToken.None);
        
        return (c, erpC?.Data);
    }

    [TestMethod]
    public async Task CompanyTest()
    {
        var provider = CreateServiceProvider();

        var service = provider.GetService<ERPNextService>();

        var (c, erpC) = await CreateCompany(service);
        
        IsNotNull(erpC);
        AreEqual(c.Abbr, erpC.Abbr);
        AreEqual(c.DefaultCurrency, erpC.DefaultCurrency);
        AreEqual(c.Domain, erpC.Domain);
        AreEqual(c.CompanyName, erpC.Name);
        
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


    [TestMethod]
    public async Task UsersInCompanyTest()
    {
        var provider = CreateServiceProvider();

        var service = provider.GetService<ERPNextService>()!;

        var usr = await CreateUser(service);
        var company = await CreateCompany(service);

        try
        {
            var res = await service.AddUserInCompanyAsync(usr.response.Email, company.response.Name, CancellationToken.None);

            var resComps = await service.GetUserCompaniesAsync(usr.response.Email, CancellationToken.None);

            var removed = await service.RemoveUserFromCompanyAsync(usr.response.Email, company.response.Name, CancellationToken.None);
        }
        catch (Exception e)
        {
        }
        
        var delComp = await service.DeleteCompanyAsync(company.response.Name, CancellationToken.None);
        var delUsr =await service.DeleteUserAsync(usr.response.Name, CancellationToken.None);
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