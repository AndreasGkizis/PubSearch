using Microsoft.EntityFrameworkCore;
using ResearchPublications.API.Middleware;
using ResearchPublications.Application.Services;
using ResearchPublications.Infrastructure;
using ResearchPublications.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddScoped<PublicationService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── Database migration + seed ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbCntx>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

// ── Ensure PDF storage folder exists ──────────────────────────────────────
var pdfPath = app.Configuration["PdfStorage:Path"];
if (!string.IsNullOrWhiteSpace(pdfPath))
    Directory.CreateDirectory(pdfPath);

// ── Middleware pipeline ───────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

