using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace ResearchPublications.IntegrationTests.Fixtures;

/// <summary>
/// Shared collection so all test classes reuse the same SQL Server container and test server.
/// Decorate test classes with [Collection("Integration")] to opt in.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<PubSearchApiFactory>;

/// <summary>
/// Spins up a real SQL Server via Testcontainers and boots the API on top of it.
/// Migrations run automatically on first request (the app calls MigrateAsync in Program.cs).
/// </summary>
public class PubSearchApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DbPassword = "Test@Strong12345!";

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithPassword(DbPassword)
        .Build();

    /// <summary>The temporary PDF storage directory used by this test server instance.</summary>
    public string PdfStoragePath { get; } = Path.Combine(Path.GetTempPath(), $"pubsearch-tests-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SqlSettings:Server"]   = _dbContainer.Hostname,
                ["SqlSettings:Port"]     = _dbContainer.GetMappedPublicPort(1433).ToString(),
                ["SqlSettings:DbName"]   = "master",
                ["SqlSettings:UserId"]   = "sa",
                ["SqlSettings:Password"] = DbPassword,
                ["PdfStorage:Path"]      = PdfStoragePath,
            });
        });

        // AddInfrastructure in Program.cs eagerly materialises SqlSettings and
        // bakes the connection string into DbContextOptions BEFORE the config
        // overrides above are applied.  Re-register the DbContext here so it
        // points at the Testcontainer instead of the appsettings values.
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbCntx>));
            if (descriptor != null)
                services.Remove(descriptor);

            var connectionString =
                $"Server={_dbContainer.Hostname},{_dbContainer.GetMappedPublicPort(1433)};" +
                $"Database=master;User Id=sa;Password={DbPassword};TrustServerCertificate=True";

            services.AddDbContext<AppDbCntx>(opts =>
                opts.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly("ResearchPublications.Infrastructure")
                           .MigrationsHistoryTable("__EFMigrationsHistory")));
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _dbContainer.DisposeAsync();
}
