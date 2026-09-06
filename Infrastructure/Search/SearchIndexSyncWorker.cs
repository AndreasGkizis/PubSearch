using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Settings;

namespace ResearchPublications.Infrastructure.Search;

internal sealed class SearchIndexSyncWorker(
    IServiceScopeFactory scopeFactory,
    SearchIndexSyncSettings settings,
    ILogger<SearchIndexSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Background search-index synchronization is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.IntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITypesensePublicationIndexService>();
            await service.SynchronizeFromSqlAsync(stoppingToken);
        }
    }
}
