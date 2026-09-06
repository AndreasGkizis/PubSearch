using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Settings;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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

    private readonly IContainer _dbContainer = new ContainerBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_SA_PASSWORD", DbPassword)
        .WithPortBinding(1433, true)
        .Build();

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
                ["PdfStorage:Path"]      = Path.Combine(Path.GetTempPath(), $"pubsearch-tests-{Guid.NewGuid():N}"),
                ["SearchIndexSync:Enabled"] = "false",
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

            var syncSettingsDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(SearchIndexSyncSettings));
            if (syncSettingsDescriptor is not null)
                services.Remove(syncSettingsDescriptor);
            services.AddSingleton(new SearchIndexSyncSettings { Enabled = false });
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await WaitForSqlServerAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await _dbContainer.DisposeAsync();

    private async Task WaitForSqlServerAsync()
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(
                    $"Server={_dbContainer.Hostname},{_dbContainer.GetMappedPublicPort(1433)};" +
                    $"Database=master;User Id=sa;Password={DbPassword};TrustServerCertificate=True;Connect Timeout=2");
                await connection.OpenAsync();
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("SQL Server did not become ready.", lastError);
    }
}
