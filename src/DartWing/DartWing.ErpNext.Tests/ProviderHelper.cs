using DartWing.Frappe;
using DartWing.Frappe.Erp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.ErpNext.Tests;

internal static class ProviderHelper
{
    public static ServiceProvider CreateServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigurationManager configurationManager = new();
        configurationManager.AddJsonFile("appsettings.json");
        services.AddFrappe(configurationManager);
        return services.BuildServiceProvider();
    }
    
    internal static async Task<(FrappeUserData request, FrappeUserData? response)> CreateRandomUser(this IErpUserService service)
    {
        FrappeUserData u = new()
        {
            Location = "12345, 123 Main Street, US",
            Email = $"test.{Guid.NewGuid().ToString()[..6]}@test.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = Random.Shared.NextInt64().ToString(),
            MobileNo = Random.Shared.NextInt64().ToString(),
            Roles = [new ErpUserRoleDto { Role = "Guest" }],
            SendWelcomeEmail = 0,
            Language = "en"
        };

        var newUserData = await service.CreateUser(u, CancellationToken.None);
        return (u, newUserData);
    }
    
    public static FrappeCompanyDto CreateRandomCompany()
    {
        FrappeCompanyDto c = new()
        {
            Name = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('='),
            Abbr = "Test Abbr" + Random.Shared.Next(10000),
            DefaultCurrency = "USD",
            Domain = "Test Domain" + Random.Shared.Next(10000),
            Country = "United States"
        };
        return c;
    }
    
    public static async Task<(FrappeSupplierData request, FrappeSupplierData? response)> CreateRandomSupplier(this FrappeSupplierService service)
    {
        FrappeSupplierData c = new()
        {
            Name = "Test.Suppl " + Guid.NewGuid().ToString()[..5],
            SupplierDetails = "Details " + Guid.NewGuid().ToString()[..5],
            Country = "us",
            Language = "en",
            SupplierType = "Company",
            DefaultCurrency = "USD",
            CustomInvoiceEmailsWhitelist = "aa@my.com,bb@your.dot",
            TaxId = "TaxIddd 123",
            Website = "www.mywebsite66.com",
        };
        
        var erpC = await service.Create(c, CancellationToken.None);
        
        return (c, erpC);
    }
}