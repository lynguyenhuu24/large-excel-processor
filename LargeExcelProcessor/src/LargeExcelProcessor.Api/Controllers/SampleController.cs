using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Globalization;

namespace LargeExcelProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    [HttpGet]
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

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Invoices");

        var headers = new[] {
            "InvoiceNumber", "InvoiceDate", "VendorName", "VendorTaxId", "CustomerName",
            "CustomerEmail", "LineItemCount", "Subtotal", "TaxAmount", "DiscountAmount",
            "TotalAmount", "CurrencyCode", "DueDate", "Status", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cells[1, i + 1].Value = headers[i];

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

            ws.Cells[row + 2, 1].Value = $"INV-{invDate.Year}-{row + 1:D6}";
            ws.Cells[row + 2, 2].Value = invDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ws.Cells[row + 2, 3].Value = vendor;
            ws.Cells[row + 2, 4].Value = $"TAX-{rng.Next(10000, 99999)}";
            ws.Cells[row + 2, 5].Value = customer;
            ws.Cells[row + 2, 6].Value = email;
            ws.Cells[row + 2, 7].Value = rng.Next(1, 21);
            ws.Cells[row + 2, 8].Value = subtotal;
            ws.Cells[row + 2, 9].Value = tax;
            ws.Cells[row + 2, 10].Value = discount;
            ws.Cells[row + 2, 11].Value = total;
            ws.Cells[row + 2, 12].Value = currencies[rng.Next(currencies.Length)];
            ws.Cells[row + 2, 13].Value = invDate.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ws.Cells[row + 2, 14].Value = statuses[rng.Next(statuses.Length)];
            ws.Cells[row + 2, 15].Value = notesPool[rng.Next(notesPool.Length)];
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();

        using var ms = new MemoryStream();
        package.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);

        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sample-invoices.xlsx");
    }
}
