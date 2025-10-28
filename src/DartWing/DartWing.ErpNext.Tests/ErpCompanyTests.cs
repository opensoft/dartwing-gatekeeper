using DartWing.Frappe;
using DartWing.Frappe.Erp;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DartWing.ErpNext.Tests;

[TestClass]
public class ErpCompanyTests
{
    [TestMethod]
    public async Task CompanyTest()
    {
        var provider = ProviderHelper.CreateServiceProvider();

        var companyService = provider.GetService<IErpCompanyService>()!;
        var service = provider.GetService<IFrappeService>()!;
        var ttt = service.GetService<FrappeCompanyService>("https://qa.frappe.opensoft.one.com", $"token 7f41d1a38f3841d:c6e7938fa2a97cd");
        
        var countryCodes = await provider.GetService<FrappeCountryService>()!.GetCompanyCodes(CancellationToken.None);
        IsNotNull(countryCodes);
        IsTrue(countryCodes.Count > 20);

        var c = ProviderHelper.CreateRandomCompany();
        var erpC = await companyService.Create(c, CancellationToken.None);
        
        IsNotNull(erpC);
        AreEqual(c.Abbr, erpC.Abbr);
        AreEqual(c.DefaultCurrency, erpC.DefaultCurrency);
        AreEqual(c.Domain, erpC.Domain);
        AreEqual(c.CompanyName, erpC.Name);

        FrappeCompanyDto uc = new()
        {
            DefaultCurrency = "USD",
            Domain = "Test Domain" + Random.Shared.Next(10000),
            CustomMicrosoftSharepointFolderPath = "root:",
            CustomMicrosoftTenantId = Guid.NewGuid().ToString(),
            Country = countryCodes["us"]
        };
        
        var erpuC = await companyService.Update(c.CompanyName, uc, CancellationToken.None);
        
        IsNotNull(erpuC);
        AreEqual(uc.DefaultCurrency, erpuC.DefaultCurrency);
        AreEqual(uc.Domain, erpuC.Domain);

        c.Name += 1;
        var erpC1 = await companyService.Create(c, CancellationToken.None);
        
        var success = await companyService.Delete(erpuC.Name, CancellationToken.None);
        IsTrue(success);
        
        var success1 = await companyService.Delete(erpC1.Name, CancellationToken.None);

    }


    [TestMethod]
    public async Task UsersInCompanyTest()
    {
        var provider = ProviderHelper.CreateServiceProvider();

        var companyService = provider.GetService<IErpCompanyService>()!;
        var userService = provider.GetService<IErpUserService>()!;
        //var permService = provider.GetService<ErpUserCompanyPermissionService>()!;

        var usr = await userService.CreateRandomUser();
        IsNotNull(usr.response);
        var c = ProviderHelper.CreateRandomCompany();
        var erpC = await companyService.Create(c, CancellationToken.None);
        IsNotNull(erpC);

        try
        {
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
            // ignored
        }
        finally
        {
            var delComp = await companyService.Delete(erpC.Name, CancellationToken.None);
            var delUsr =await userService.Delete(usr.response.Name, CancellationToken.None);
            IsTrue(delComp);
            IsTrue(delUsr);
        }
    }
}