using ClosedXML.Excel;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LargeExcelProcessor.Api.Services;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly AppDbContext _db;
    private const int BatchSize = 1000;

    public ExcelProcessingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UploadResultDto> ProcessExcelAsync(Stream excelStream, CancellationToken cancellationToken)
    {
        var result = new UploadResultDto();
        var records = new List<InvoiceRecord>();
        var batchId = Guid.NewGuid().ToString("N");

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        if (range is null)
            return result;

        var rows = range.RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var record = new InvoiceRecord
                {
                    InvoiceNumber = row.Cell(1).GetString().Trim(),
                    InvoiceDate = ParseDate(row.Cell(2)),
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
                    DueDate = ParseDate(row.Cell(13)),
                    Status = row.Cell(14).GetString().Trim(),
                    Notes = row.Cell(15).GetString().Trim(),
                    BatchId = batchId
                };

                records.Add(record);
                result.TotalRows++;

                if (records.Count >= BatchSize)
                {
                    await FlushBatchAsync(records, cancellationToken);
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
            await FlushBatchAsync(records, cancellationToken);
            result.ImportedRows += records.Count;
        }

        return result;
    }

    public async Task<PagedResult<InvoiceRecordDto>> GetRecordsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.InvoiceRecords.OrderByDescending(r => r.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new InvoiceRecordDto
            {
                Id = r.Id,
                InvoiceNumber = r.InvoiceNumber,
                InvoiceDate = r.InvoiceDate,
                VendorName = r.VendorName,
                VendorTaxId = r.VendorTaxId,
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                LineItemCount = r.LineItemCount,
                Subtotal = r.Subtotal,
                TaxAmount = r.TaxAmount,
                DiscountAmount = r.DiscountAmount,
                TotalAmount = r.TotalAmount,
                CurrencyCode = r.CurrencyCode,
                DueDate = r.DueDate,
                Status = r.Status,
                Notes = r.Notes,
                BatchId = r.BatchId,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceRecordDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task FlushBatchAsync(List<InvoiceRecord> records, CancellationToken cancellationToken)
    {
        _db.InvoiceRecords.AddRange(records);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static decimal ParseDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<decimal>(out var value))
            return value;

        if (decimal.TryParse(cell.GetString(), out var parsed))
            return parsed;

        return 0;
    }

    private static int ParseInt(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var value))
            return value;

        if (int.TryParse(cell.GetString(), out var parsed))
            return parsed;

        return 0;
    }

    private static DateTime ParseDate(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var date))
            return date;

        if (DateTime.TryParse(cell.GetString(), out var parsed))
            return parsed;

        return DateTime.MinValue;
    }
}
