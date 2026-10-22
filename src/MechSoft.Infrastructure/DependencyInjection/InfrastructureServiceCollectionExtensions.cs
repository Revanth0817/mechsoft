using MechSoft.Application.Interfaces;
using MechSoft.Infrastructure.Persistence;
using MechSoft.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace MechSoft.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // QuestPDF's free Community license covers this project's use case
        // (small business, non-commercial-scale document generation).
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IPartService, PartService>();
        services.AddScoped<ICadFileService, CadFileService>();
        services.Configure<ApsOptions>(configuration.GetSection("Aps"));
        services.AddHttpClient<IApsService, ApsService>(c => c.Timeout = TimeSpan.FromMinutes(5));
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IDeliveryChallanService, DeliveryChallanService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPdfDocumentService, PdfDocumentService>();

        return services;
    }
}
