namespace LargeExcelProcessor.Shared.Models;

public class UploadResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public List<string> Errors { get; set; } = [];
}
