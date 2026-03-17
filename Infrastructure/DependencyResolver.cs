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

namespace ResearchPublications.Infrastructure;

public static class DependencyResolver
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var sqlSettings = config.GetSection("SqlSettings").Get<SqlSettings>()
            ?? throw new InvalidOperationException("SqlSettings section is missing from configuration.");

        services.AddSingleton(sqlSettings);

        services.AddDbContext<AppDbCntx>(opts =>
            opts.UseSqlServer(sqlSettings.FormattedConnectionString));

        services.AddScoped<IPublicationRepository, PublicationRepository>();
        services.AddScoped<ISearchService, MssqlSearchService>();
        services.AddScoped<IFileService, LocalFileService>();
        services.AddTransient<DbSeeder>();

        return services;
    }
}
