namespace LargeExcelProcessor.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string prefix, Guid jobId, string fileName, Stream content, CancellationToken cancellationToken);
    Task<Stream?> DownloadAsync(string blobUri, CancellationToken cancellationToken);
    Task DeleteAsync(string blobUri, CancellationToken cancellationToken);
}
