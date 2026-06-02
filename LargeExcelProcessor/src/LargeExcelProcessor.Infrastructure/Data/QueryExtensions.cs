using LargeExcelProcessor.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace LargeExcelProcessor.Infrastructure.Data;

public static class QueryExtensions
{
    public static IQueryable<InvoiceRecord> ApplyFilters(
        this IQueryable<InvoiceRecord> query,
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r => EF.Functions.ILike(r.InvoiceNumber, $"%{s}%")
                || EF.Functions.ILike(r.VendorName, $"%{s}%")
                || EF.Functions.ILike(r.CustomerName, $"%{s}%"));
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
