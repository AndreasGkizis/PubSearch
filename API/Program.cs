using ResearchPublications.API.Middleware;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Application.Services;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.Infrastructure.Files;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Persistence.Repositories;
using ResearchPublications.Infrastructure.Search;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<IPublicationRepository, PublicationRepository>();
builder.Services.AddScoped<PublicationService>();

// TODO: To swap search, replace MssqlSearchService with TypesenseSearchService
//       (or any ISearchService implementation) here only.
builder.Services.AddScoped<ISearchService, MssqlSearchService>();

// TODO: To swap file storage, replace LocalFileService with AzureBlobFileService
//       (or any IFileService implementation) here only.
builder.Services.AddScoped<IFileService, LocalFileService>();

builder.Services.AddTransient<DatabaseInitializer>();

var app = builder.Build();

// ── Database initialisation ───────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
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
