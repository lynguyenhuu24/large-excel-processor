using LargeExcelProcessor.Api.Services;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;
    private readonly ILogger<FileRequestsController> _logger;

    public FileRequestsController(AppDbContext db, IBlobStorageService blob, ILogger<FileRequestsController> logger)
    {
        _db = db;
        _blob = blob;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<FileRequestDto>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.FileRequests.OrderByDescending(r => r.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new FileRequestDto
            {
                Id = r.Id,
                RequestType = r.RequestType,
                Status = r.Status,
                FileName = r.FileName,
                FileSize = r.FileSize,
                BlobUri = r.BlobUri,
                ResultBlobUri = r.ResultBlobUri,
                TotalRows = r.TotalRows,
                ImportedRows = r.ImportedRows,
                ErrorMessage = r.ErrorMessage,
                CreatedAt = r.CreatedAt,
                CompletedAt = r.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<FileRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FileRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await _db.FileRequests
            .Where(r => r.Id == id)
            .Select(r => new FileRequestDto
            {
                Id = r.Id,
                RequestType = r.RequestType,
                Status = r.Status,
                FileName = r.FileName,
                FileSize = r.FileSize,
                BlobUri = r.BlobUri,
                ResultBlobUri = r.ResultBlobUri,
                TotalRows = r.TotalRows,
                ImportedRows = r.ImportedRows,
                ErrorMessage = r.ErrorMessage,
                CreatedAt = r.CreatedAt,
                CompletedAt = r.CompletedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request is null)
            return NotFound();

        return Ok(request);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var fileRequest = await _db.FileRequests.FindAsync(new object[] { id }, cancellationToken);
        if (fileRequest is null)
            return NotFound();

        if (!string.IsNullOrEmpty(fileRequest.BlobUri))
            await _blob.DeleteAsync(fileRequest.BlobUri, cancellationToken);

        if (!string.IsNullOrEmpty(fileRequest.ResultBlobUri))
            await _blob.DeleteAsync(fileRequest.ResultBlobUri, cancellationToken);

        var batchKey = id.ToString();
        var importedRows = await _db.InvoiceRecords
            .Where(r => r.BatchId == batchKey)
            .CountAsync(cancellationToken);

        if (importedRows > 0)
        {
            await _db.InvoiceRecords
                .Where(r => r.BatchId == batchKey)
                .ExecuteDeleteAsync(cancellationToken);
        }

        _db.FileRequests.Remove(fileRequest);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted file request {Id} with {Rows} imported rows", id, importedRows);

        return NoContent();
    }
}
