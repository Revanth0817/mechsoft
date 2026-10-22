using MechSoft.Application.DependencyInjection;
using MechSoft.Infrastructure.DependencyInjection;
using MechSoft.Infrastructure.Persistence;
using MechSoft.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// By default, ASP.NET Core's model metadata converts an empty posted string to null
// (ModelMetadata.ConvertEmptyStringToNull = true). Every document form here (Purchase
// Orders, Delivery Challans, Invoices, Parts) has legitimately optional text fields
// (GSTIN, PAN, notes, etc.) backed by non-nullable NOT NULL string columns, so leaving
// one blank would otherwise insert NULL and violate the column constraint. Disable the
// conversion globally so a blank field round-trips as "" instead of null.
builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    options.ModelMetadataDetailsProviders.Add(new SuppressEmptyStringToNullMetadataProvider());
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed mock data on startup.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    // LocalDB can be briefly unavailable right after it (re)starts, so retry a few times
    // before giving up instead of leaving the app running without a database.
    const int maxAttempts = 4;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await DbInitializer.InitializeAsync(context, logger);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt} of {Max}); retrying in 3 seconds.", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

internal sealed class SuppressEmptyStringToNullMetadataProvider : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.ModelType == typeof(string))
        {
            context.DisplayMetadata.ConvertEmptyStringToNull = false;
        }
    }
}
