namespace LargeExcelProcessor.Infrastructure;

public static class Constants
{
    public const string StatusPending = "Pending";
    public const string StatusProcessing = "Processing";
    public const string StatusCompleted = "Completed";
    public const string StatusFailed = "Failed";

    public const string RequestTypeUpload = "Upload";
    public const string RequestTypeExport = "Export";

    public const string SignalRGroupRequests = "requests";

    public const string SignalRMethodNewRequest = "NewRequest";
    public const string SignalRMethodRequestStatusChanged = "RequestStatusChanged";
    public const string SignalRMethodRequestDeleted = "RequestDeleted";
    public const string SignalRMethodProcessingCompleted = "ProcessingCompleted";

    public const string BlobContainerName = "file-requests";
    public const string BlobPrefixUploads = "uploads";
    public const string BlobPrefixExports = "exports";
    public const string ResultBlobFileName = "result.xlsx";

    public const string QueueExportRequests = "export-requests";
    public const string QueueConnectionStringName = "AzureWebJobsStorage";

    public const string ConfigConnectionStringDefault = "Default";
    public const string ConfigConnectionStringAzureWebJobs = "AzureWebJobsStorage";
    public const string EnvVarNotifyApiBaseUrl = "NotifyApiBaseUrl";
    public const string EnvVarConnectionStringsDefault = "ConnectionStrings__Default";

    public const string EpplusLicenseAppName = "Invoicing App";

    public const string DefaultConnectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost:10000";
    public const string DefaultNotifyApiBaseUrl = "http://localhost:5000";
}
