using Microsoft.EntityFrameworkCore;
using ResearchPublications.API.Middleware;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Application.Services;
using ResearchPublications.Infrastructure;
using ResearchPublications.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<PublicationService>();
builder.Services.AddScoped<AuthorService>();
builder.Services.AddScoped<KeywordService>();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<PublicationTypeService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── Database migration + seed ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbCntx>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();

    // Preload filter-option caches
    var cacheService = scope.ServiceProvider.GetRequiredService<CacheService>();
    await cacheService.RefreshAuthorFilterOptionsAsync();
    await cacheService.RefreshKeywordFilterOptionsAsync();
    await cacheService.RefreshLanguageFilterOptionsAsync();
    await cacheService.RefreshPublicationTypeFilterOptionsAsync();

    // Initialize Typesense collection and index all publications
    var indexingService = scope.ServiceProvider.GetRequiredService<ITypesenseIndexingService>();
    await indexingService.EnsureCollectionExistsAsync();
    await indexingService.IndexAllPublicationsAsync();
}

// ── Ensure PDF storage folder exists ──────────────────────────────────────
var pdfPath = app.Configuration["PdfStorage:Path"];
if (!string.IsNullOrWhiteSpace(pdfPath))
    Directory.CreateDirectory(pdfPath);

// ── Middleware pipeline ───────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program;

