using DartWing.Frappe.Erp;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.ErpNext.Tests;

[TestClass]
public class ErpSupplierTests
{
    [TestMethod]
    public async Task SupplierTest()
    {
        var provider = ProviderHelper.CreateServiceProvider();

        var service = provider.GetService<FrappeSupplierService>()!;

        var (supplier, res) = await service.CreateRandomSupplier();
        Assert.IsNotNull(res);
        Assert.AreEqual(supplier.Name, res.Name);
        Assert.AreEqual(supplier.Country, res.Country);
        Assert.AreEqual(supplier.Language, res.Language);
        Assert.AreEqual(supplier.DefaultCurrency, res.DefaultCurrency);
        Assert.AreEqual(supplier.CustomInvoiceEmailsWhitelist, res.CustomInvoiceEmailsWhitelist);
        Assert.AreEqual(supplier.TaxId, res.TaxId);
        Assert.AreEqual(supplier.Website, res.Website);
        Assert.AreEqual(supplier.SupplierType, res.SupplierType);
        
        var all = await service.GetList();
        Assert.IsNotNull(all);
        Assert.AreNotEqual(0, all.Length);
        
        var deleted = await service.Delete(res.Name, CancellationToken.None);
        Assert.IsTrue(deleted);
    }
}