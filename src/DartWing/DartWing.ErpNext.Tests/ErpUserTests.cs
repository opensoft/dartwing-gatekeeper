using DartWing.Frappe.Erp;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DartWing.ErpNext.Tests;

[TestClass]
public sealed class ErpUserTests
{
    [TestMethod]
    public async Task UserTest()
    {
        var provider = ProviderHelper.CreateServiceProvider();

        var service = provider.GetService<IErpUserService>()!;
        var (u, newUser) = await service.CreateRandomUser();
        IsNotNull(newUser);
        AreEqual(u.Email, newUser.Email);
        AreEqual(u.FirstName, newUser.FirstName);
        AreEqual(u.Language, newUser.Language);
        AreEqual(u.LastName, newUser.LastName);
        AreEqual(u.Phone, newUser.Phone);
        AreEqual(u.MobileNo, newUser.MobileNo);
        
        var getUser = await service.Get(u.Email, CancellationToken.None);
        IsNotNull(getUser);
        AreEqual(u.Email, getUser.Email);
        AreEqual(u.FirstName, getUser.FirstName);
        AreEqual(u.Language, getUser.Language);
        AreEqual(u.LastName, getUser.LastName);
        AreEqual(u.Phone, getUser.Phone);
        AreEqual(u.MobileNo, getUser.MobileNo);
        
        var getUsers = await service.GetList(ct: CancellationToken.None);
        IsNotNull(getUsers);
        IsTrue(getUsers.Length > 0);
        IsTrue(getUsers.Any(x => x.Name == u.Email));
        
        var deleted = await service.Delete(u.Email, CancellationToken.None);
        IsTrue(deleted);
    }
}