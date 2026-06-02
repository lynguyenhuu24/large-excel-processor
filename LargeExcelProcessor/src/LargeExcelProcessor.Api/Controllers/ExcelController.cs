using System.Globalization;
using ClosedXML.Excel;
using LargeExcelProcessor.Api.Services;
using LargeExcelProcessor.Infrastructure.Data;
using LargeExcelProcessor.Infrastructure.Models;
using LargeExcelProcessor.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly IBlobStorageService _blob;
    private readonly AppDbContext _db;
    private readonly IExcelProcessingService _excelService;

    public ExcelController(IBlobStorageService blob, AppDbContext db, IExcelProcessingService excelService)
    {
        _blob = blob;
        _db = db;
        _excelService = excelService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<FileRequestDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx")
            return BadRequest("Only .xlsx files are supported.");

        var jobId = Guid.NewGuid();

        await using var uploadStream = file.OpenReadStream();
        var blobUri = await _blob.UploadAsync("uploads", jobId, file.FileName, uploadStream, cancellationToken);

        var fileRequest = new FileRequest
        {
            Id = jobId,
            RequestType = "Upload",
            Status = "Pending",
            FileName = file.FileName,
            FileSize = file.Length,
            BlobUri = blobUri,
            CreatedAt = DateTime.UtcNow
        };

        _db.FileRequests.Add(fileRequest);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new FileRequestDto
        {
            Id = fileRequest.Id,
            RequestType = fileRequest.RequestType,
            Status = fileRequest.Status,
            FileName = fileRequest.FileName,
            FileSize = fileRequest.FileSize,
            BlobUri = fileRequest.BlobUri,
            CreatedAt = fileRequest.CreatedAt
        });
    }

    [HttpGet("records")]
    public async Task<ActionResult<PagedResult<InvoiceRecordDto>>> GetRecords(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _excelService.GetRecordsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sample")]
    public IActionResult GenerateSample([FromQuery] int count = 100)
    {
        if (count < 1) count = 1;
        if (count > 10_000) count = 10_000;

        var rng = Random.Shared;
        var vendors = new[] { "Acme Corp", "Globex Inc", "Initech", "Umbrella Co", "Cyberdyne Systems", "Wonka Industries", "Stark Enterprises", "Wayne Enterprises" };
        var customers = new[] { "Alice Johnson", "Bob Chen", "Carol Martinez", "David Kim", "Eva Müller", "Frank Okafor", "Grace Patel", "Hiro Tanaka" };
        var statuses = new[] { "Pending", "Paid", "Overdue", "Cancelled" };
        var currencies = new[] { "USD", "USD", "USD", "EUR" };
        var notesPool = new[] { "", "", "", "", "Rush order", "Net 30 terms", "PO required", "International shipping", "Tax exempt" };

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Invoices");

        var headers = new[] {
            "InvoiceNumber", "InvoiceDate", "VendorName", "VendorTaxId", "CustomerName",
            "CustomerEmail", "LineItemCount", "Subtotal", "TaxAmount", "DiscountAmount",
            "TotalAmount", "CurrencyCode", "DueDate", "Status", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        for (int row = 0; row < count; row++)
        {
            var invDate = new DateTime(2025, 1, 1).AddDays(rng.Next(0, 545));
            var subtotal = Math.Round((decimal)(rng.NextDouble() * 9900 + 100), 2);
            var taxRate = 0.08m + (decimal)rng.NextDouble() * 0.04m;
            var tax = Math.Round(subtotal * taxRate, 2);
            var discount = rng.Next(0, 3) == 0
                ? Math.Round(subtotal * (0.01m + (decimal)rng.NextDouble() * 0.04m), 2)
                : 0;
            var total = Math.Round(subtotal + tax - discount, 2);
            var vendor = vendors[rng.Next(vendors.Length)];
            var customer = customers[rng.Next(customers.Length)];
            var email = customer.ToLowerInvariant().Replace(' ', '.') + "@example.com";

            // ReSharper disable once PossibleLossOfFraction
            ws.Cell(row + 2, 1).Value = $"INV-{invDate.Year}-{row + 1:D6}";
            ws.Cell(row + 2, 2).Value = invDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ws.Cell(row + 2, 3).Value = vendor;
            ws.Cell(row + 2, 4).Value = $"TAX-{rng.Next(10000, 99999)}";
            ws.Cell(row + 2, 5).Value = customer;
            ws.Cell(row + 2, 6).Value = email;
            ws.Cell(row + 2, 7).Value = rng.Next(1, 21);
            ws.Cell(row + 2, 8).Value = subtotal;
            ws.Cell(row + 2, 9).Value = tax;
            ws.Cell(row + 2, 10).Value = discount;
            ws.Cell(row + 2, 11).Value = total;
            ws.Cell(row + 2, 12).Value = currencies[rng.Next(currencies.Length)];
            ws.Cell(row + 2, 13).Value = invDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ws.Cell(row + 2, 14).Value = statuses[rng.Next(statuses.Length)];
            ws.Cell(row + 2, 15).Value = notesPool[rng.Next(notesPool.Length)];
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sample-invoices.xlsx");
    }
}
