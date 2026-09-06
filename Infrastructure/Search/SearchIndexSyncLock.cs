namespace ResearchPublications.Infrastructure.Search;

internal sealed class SearchIndexSyncLock
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
