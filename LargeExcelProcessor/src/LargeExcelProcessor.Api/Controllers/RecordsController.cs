using LargeExcelProcessor.Api.Services;
using LargeExcelProcessor.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordsController : ControllerBase
{
    private readonly IExcelProcessingService _excelService;

    public RecordsController(IExcelProcessingService excelService)
    {
        _excelService = excelService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceRecordDto>>> GetRecords(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var result = await _excelService.GetRecordsAsync(page, pageSize, search, status, dateFrom, dateTo, cancellationToken);
        return Ok(result);
    }
}
