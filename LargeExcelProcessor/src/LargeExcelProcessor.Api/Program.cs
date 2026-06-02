using LargeExcelProcessor.Api.Hubs;
using LargeExcelProcessor.Api.Services;
using LargeExcelProcessor.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace LargeExcelProcessor.Api;

public class Program
{
    public static void Main(string[] args)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Invoicing App");
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

        builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
        builder.Services.AddScoped<IExcelProcessingService, ExcelProcessingService>();

        builder.Services.AddSignalR();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<UploadHub>("/hubs/upload");

        app.Run();
    }
}
