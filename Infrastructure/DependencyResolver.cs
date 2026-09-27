using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.Infrastructure.Files;
using ResearchPublications.Infrastructure.Ocr;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Persistence.Repositories;
using ResearchPublications.Infrastructure.Search;
using ResearchPublications.Infrastructure.Settings;
using Typesense;
using Typesense.Setup;

namespace ResearchPublications.Infrastructure;

public static class DependencyResolver
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var sqlSettings = config.GetSection("SqlSettings").Get<SqlSettings>()
            ?? throw new InvalidOperationException("SqlSettings section is missing from configuration.");

        services.AddSingleton(sqlSettings);

        services.AddDbContext<AppDbCntx>(opts =>
            opts.UseSqlServer(sqlSettings.FormattedConnectionString,
                x => x.MigrationsAssembly("ResearchPublications.Infrastructure")
                       .MigrationsHistoryTable("__EFMigrationsHistory")));

        // Typesense
        var typesenseSettings = config.GetSection("TypesenseSettings").Get<TypesenseSettings>()
            ?? throw new InvalidOperationException("TypesenseSettings section is missing from configuration.");

        services.AddSingleton(typesenseSettings);

        var searchIndexSyncSettings = config.GetSection("SearchIndexSync").Get<SearchIndexSyncSettings>()
            ?? new SearchIndexSyncSettings();
        services.AddSingleton(searchIndexSyncSettings);

        var ocrSettings = config.GetSection("Ocr").Get<OcrSettings>() ?? new OcrSettings();
        services.AddSingleton(ocrSettings);

        services.AddTypesenseClient(opts =>
        {
            opts.ApiKey = typesenseSettings.ApiKey;
            opts.Nodes = [new Node(typesenseSettings.Host, typesenseSettings.Port.ToString(), typesenseSettings.Protocol)];
        });

        services.AddScoped<IPublicationRepository, PublicationRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IKeywordRepository, KeywordRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IPublicationTypeRepository, PublicationTypeRepository>();
        services.AddScoped<ISearchService, TypesenseSearchService>();
        services.AddScoped<IOcrSearchService, TypesenseOcrSearchService>();
        services.AddSingleton<SearchIndexSyncLock>();
        services.AddScoped<IEntitySearchIndex, TypesenseEntitySearchIndex>();
        services.AddScoped<ITypesensePublicationIndexService, TypesensePublicationIndexService>();
        services.AddHostedService<SearchIndexSyncWorker>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IOcrPageRenderer, PdfToImagePageRenderer>();
        services.AddHttpClient<IOllamaOcrClient, OllamaOcrClient>(client =>
            client.BaseAddress = new Uri(ocrSettings.OllamaBaseUrl.TrimEnd('/') + "/"));
        services.AddScoped<IOcrPdfProcessor, OcrPdfProcessor>();
        services.AddScoped<IOcrIndexingService, TypesenseOcrIndexingService>();
        services.AddHostedService<OcrWorker>();
        services.AddScoped<IFileService, LocalFileService>();
        services.AddTransient<DbSeeder>();

        return services;
    }
}
