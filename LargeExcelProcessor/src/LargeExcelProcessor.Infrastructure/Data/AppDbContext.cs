using LargeExcelProcessor.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace LargeExcelProcessor.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<InvoiceRecord> InvoiceRecords => Set<InvoiceRecord>();
    public DbSet<FileRequest> FileRequests => Set<FileRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        base.OnModelCreating(modelBuilder);
    }
}
