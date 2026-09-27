using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Settings;

namespace ResearchPublications.Infrastructure.Ocr;

internal sealed class OcrWorker(
    IServiceScopeFactory scopeFactory,
    OcrSettings settings,
    ILogger<OcrWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("OCR background processing is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IOcrIndexingService>();
                await service.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OCR background scan failed; it will be retried.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, settings.IntervalSeconds)), stoppingToken);
        }
    }
}
