using System.Text.Json;
using DartWing.KeyCloak;
using Microsoft.Extensions.Caching.Memory;
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
            new KeyValuePair<string, string>("client_secret", ""),
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
            var adapter = new GraphApiAdapter(token, DefaultHttpClientFactory.Instance, new MemoryCache(new MemoryCacheOptions()));
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
        var keyCloakHelper = p.GetService<KeyCloakTokenProvider>();

        var token = await keyCloakHelper.TokenExchangeProviderAccessToken("nikita.flimakov@defaultPermissionstenant.onmicrosoft.com",
            "azure_sharepoint", false, CancellationToken.None);

        Assert.IsNotNull(token);

        var adapter = new GraphApiAdapter(token.AccessToken, DefaultHttpClientFactory.Instance, new MemoryCache(new MemoryCacheOptions()));
        try
        {
            var me = await adapter.Me(CancellationToken.None);
            var my = await adapter.GetMyFolders(CancellationToken.None);
            var sites = await adapter.GetAllSites(CancellationToken.None);
            var drives = await adapter.GetAllDrives(CancellationToken.None);
            var folders = await adapter.GetAllFolders(drives[0].Id, recursive: true, ct: CancellationToken.None);
            var folders2 = await adapter.GetAllFolders("b!EqFpt3Qt40-22UOfOogtmzN05V8GV8tHvZs77CRfPJBZWOwcg3WBT6SG5eTEDc6i", recursive: true, ct: CancellationToken.None);
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [TestMethod]
    public async Task GetSiteName()
    {
        var token = "";
        var adapter = new GraphApiAdapter(token, DefaultHttpClientFactory.Instance, new MemoryCache(new MemoryCacheOptions()));
        //var sites = await adapter.GetAllSitesWithDrives(CancellationToken.None);
        
        var tenantName = await adapter.GetTenantName(CancellationToken.None);
        var s = await adapter.GetSite("FHASClients-InkRouter", tenantName, CancellationToken.None);
        
        var dr = await adapter.GetAllDrives(s.Id, CancellationToken.None);

        var folds = await adapter.GetAllFolders(dr[0].Id);

        var folds1 = await adapter.GetAllFolders(dr[0].Id, folds[0].Id);
    }
    
    [TestMethod]
    public async Task UploadFileFarHeap()
    {
        var token = "";

        var driveId = "b!Xn5lp85zJUmzeSqRE8sOt9uc9r8bA6JMnjzJHmX88PwBA05wwx_NS5_bce0Haj6j";
        var folderId = "01V7HICG2HUQTRMXDO7VFIFRGLSMKMDJAQ";
        var adapter = new GraphApiAdapter(token, DefaultHttpClientFactory.Instance, new MemoryCache(new MemoryCacheOptions()));
        var driveItem = await adapter.UploadFileByLink(driveId, folderId,
            "https://image.overnightprints.com/61/81/6d/hi_68ad1d524fdf93.37880798.pdf", CancellationToken.None);

    }
    
    [TestMethod]
    public async Task UploadFileOpensoft()
    {
        var token = "";

        var driveId = "b!DarVqd9QqEGCX4X7W-YCZXkZ3S_Mo-ZGpR3381Uo_iY7iipW0gWUTbPmZtwZ-O4X";
        var folderId = "0154CUDT22CNQAR3CLEFGJMQ7G33NNC72U";//"0154CUDT5K4VKBRP5HBNHJPKQZRWNE2PV7";
        var adapter = new GraphApiAdapter(token, DefaultHttpClientFactory.Instance, new MemoryCache(new MemoryCacheOptions()));
        var driveItem = await adapter.UploadFileByLink(driveId, folderId,
            "https://image.overnightprints.com/61/81/6d/hi_68ad1d524fdf93.37880798.pdf", CancellationToken.None);

    }

    [TestMethod]
    public async Task GetDriveFolderId()
    {
        var token = "";
        var adapter = new GraphApiAdapter(token, DefaultHttpClientFactory.Instance, new MemoryCache(new MemoryCacheOptions()));
        var site = await adapter.GetSite("FHASSAG", "farheap",CancellationToken.None);

        var drive = await adapter.GetAllDrives(site.Id, CancellationToken.None);
        var driveId = drive[0].Id;
        var folders = await adapter.GetAllFolders(driveId);
        var f = folders.FirstOrDefault(f => f.Name.Contains(" payable"));
        var folders1 = await adapter.GetAllFolders(driveId, f.Id);
        var f1 =  folders1.FirstOrDefault(f => f.Name.Contains("SAG Vendors"));
        var folders2 = await adapter.GetAllFolders(driveId, f1.Id);
        var f2 =  folders2.FirstOrDefault(f => f.Name == "ONP");
        var folders3 = await adapter.GetAllFolders(driveId, f2.Id);
        var f4 =  folders3.FirstOrDefault(f => f.Name == "LL Documents");
        var txt = JsonSerializer.Serialize(new {DriveId = driveId, FolderId = f4.Id});
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

public sealed class DefaultHttpClientFactory : IHttpClientFactory, IDisposable
{
    public static DefaultHttpClientFactory Instance { get; } = new();
    
    private readonly Lazy<HttpMessageHandler> _handlerLazy = new (() => new HttpClientHandler());

    public HttpClient CreateClient(string name) => new (_handlerLazy.Value, disposeHandler: false);

    public void Dispose()
    {
        if (_handlerLazy.IsValueCreated)
        {
            _handlerLazy.Value.Dispose();
        }
    }
}