using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.Infrastructure.Files;
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
        services.AddKeyedScoped<ISearchService, TypesenseSearchService>("typesense");
        services.AddKeyedScoped<ISearchService, MssqlSearchService>("mssql");
        services.AddScoped<ITypesenseIndexingService, TypesenseIndexingService>();
        services.AddScoped<IFileService, LocalFileService>();
        services.AddTransient<DbSeeder>();

        return services;
    }
}
