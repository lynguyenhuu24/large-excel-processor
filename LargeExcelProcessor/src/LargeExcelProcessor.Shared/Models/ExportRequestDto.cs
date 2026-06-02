namespace LargeExcelProcessor.Shared.Models;

public class ExportRequestDto
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
