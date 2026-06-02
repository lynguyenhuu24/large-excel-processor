namespace LargeExcelProcessor.Shared.Models;

public class InvoiceRecordDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? VendorTaxId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public int LineItemCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
    public string? BatchId { get; set; }
    public DateTime CreatedAt { get; set; }
}
