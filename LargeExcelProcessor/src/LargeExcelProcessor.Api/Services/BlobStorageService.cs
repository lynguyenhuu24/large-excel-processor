using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace LargeExcelProcessor.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("AzureWebJobsStorage")
            ?? "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost:10000";
        var blobOptions = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2024_08_04);
        var blobServiceClient = new BlobServiceClient(connString, blobOptions);
        _containerClient = blobServiceClient.GetBlobContainerClient("file-requests");
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> UploadAsync(string prefix, Guid jobId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var blobName = $"{prefix}/{jobId:N}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        content.Position = 0;
        await blobClient.UploadAsync(content, cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream?> DownloadAsync(string blobUri, CancellationToken cancellationToken)
    {
        var blobName = new BlobUriBuilder(new Uri(blobUri)).BlobName;
        var blobClient = _containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadAsync(cancellationToken);
        return response.Value?.Content;
    }

    public async Task DeleteAsync(string blobUri, CancellationToken cancellationToken)
    {
        var blobName = new BlobUriBuilder(new Uri(blobUri)).BlobName;
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
