namespace ResearchPublications.Application.Interfaces;

public interface ITypesensePublicationIndexService
{
    Task<SearchIndexReconciliationResult> SynchronizeFromSqlAsync(CancellationToken cancellationToken = default);
    Task<SearchIndexReconciliationResult> RebuildAsync(CancellationToken cancellationToken = default);
}


// ----------- models ----------- //
public sealed record SearchIndexReconciliationResult(
    bool Success,
    int Added,
    int Updated,
    int Deleted,
    int Unchanged,
    string? ErrorMessage = null);
