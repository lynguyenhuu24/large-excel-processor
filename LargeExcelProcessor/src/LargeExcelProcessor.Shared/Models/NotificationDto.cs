namespace LargeExcelProcessor.Shared.Models;

public class NotificationDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? TotalRows { get; set; }
    public int? ImportedRows { get; set; }
    public long FileSize { get; set; }
    public string? ResultBlobUri { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
}
