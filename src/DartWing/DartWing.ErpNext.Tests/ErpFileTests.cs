using DartWing.Frappe.Erp;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.ErpNext.Tests;

[TestClass]
public class ErpFileTests
{
    [TestMethod]
    public async Task FileTest()
    {
        var provider = ProviderHelper.CreateServiceProvider();

        var service = provider.GetService<IErpFileService>()!;
        var supService = provider.GetService<FrappeSupplierService>()!;

        var supplier = await supService.CreateRandomSupplier();

        FrappeFileDataDto file = new()
        {
            FileName = "Test.file " + Guid.NewGuid().ToString()[..5] + ".pdf",
            FileUrl = "https://farheap.sharepoint.com/sites/FHASClients-SGX/Shared%20Documents/SGX/Document%20Library/Kelly%20Spicers%202024.09.03%20SGX%20bill%2050201223_9161.pdf",
            FileSize = 3456,
            FileType = "application/pdf",
            IsFolder = 0,
            Folder = "",
            AttachedToDoctype = "Supplier",
            AttachedToName = supplier.response.Name
        };

        var res = await service.Create(file, CancellationToken.None);
        Assert.IsNotNull(res);
        Assert.AreEqual(file.FileName, res.FileName);
        //Assert.AreEqual(file.FileUrl, res.FileUrl);

        var all = await service.GetList();
        Assert.IsNotNull(all);
        Assert.AreNotEqual(0, all.Length);
        
        var deleted = await service.Delete(res.Name, CancellationToken.None);
        Assert.IsTrue(deleted);

        deleted = await supService.Delete(supplier.response.Name, CancellationToken.None);
        Assert.IsTrue(deleted);
    }
}