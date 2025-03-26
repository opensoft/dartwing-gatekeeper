using System.Text.Json;
using DartWing.KeyCloak;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartWing.Microsoft.Tests;

[TestClass]
public sealed class GraphApiTests
{
    [TestMethod]
    public async Task ClienAccessToSharePointSites()
    {
        var body = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", "d177cf09-932e-4590-a6b3-47f6ac1a9691"),
            new KeyValuePair<string, string>("client_secret", "**REMOVED_CLIENT_SECRET_4**"),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
        ]);
        
        var url = "https://login.microsoftonline.com/b5f8d51e-1fcb-444f-a548-8d840db1498d/oauth2/v2.0/token";
        using var response = await new HttpClient().PostAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            return;
        }
        
        var tokenString = await response.Content.ReadAsStringAsync();
        Assert.IsNotNull(tokenString);

        var token = JsonDocument.Parse(tokenString).RootElement.GetProperty("access_token").GetString();
        
        Assert.IsNotNull(token);

        try
        {
            var adapter = new GraphApiAdapter(token);
            var sites = await adapter.GetAllSites(CancellationToken.None);
            
            var allDrives = new List<string>();
            foreach (var s in sites)
            {
                var drive = await adapter.GetAllDrives(s.Id, CancellationToken.None);
                foreach (var d in drive)
                {
                    if (d == null) continue;
                    allDrives.Add($"{s.Name} __ {d.Name} __ {d.Id}");
                }
            }
            var drives = await adapter.GetAllDrives(CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }

    [TestMethod]
    public async Task UserAccessToSharePointSites()
    {
        var p = CreateServiceProvider();
        var keyCloakHelper = p.GetService<KeyCloakHelper>();

        var token = await keyCloakHelper.GetProviderToken("nikita.flimakov@defaultPermissionstenant.onmicrosoft.com",
            "azure_sharepoint", CancellationToken.None);

        Assert.IsNotNull(token);

        var adapter = new GraphApiAdapter(token);
        try
        {
            var me = await adapter.Me(CancellationToken.None);
            var my = await adapter.GetMyFolders(CancellationToken.None);
            var sites = await adapter.GetAllSites(CancellationToken.None);
            var drives = await adapter.GetAllDrives(CancellationToken.None);
            var folders = await adapter.GetFolders(drives[0].Id, true, CancellationToken.None);
            var folders2 = await adapter.GetFolders("b!EqFpt3Qt40-22UOfOogtmzN05V8GV8tHvZs77CRfPJBZWOwcg3WBT6SG5eTEDc6i", true, CancellationToken.None);
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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