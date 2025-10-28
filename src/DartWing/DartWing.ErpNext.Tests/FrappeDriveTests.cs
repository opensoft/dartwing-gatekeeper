using DartWing.DomainModel.Files;
using DartWing.Frappe;
using DartWing.Frappe.Drive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.ErpNext.Tests;

[TestClass]
public class FrappeDriveTests
{
    [TestMethod]
    public async Task DriveFileTest()
    {
        var provider = CreateServiceProvider();

        var service = provider.GetService<FrappeDriveService>();
        var files = await service.GetFiles("", EFileType.File | EFileType.Folder, CancellationToken.None);
        Assert.IsNotNull(files);
    }
    
    private static ServiceProvider CreateServiceProvider()
    {
        IServiceCollection services = new ServiceCollection();
        ConfigurationManager configurationManager = new();
        configurationManager.AddJsonFile("appsettings.json");
        services.AddFrappe(configurationManager);
        return services.BuildServiceProvider();
    }
}