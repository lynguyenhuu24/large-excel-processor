using LargeExcelProcessor.Api.Hubs;
using LargeExcelProcessor.Api.Services;
using LargeExcelProcessor.Infrastructure;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IBlobStorageService _blob;
    private readonly AppDbContext _db;
    private readonly IHubContext<UploadHub> _hubContext;

    public UploadController(IBlobStorageService blob, AppDbContext db, IHubContext<UploadHub> hubContext)
    {
        _blob = blob;
        _db = db;
        _hubContext = hubContext;
    }

    [HttpPost]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<FileRequestDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
            return BadRequest("Only .xlsx files are supported.");

        var jobId = Guid.NewGuid();

        await using var uploadStream = file.OpenReadStream();
        var blobUri = await _blob.UploadAsync(Constants.BlobPrefixUploads, jobId, file.FileName, uploadStream, cancellationToken);

        var fileRequest = new FileRequest
        {
            Id = jobId,
            RequestType = Constants.RequestTypeUpload,
            Status = Constants.StatusPending,
            FileName = file.FileName,
            FileSize = file.Length,
            BlobUri = blobUri,
            CreatedAt = DateTime.UtcNow
        };

        _db.FileRequests.Add(fileRequest);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(fileRequest);

        await _hubContext.Clients.Group(Constants.SignalRGroupRequests).SendAsync(Constants.SignalRMethodNewRequest, dto, cancellationToken);

        return Ok(dto);
    }

    private static FileRequestDto MapToDto(FileRequest r) => new()
    {
        Id = r.Id,
        RequestType = r.RequestType,
        Status = r.Status,
        FileName = r.FileName,
        FileSize = r.FileSize,
        BlobUri = r.BlobUri,
        CreatedAt = r.CreatedAt
    };
}
