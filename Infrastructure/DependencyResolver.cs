using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ResearchPublications.Infrastructure;

public static class DependencyResolver
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<PublicationRepository>();
        services.AddDbContext(config);
     
        return services;
    }
}
