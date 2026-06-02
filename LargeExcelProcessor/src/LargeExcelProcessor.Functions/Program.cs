using LargeExcelProcessor.Infrastructure;
using LargeExcelProcessor.Infrastructure.Data;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;

namespace LargeExcelProcessor.Functions;

public class Program
{
    public static void Main(string[] args)
    {
        ExcelPackage.License.SetNonCommercialPersonal(Constants.EpplusLicenseAppName);
        var builder = FunctionsApplication.CreateBuilder(args);

        builder.ConfigureFunctionsWebApplication();

        var connectionString = Environment.GetEnvironmentVariable(Constants.EnvVarConnectionStringsDefault);

        builder.Services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        builder.Services.AddHttpClient();

        builder.Build().Run();
    }
}
