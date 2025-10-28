using System.Diagnostics;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace DartWing.Microsoft;

public sealed class AzureStorageAdapter(BlobServiceClient blobServiceClient, IHttpClientFactory clientFactory, ILogger<AzureStorageAdapter> logger)
{
    public Uri GetPublicUrl(string containerName, string blobName, TimeSpan? duration = null)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);

        return blobClient.GenerateSasUri(BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(duration ?? TimeSpan.FromDays(1)));
    }

    public async Task<UploadResult> Upload(string fileUrl, string containerName, bool generatePublicUri, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        using var message = await clientFactory.CreateClient().GetAsync(fileUrl, ct);
        var contentType = message.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var originalFileName = message.Content.Headers.ContentDisposition?.FileName;
        if (!string.IsNullOrEmpty(originalFileName)) originalFileName = Uri.UnescapeDataString(originalFileName);
        var fileName = originalFileName;
        if (string.IsNullOrEmpty(fileName))
            fileName = $"{Path.GetFileNameWithoutExtension(fileUrl)}_{Random.Shared.Next(9999)}{Path.GetExtension(fileUrl)}";
        
        await using var fileStream = await message.Content.ReadAsStreamAsync(ct);
        var result= await Upload(fileStream, containerName, fileName, fileName, contentType, generatePublicUri, ct);
        logger.LogInformation("AzureStorage Uploaded {l} {file}/{ct} to {cont} {sw}", fileUrl, fileName, contentType,
            containerName, Stopwatch.GetElapsedTime(sw));
        return result;
    }

    public async Task<UploadResult> Upload(Stream stream, string containerName, string blobName, string fileName,
        string contentType, bool generatePublicUri, CancellationToken ct)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(stream, new BlobHttpHeaders
        {
            ContentType = contentType,
            ContentDisposition = $"attachment; filename={Uri.EscapeDataString(fileName)}"
        }, cancellationToken: ct);

        var publicUri = generatePublicUri
            ? blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMonths(1))
            : null;

        return new UploadResult(true, fileName, blobName, publicUri);
    }

    public async Task<Stream> GetStream(string containerName, string blobName, CancellationToken ct)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);

        return await blobClient.OpenReadAsync(cancellationToken: ct);
    }
    
    public async Task<string?> CreateContainer(string containerName, CancellationToken ct)
    {
        var containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName, cancellationToken: ct);
        return containerClient.HasValue ? containerClient.Value.Name :  null;
    }
}

public record UploadResult(
    bool Success,
    string Filename,
    string BlobName,
    Uri? PublicUri = null,
    Exception? Exception = null);