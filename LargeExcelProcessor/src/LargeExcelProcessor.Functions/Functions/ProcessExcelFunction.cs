using System.Globalization;
using System.Text.Json;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LargeExcelProcessor.Functions.Functions;

public class ProcessExcelFunction
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProcessExcelFunction> _logger;

    public ProcessExcelFunction(
        AppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ProcessExcelFunction> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [Function("ProcessExcel")]
    public async Task Run(
        [BlobTrigger("file-requests/uploads/{name}", Connection = "AzureWebJobsStorage")] byte[] blobBytes,
        string name)
    {
        _logger.LogInformation("Processing blob: {Name}", name);

        var jobId = ParseJobId(name);
        if (jobId is null)
        {
            _logger.LogWarning("Could not parse job ID from blob name: {Name}", name);
            return;
        }

        var fileRequest = await _db.FileRequests.FindAsync(jobId.Value);
        if (fileRequest is null)
        {
            _logger.LogWarning("FileRequest not found for job: {JobId}", jobId);
            return;
        }

        fileRequest.Status = "Processing";
        await _db.SaveChangesAsync();

        try
        {
            var result = await ProcessExcelStream(new MemoryStream(blobBytes), jobId.Value.ToString(), jobId.Value);

            fileRequest.Status = "Completed";
            fileRequest.TotalRows = result.TotalRows;
            fileRequest.ImportedRows = result.ImportedRows;
            fileRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await NotifyAsync(new NotificationDto
            {
                JobId = jobId.Value,
                Status = "Completed",
                TotalRows = result.TotalRows,
                ImportedRows = result.ImportedRows,
                CompletedAt = fileRequest.CompletedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process blob: {Name}", name);

            fileRequest.Status = "Failed";
            fileRequest.ErrorMessage = ex.Message;
            fileRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await NotifyAsync(new NotificationDto
            {
                JobId = jobId.Value,
                Status = "Failed",
                ErrorMessage = ex.Message,
                CompletedAt = fileRequest.CompletedAt
            });
        }
    }

    private async Task<UploadResultDto> ProcessExcelStream(Stream stream, string batchId, Guid jobId)
    {
        var result = new UploadResultDto();
        var records = new List<InvoiceRecord>();
        const int batchSize = 1000;
        var lastNotify = DateTime.MinValue;

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var totalRows = Math.Max(0, rowCount - 1);

        if (totalRows == 0)
            return result;

        var fileRequest = await _db.FileRequests.FindAsync(jobId);
        if (fileRequest != null)
        {
            fileRequest.TotalRows = totalRows;
            await _db.SaveChangesAsync();
        }

        await NotifyAsync(new NotificationDto
        {
            JobId = jobId,
            Status = "Processing",
            TotalRows = totalRows
        });

        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                if (worksheet.Cells[row, 1].Value == null)
                    continue;

                records.Add(new InvoiceRecord
                {
                    InvoiceNumber = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "",
                    InvoiceDate = DateTime.SpecifyKind(ParseDate(worksheet.Cells[row, 2]), DateTimeKind.Utc),
                    VendorName = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "",
                    VendorTaxId = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "",
                    CustomerName = worksheet.Cells[row, 5].Value?.ToString()?.Trim() ?? "",
                    CustomerEmail = worksheet.Cells[row, 6].Value?.ToString()?.Trim() ?? "",
                    LineItemCount = ParseInt(worksheet.Cells[row, 7]),
                    Subtotal = ParseDecimal(worksheet.Cells[row, 8]),
                    TaxAmount = ParseDecimal(worksheet.Cells[row, 9]),
                    DiscountAmount = ParseDecimal(worksheet.Cells[row, 10]),
                    TotalAmount = ParseDecimal(worksheet.Cells[row, 11]),
                    CurrencyCode = worksheet.Cells[row, 12].Value?.ToString()?.Trim() ?? "",
                    DueDate = DateTime.SpecifyKind(ParseDate(worksheet.Cells[row, 13]), DateTimeKind.Utc),
                    Status = worksheet.Cells[row, 14].Value?.ToString()?.Trim() ?? "",
                    Notes = worksheet.Cells[row, 15].Value?.ToString()?.Trim() ?? "",
                    BatchId = batchId
                });
                result.TotalRows++;

                if (records.Count >= batchSize)
                {
                    _db.InvoiceRecords.AddRange(records);
                    await _db.SaveChangesAsync();
                    result.ImportedRows += records.Count;
                    records.Clear();

                    var now = DateTime.UtcNow;
                    if ((now - lastNotify).TotalSeconds >= 1)
                    {
                        lastNotify = now;
                        await NotifyAsync(new NotificationDto
                        {
                            JobId = jobId,
                            Status = "Processing",
                            TotalRows = totalRows,
                            ImportedRows = result.ImportedRows
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Row {result.TotalRows + 1}: {ex.Message}");
            }
        }

        if (records.Count > 0)
        {
            _db.InvoiceRecords.AddRange(records);
            await _db.SaveChangesAsync();
            result.ImportedRows += records.Count;
        }

        return result;
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

    private static Guid? ParseJobId(string blobName)
    {
        var parts = blobName.Split('/');
        if (parts.Length >= 2 && Guid.TryParse(parts[0], out var jobId))
            return jobId;

        return null;
    }

    private static decimal ParseDecimal(ExcelRange cell)
    {
        if (cell.Value is decimal d) return d;
        if (cell.Value is double db) return (decimal)db;
        if (decimal.TryParse(cell.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return 0;
    }

    private static int ParseInt(ExcelRange cell)
    {
        if (cell.Value is int i) return i;
        if (cell.Value is double db) return (int)db;
        if (int.TryParse(cell.Text, out var parsed)) return parsed;
        return 0;
    }

    private static DateTime ParseDate(ExcelRange cell)
    {
        if (cell.Value is DateTime dt) return dt;
        if (DateTime.TryParse(cell.Text, out var parsed)) return parsed;
        return DateTime.MinValue;
    }
}
