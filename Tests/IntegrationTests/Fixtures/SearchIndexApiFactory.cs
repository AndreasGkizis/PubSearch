using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchPublications.Infrastructure.Persistence;
using ResearchPublications.Infrastructure.Settings;
using Typesense;
using Typesense.Setup;
using Xunit;

namespace ResearchPublications.IntegrationTests.Fixtures;

[CollectionDefinition("SearchIndexIntegration")]
public class SearchIndexIntegrationCollection : ICollectionFixture<SearchIndexApiFactory>;

public sealed class SearchIndexApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DbPassword = "Test@Strong12345!";
    private const string TypesenseApiKey = "search-index-tests";

    private readonly IContainer _dbContainer = new ContainerBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_SA_PASSWORD", DbPassword)
        .WithPortBinding(1433, true)
        .Build();

    private readonly IContainer _typesenseContainer = new ContainerBuilder()
        .WithImage("typesense/typesense:27.1")
        .WithCommand($"--api-key={TypesenseApiKey}", "--data-dir=/tmp")
        .WithPortBinding(8108, true)
        .Build();

    public ITypesenseClient TypesenseClient => Services.GetRequiredService<ITypesenseClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SqlSettings:Server"] = _dbContainer.Hostname,
                ["SqlSettings:Port"] = _dbContainer.GetMappedPublicPort(1433).ToString(),
                ["SqlSettings:DbName"] = "master",
                ["SqlSettings:UserId"] = "sa",
                ["SqlSettings:Password"] = DbPassword,
                ["TypesenseSettings:Host"] = _typesenseContainer.Hostname,
                ["TypesenseSettings:Port"] = _typesenseContainer.GetMappedPublicPort(8108).ToString(),
                ["TypesenseSettings:Protocol"] = "http",
                ["TypesenseSettings:ApiKey"] = TypesenseApiKey,
                ["SearchIndexSync:Enabled"] = "true",
                ["SearchIndexSync:IntervalSeconds"] = "1",
                ["PdfStorage:Path"] = Path.Combine(Path.GetTempPath(), $"pubsearch-index-tests-{Guid.NewGuid():N}"),
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(DbContextOptions<AppDbCntx>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            var connectionString =
                $"Server={_dbContainer.Hostname},{_dbContainer.GetMappedPublicPort(1433)};" +
                $"Database=master;User Id=sa;Password={DbPassword};TrustServerCertificate=True";

            services.AddDbContext<AppDbCntx>(options =>
                options.UseSqlServer(connectionString,
                    sql => sql.MigrationsAssembly("ResearchPublications.Infrastructure")
                        .MigrationsHistoryTable("__EFMigrationsHistory")));

            var syncSettingsDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(SearchIndexSyncSettings));
            if (syncSettingsDescriptor is not null)
                services.Remove(syncSettingsDescriptor);
            services.AddSingleton(new SearchIndexSyncSettings { Enabled = true, IntervalSeconds = 1 });

            foreach (var descriptor in services.Where(item => item.ServiceType == typeof(ITypesenseClient)).ToList())
                services.Remove(descriptor);

            services.AddTypesenseClient(options =>
            {
                options.ApiKey = TypesenseApiKey;
                options.Nodes =
                [
                    new Node(
                        _typesenseContainer.Hostname,
                        _typesenseContainer.GetMappedPublicPort(8108).ToString(),
                        "http")
                ];
            });
        });

        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync()
    {
        // Podman's Docker compatibility API is more reliable when container
        // lifecycle operations are not issued concurrently.
        await _dbContainer.StartAsync();
        await WaitForSqlServerAsync();
        await _typesenseContainer.StartAsync();
        await WaitForTypesenseAsync();
    }

    public Task StopTypesenseAsync() => _typesenseContainer.StopAsync();

    public async Task StartTypesenseAsync()
    {
        await _typesenseContainer.StartAsync();
        await WaitForTypesenseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_dbContainer.DisposeAsync().AsTask(), _typesenseContainer.DisposeAsync().AsTask());
    }

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

    private async Task WaitForTypesenseAsync()
    {
        using var client = new HttpClient();
        var healthUrl = new Uri(
            $"http://{_typesenseContainer.Hostname}:{_typesenseContainer.GetMappedPublicPort(8108)}/health");

        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(healthUrl);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
                // Expected while Typesense starts.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("Typesense did not become ready.");
    }
}
