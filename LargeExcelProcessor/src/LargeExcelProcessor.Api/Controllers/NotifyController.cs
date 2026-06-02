using LargeExcelProcessor.Api.Hubs;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotifyController : ControllerBase
{
    private readonly IHubContext<UploadHub> _hubContext;
    private readonly AppDbContext _db;

    public NotifyController(IHubContext<UploadHub> hubContext, AppDbContext db)
    {
        _hubContext = hubContext;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Notify([FromBody] NotificationDto notification, CancellationToken cancellationToken)
    {
        var job = await _db.FileRequests.FindAsync([notification.JobId], cancellationToken);
        if (job is null)
            return NotFound();

        job.Status = notification.Status;
        if (notification.TotalRows.HasValue) job.TotalRows = notification.TotalRows;
        if (notification.ImportedRows.HasValue) job.ImportedRows = notification.ImportedRows;
        if (notification.FileSize > 0) job.FileSize = notification.FileSize;
        if (notification.ResultBlobUri != null) job.ResultBlobUri = notification.ResultBlobUri;
        if (notification.ErrorMessage != null) job.ErrorMessage = notification.ErrorMessage;

        if (notification.Status is "Completed" or "Failed")
            job.CompletedAt = notification.CompletedAt ?? DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.Group(notification.JobId.ToString()).SendAsync(
            "ProcessingCompleted", notification, cancellationToken);

        await _hubContext.Clients.Group("requests").SendAsync(
            "RequestStatusChanged", notification, cancellationToken);

        return Ok();
    }
}
