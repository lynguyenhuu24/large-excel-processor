using System.ComponentModel.DataAnnotations;

namespace LargeExcelProcessor.Shared.Models;

public class FileRequestDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string RequestType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public string? BlobUri { get; set; }
    public string? ResultBlobUri { get; set; }
    public int? TotalRows { get; set; }
    public int? ImportedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
