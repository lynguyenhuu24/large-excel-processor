using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LargeExcelProcessor.Infrastructure.Models;

[Table("invoice_records")]
public class InvoiceRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_number")]
    [MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("invoice_date")]
    public DateTime InvoiceDate { get; set; }

    [Column("vendor_name")]
    [MaxLength(500)]
    public string VendorName { get; set; } = string.Empty;

    [Column("vendor_tax_id")]
    [MaxLength(100)]
    public string? VendorTaxId { get; set; }

    [Column("customer_name")]
    [MaxLength(500)]
    public string CustomerName { get; set; } = string.Empty;

    [Column("customer_email")]
    [MaxLength(300)]
    public string? CustomerEmail { get; set; }

    [Column("line_item_count")]
    public int LineItemCount { get; set; }

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("tax_amount")]
    public decimal TaxAmount { get; set; }

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("currency_code")]
    [MaxLength(10)]
    public string CurrencyCode { get; set; } = "USD";

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("batch_id")]
    [MaxLength(50)]
    public string? BatchId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
