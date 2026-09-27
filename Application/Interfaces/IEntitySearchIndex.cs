namespace ResearchPublications.Application.Interfaces;

public enum EntityIndexKind
{
    Authors,
    Keywords,
    Languages,
    PublicationTypes
}

public interface IEntitySearchIndex
{
    Task<(IReadOnlyList<int> Ids, int TotalCount)> SearchAsync(
        EntityIndexKind kind, string query, int page, int pageSize);
    Task<bool> SynchronizeAsync(EntityIndexKind kind, CancellationToken cancellationToken = default);
    Task<bool> SynchronizeAllAsync(CancellationToken cancellationToken = default);
}
