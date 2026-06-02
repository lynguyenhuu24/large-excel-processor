namespace LargeExcelProcessor.Shared.Models;

public class NotificationDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? TotalRows { get; set; }
    public int? ImportedRows { get; set; }
    public string? ErrorMessage { get; set; }
}
