using System.ComponentModel.DataAnnotations;

namespace LargeExcelProcessor.Shared.Models;

public class ExportRequestDto
{
    [MaxLength(500)]
    public string? Search { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
