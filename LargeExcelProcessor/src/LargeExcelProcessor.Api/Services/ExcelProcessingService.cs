using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace LargeExcelProcessor.Api.Services;

public class ExcelProcessingService : IExcelProcessingService
{
    private readonly AppDbContext _db;

    public ExcelProcessingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<InvoiceRecordDto>> GetRecordsAsync(int page, int pageSize, string? search, string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_db.InvoiceRecords.AsQueryable(), search, status, dateFrom, dateTo)
            .OrderByDescending(r => r.CreatedAt);

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

}
