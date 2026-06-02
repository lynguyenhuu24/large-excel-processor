using ClosedXML.Excel;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
            var result = await ProcessExcelStream(blobStream, jobId.Value.ToString());

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
                ImportedRows = result.ImportedRows
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
                ErrorMessage = ex.Message
            });
        }
    }

    private async Task<UploadResultDto> ProcessExcelStream(Stream stream, string batchId)
    {
        var result = new UploadResultDto();
        var records = new List<InvoiceRecord>();
        const int batchSize = 1000;

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        if (range is null)
            return result;

        var rows = range.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            try
            {
                records.Add(new InvoiceRecord
                {
                    InvoiceNumber = row.Cell(1).GetString().Trim(),
                    InvoiceDate = DateTime.SpecifyKind(ParseDate(row.Cell(2)), DateTimeKind.Utc),
                    VendorName = row.Cell(3).GetString().Trim(),
                    VendorTaxId = row.Cell(4).GetString().Trim(),
                    CustomerName = row.Cell(5).GetString().Trim(),
                    CustomerEmail = row.Cell(6).GetString().Trim(),
                    LineItemCount = ParseInt(row.Cell(7)),
                    Subtotal = ParseDecimal(row.Cell(8)),
                    TaxAmount = ParseDecimal(row.Cell(9)),
                    DiscountAmount = ParseDecimal(row.Cell(10)),
                    TotalAmount = ParseDecimal(row.Cell(11)),
                    CurrencyCode = row.Cell(12).GetString().Trim(),
                    DueDate = DateTime.SpecifyKind(ParseDate(row.Cell(13)), DateTimeKind.Utc),
                    Status = row.Cell(14).GetString().Trim(),
                    Notes = row.Cell(15).GetString().Trim(),
                    BatchId = batchId
                });
                result.TotalRows++;

                if (records.Count >= batchSize)
                {
                    _db.InvoiceRecords.AddRange(records);
                    await _db.SaveChangesAsync();
                    result.ImportedRows += records.Count;
                    records.Clear();
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

    private static decimal ParseDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var value)) return value;
        if (decimal.TryParse(cell.GetString(), out var parsed)) return parsed;
        return 0;
    }

    private static int ParseInt(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var value)) return value;
        if (int.TryParse(cell.GetString(), out var parsed)) return parsed;
        return 0;
    }

    private static DateTime ParseDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var date)) return date;
        if (DateTime.TryParse(cell.GetString(), out var parsed)) return parsed;
        return DateTime.MinValue;
    }
}
