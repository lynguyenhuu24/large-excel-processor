using LargeExcelProcessor.Shared.Models;

namespace LargeExcelProcessor.Api.Services;

public interface IExcelProcessingService
{
    Task<UploadResultDto> ProcessExcelAsync(Stream excelStream, CancellationToken cancellationToken);
    Task<PagedResult<InvoiceRecordDto>> GetRecordsAsync(int page, int pageSize, CancellationToken cancellationToken);
}
