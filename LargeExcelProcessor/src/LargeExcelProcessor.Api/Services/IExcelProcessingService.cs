using LargeExcelProcessor.Shared.Models;

namespace LargeExcelProcessor.Api.Services;

public interface IExcelProcessingService
{
    Task<PagedResult<InvoiceRecordDto>> GetRecordsAsync(int page, int pageSize, string? search, string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken);
}
