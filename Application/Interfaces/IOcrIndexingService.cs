namespace ResearchPublications.Application.Interfaces;

public interface IOcrIndexingService
{
    Task ProcessPendingAsync(CancellationToken cancellationToken = default);
}
