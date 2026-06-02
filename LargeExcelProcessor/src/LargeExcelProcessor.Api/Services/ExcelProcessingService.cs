using LargeExcelProcessor.Infrastructure.Data;
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
        var query = _db.InvoiceRecords
            .AsQueryable()
            .ApplyFilters(search, status, dateFrom, dateTo)
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
}
