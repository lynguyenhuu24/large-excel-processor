using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LargeExcelProcessor.Infrastructure.Models;

[Table("file_requests")]
public class FileRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("request_type")]
    [MaxLength(50)]
    public string RequestType { get; set; } = "Upload";

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Column("file_name")]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("blob_uri")]
    public string? BlobUri { get; set; }

    [Column("result_blob_uri")]
    public string? ResultBlobUri { get; set; }

    [Column("total_rows")]
    public int? TotalRows { get; set; }

    [Column("imported_rows")]
    public int? ImportedRows { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}
