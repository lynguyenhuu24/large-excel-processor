using Azure.Storage.Queues;
using LargeExcelProcessor.Api.Hubs;
using LargeExcelProcessor.Infrastructure;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<UploadHub> _hubContext;

    public ExportController(AppDbContext db, IConfiguration configuration, IHubContext<UploadHub> hubContext)
    {
        _db = db;
        _configuration = configuration;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<FileRequestDto>> Export(
        [FromBody] ExportRequestDto request,
        CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();

        var fileRequest = new FileRequest
        {
            Id = jobId,
            RequestType = Constants.RequestTypeExport,
            Status = Constants.StatusPending,
            FileName = $"invoices-{(request.Status ?? "all")}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx",
            FileSize = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.FileRequests.Add(fileRequest);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var queueConnection = _configuration.GetConnectionString(Constants.ConfigConnectionStringAzureWebJobs)
                ?? Constants.DefaultConnectionString;
            var queueClient = new QueueClient(queueConnection, Constants.QueueExportRequests,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var messageObj = new { jobId, request.Search, request.Status, request.DateFrom, request.DateTo };
            var messageJson = JsonSerializer.Serialize(messageObj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await queueClient.SendMessageAsync(messageJson, cancellationToken: cancellationToken);
        }
        catch
        {
            _db.FileRequests.Remove(fileRequest);
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }

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
