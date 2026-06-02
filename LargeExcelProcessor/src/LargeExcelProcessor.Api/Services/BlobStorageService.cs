using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LargeExcelProcessor.Infrastructure;

namespace LargeExcelProcessor.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private BlobContainerClient? _containerClient;
    private readonly object _initLock = new();
    private bool _initialized;
    private readonly IConfiguration _configuration;

    public BlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private BlobContainerClient GetContainer()
    {
        if (_initialized && _containerClient != null)
            return _containerClient;

        lock (_initLock)
        {
            if (_initialized && _containerClient != null)
                return _containerClient;

            var connString = _configuration.GetConnectionString(Constants.ConfigConnectionStringAzureWebJobs)
                ?? Constants.DefaultConnectionString;
            var blobOptions = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2024_08_04);
            var blobServiceClient = new BlobServiceClient(connString, blobOptions);
            _containerClient = blobServiceClient.GetBlobContainerClient(Constants.BlobContainerName);
            _containerClient.CreateIfNotExists();
            _initialized = true;
        }

        return _containerClient!;
    }

    public async Task<string> UploadAsync(string prefix, Guid jobId, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var container = GetContainer();
        var blobName = $"{prefix}/{jobId:N}/{fileName}";
        var blobClient = container.GetBlobClient(blobName);

        content.Position = 0;
        await blobClient.UploadAsync(content, cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream?> DownloadAsync(string blobUri, CancellationToken cancellationToken)
    {
        var container = GetContainer();
        var blobName = new BlobUriBuilder(new Uri(blobUri)).BlobName;
        var blobClient = container.GetBlobClient(blobName);

        var response = await blobClient.DownloadAsync(cancellationToken);
        return response.Value?.Content;
    }

    public async Task DeleteAsync(string blobUri, CancellationToken cancellationToken)
    {
        var container = GetContainer();
        var blobName = new BlobUriBuilder(new Uri(blobUri)).BlobName;
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
