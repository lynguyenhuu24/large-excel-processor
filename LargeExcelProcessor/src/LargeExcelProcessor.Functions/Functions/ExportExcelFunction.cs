using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace LargeExcelProcessor.Functions.Functions;

public class ExportExcelFunction
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExportExcelFunction> _logger;

    public ExportExcelFunction(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ExportExcelFunction> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Function("ExportExcel")]
    public async Task Run(
        [QueueTrigger("export-requests", Connection = "AzureWebJobsStorage")] string message)
    {
        _logger.LogInformation("Export queue trigger fired");

        ExportQueueMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<ExportQueueMessage>(message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize queue message");
            return;
        }

        if (msg is null || msg.JobId == Guid.Empty)
        {
            _logger.LogWarning("Invalid queue message: missing job ID");
            return;
        }

        var jobId = msg.JobId;

        var fileRequest = await _db.FileRequests.FindAsync(jobId);
        if (fileRequest is null)
        {
            _logger.LogWarning("FileRequest not found for job: {JobId}", jobId);
            return;
        }

        fileRequest.Status = "Processing";
        await _db.SaveChangesAsync();

        var query = ApplyFilters(_db.InvoiceRecords.AsQueryable(),
                msg.Search, msg.Status, msg.DateFrom, msg.DateTo)
            .OrderByDescending(r => r.CreatedAt);

        var totalRecords = await query.CountAsync();

        fileRequest.TotalRows = totalRecords;
        await _db.SaveChangesAsync();

        await NotifyAsync(new NotificationDto
        {
            JobId = jobId,
            Status = "Processing",
            TotalRows = totalRecords
        });

        try
        {
            var records = await query.ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Invoices");

            var headers = new[] {
                "InvoiceNumber", "InvoiceDate", "VendorName", "VendorTaxId", "CustomerName",
                "CustomerEmail", "LineItemCount", "Subtotal", "TaxAmount", "DiscountAmount",
                "TotalAmount", "CurrencyCode", "DueDate", "Status", "Notes"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cells[1, i + 1].Value = headers[i];

            var lastNotify = DateTime.MinValue;
            const int progressBatchSize = 1000;

            for (int row = 0; row < records.Count; row++)
            {
                var r = records[row];
                ws.Cells[row + 2, 1].Value = r.InvoiceNumber;
                ws.Cells[row + 2, 2].Value = r.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                ws.Cells[row + 2, 3].Value = r.VendorName;
                ws.Cells[row + 2, 4].Value = r.VendorTaxId;
                ws.Cells[row + 2, 5].Value = r.CustomerName;
                ws.Cells[row + 2, 6].Value = r.CustomerEmail;
                ws.Cells[row + 2, 7].Value = r.LineItemCount;
                ws.Cells[row + 2, 8].Value = r.Subtotal;
                ws.Cells[row + 2, 9].Value = r.TaxAmount;
                ws.Cells[row + 2, 10].Value = r.DiscountAmount;
                ws.Cells[row + 2, 11].Value = r.TotalAmount;
                ws.Cells[row + 2, 12].Value = r.CurrencyCode;
                ws.Cells[row + 2, 13].Value = r.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                ws.Cells[row + 2, 14].Value = r.Status;
                ws.Cells[row + 2, 15].Value = r.Notes;

                var written = row + 1;
                if (written % progressBatchSize == 0 || written == records.Count)
                {
                    var now = DateTime.UtcNow;
                    if ((now - lastNotify).TotalSeconds >= 1)
                    {
                        lastNotify = now;
                        await NotifyAsync(new NotificationDto
                        {
                            JobId = jobId,
                            Status = "Processing",
                            TotalRows = totalRecords,
                            ImportedRows = written
                        });
                    }
                }
            }

            var dataRange = ws.Cells[1, 1, records.Count + 1, headers.Length];
            var table = ws.Tables.Add(dataRange, "Invoices");
            table.TableStyle = TableStyles.Medium9;

            for (int col = 8; col <= 11; col++)
                ws.Cells[2, col, records.Count + 1, col].Style.Numberformat.Format = "#,##0.00";

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            byte[] xlsxBytes;
            using (var ms = new MemoryStream())
            {
                package.SaveAs(ms);
                xlsxBytes = ms.ToArray();
            }

            var connString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost:10000";
            var blobOptions = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2024_08_04);
            var blobServiceClient = new BlobServiceClient(connString, blobOptions);
            var containerClient = blobServiceClient.GetBlobContainerClient("file-requests");
            var resultBlobName = $"exports/{jobId:N}/result.xlsx";
            var resultBlobClient = containerClient.GetBlobClient(resultBlobName);

            using (var uploadStream = new MemoryStream(xlsxBytes))
            {
                await resultBlobClient.UploadAsync(uploadStream, overwrite: true);
            }

            var resultBlobUri = resultBlobClient.Uri.ToString();

            fileRequest.Status = "Completed";
            fileRequest.ResultBlobUri = resultBlobUri;
            fileRequest.FileSize = xlsxBytes.Length;
            fileRequest.TotalRows = records.Count;
            fileRequest.ImportedRows = records.Count;
            fileRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await NotifyAsync(new NotificationDto
            {
                JobId = jobId,
                Status = "Completed",
                TotalRows = records.Count,
                ImportedRows = records.Count,
                FileSize = xlsxBytes.Length,
                ResultBlobUri = resultBlobUri,
                CompletedAt = fileRequest.CompletedAt
            });

            _logger.LogInformation("Export completed for job {JobId}: {Count} rows", jobId, records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process export for job: {JobId}", jobId);

            fileRequest.Status = "Failed";
            fileRequest.ErrorMessage = ex.Message;
            fileRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await NotifyAsync(new NotificationDto
            {
                JobId = jobId,
                Status = "Failed",
                ErrorMessage = ex.Message,
                CompletedAt = fileRequest.CompletedAt
            });
        }
    }

    private static IQueryable<InvoiceRecord> ApplyFilters(IQueryable<InvoiceRecord> query, string? search, string? status, DateTime? dateFrom, DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r => r.InvoiceNumber.ToLower().Contains(s)
                || r.VendorName.ToLower().Contains(s)
                || r.CustomerName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (dateFrom.HasValue)
            query = query.Where(r => r.InvoiceDate >= DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc));

        if (dateTo.HasValue)
            query = query.Where(r => r.InvoiceDate <= DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc));

        return query;
    }

    private async Task NotifyAsync(NotificationDto notification)
    {
        var baseUrl = Environment.GetEnvironmentVariable("NotifyApiBaseUrl") ?? "http://localhost:5000";
        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(notification);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync($"{baseUrl}/api/notify", content);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Notification sent for job {JobId}", notification.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification for job {JobId}", notification.JobId);
        }
    }

    private record ExportQueueMessage(
        [property: JsonPropertyName("jobId")] Guid JobId,
        [property: JsonPropertyName("search")] string? Search,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("dateFrom")] DateTime? DateFrom,
        [property: JsonPropertyName("dateTo")] DateTime? DateTo);
}
