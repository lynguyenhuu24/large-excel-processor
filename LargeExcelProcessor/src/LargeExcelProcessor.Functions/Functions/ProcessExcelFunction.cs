using LargeExcelProcessor.Infrastructure;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Globalization;
using System.Text.Json;

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
        [BlobTrigger("file-requests/uploads/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing blob: {Name}", name);

        var jobId = ParseJobId(name);
        if (jobId is null)
        {
            _logger.LogWarning("Could not parse job ID from blob name: {Name}", name);
            return;
        }

        var fileRequest = await _db.FileRequests.FindAsync(new object[] { jobId.Value }, cancellationToken);
        if (fileRequest is null)
        {
            _logger.LogWarning("FileRequest not found for job: {JobId}", jobId);
            return;
        }

        fileRequest.Status = Constants.StatusProcessing;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await ProcessExcelStream(blobStream, jobId.Value.ToString(), jobId.Value, cancellationToken);

            fileRequest.Status = Constants.StatusCompleted;
            fileRequest.TotalRows = result.TotalRows;
            fileRequest.ImportedRows = result.ImportedRows;
            fileRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await NotifyWithRetryAsync(new NotificationDto
            {
                JobId = jobId.Value,
                Status = Constants.StatusCompleted,
                TotalRows = result.TotalRows,
                ImportedRows = result.ImportedRows,
                CompletedAt = fileRequest.CompletedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process blob: {Name}", name);

            fileRequest.Status = Constants.StatusFailed;
            fileRequest.ErrorMessage = ex.Message;
            fileRequest.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await NotifyWithRetryAsync(new NotificationDto
            {
                JobId = jobId.Value,
                Status = Constants.StatusFailed,
                ErrorMessage = ex.Message,
                CompletedAt = fileRequest.CompletedAt
            }, cancellationToken);
        }
    }

    private async Task<UploadResultDto> ProcessExcelStream(Stream stream, string batchId, Guid jobId, CancellationToken cancellationToken)
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

        var fileRequest = await _db.FileRequests.FindAsync(new object[] { jobId }, cancellationToken);
        if (fileRequest != null)
        {
            fileRequest.TotalRows = totalRows;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await NotifyWithRetryAsync(new NotificationDto
        {
            JobId = jobId,
            Status = Constants.StatusProcessing,
            TotalRows = totalRows
        }, cancellationToken);

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
                    await _db.SaveChangesAsync(cancellationToken);
                    result.ImportedRows += records.Count;
                    records.Clear();

                    var now = DateTime.UtcNow;
                    if ((now - lastNotify).TotalSeconds >= 1)
                    {
                        lastNotify = now;
                        await NotifyWithRetryAsync(new NotificationDto
                        {
                            JobId = jobId,
                            Status = Constants.StatusProcessing,
                            TotalRows = totalRows,
                            ImportedRows = result.ImportedRows
                        }, cancellationToken);
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
            await _db.SaveChangesAsync(cancellationToken);
            result.ImportedRows += records.Count;
        }

        return result;
    }

    private async Task NotifyWithRetryAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await NotifyAsync(notification, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts - 1)
            {
                _logger.LogWarning(ex, "Notification attempt {Attempt} failed for job {JobId}, retrying", attempt + 1, notification.JobId);
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "All notification attempts failed for job {JobId}", notification.JobId);
            }
        }
    }

    private async Task NotifyAsync(NotificationDto notification, CancellationToken cancellationToken)
    {
        var baseUrl = Environment.GetEnvironmentVariable(Constants.EnvVarNotifyApiBaseUrl) ?? Constants.DefaultNotifyApiBaseUrl;
        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(notification);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{baseUrl}/api/notify", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Notification sent for job {JobId}", notification.JobId);
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
