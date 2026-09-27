using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Infrastructure.Persistence;
using Typesense;

namespace ResearchPublications.Infrastructure.Search;

internal sealed class TypesenseEntitySearchIndex(
    ITypesenseClient typesense,
    AppDbCntx context,
    SearchIndexSyncLock syncLock,
    ILogger<TypesenseEntitySearchIndex> logger) : IEntitySearchIndex
{
    public async Task<(IReadOnlyList<int> Ids, int TotalCount)> SearchAsync(
        EntityIndexKind kind, string query, int page, int pageSize)
    {
        var result = await typesense.Search<EntityDocument>(CollectionName(kind), new SearchParameters(query.Trim(), "name,email")
        {
            Page = page,
            PerPage = pageSize,
            NumberOfTypos = "2",
            Prefix = true,
            DropTokensThreshold = 0
        });
        return ((result.Hits ?? []).Select(hit => int.Parse(hit.Document.Id)).ToArray(), result.Found);
    }

    public async Task<bool> SynchronizeAllAsync(CancellationToken cancellationToken = default)
    {
        var success = true;
        foreach (var kind in Enum.GetValues<EntityIndexKind>())
            success = await SynchronizeAsync(kind, cancellationToken) && success;
        return success;
    }

    public async Task<bool> SynchronizeAsync(EntityIndexKind kind, CancellationToken cancellationToken = default)
    {
        await syncLock.Semaphore.WaitAsync(cancellationToken);
        try
        {
            var collection = CollectionName(kind);
            try
            {
                await typesense.RetrieveCollection(collection, cancellationToken);
            }
            catch (TypesenseApiNotFoundException)
            {
                await typesense.CreateCollection(new Schema(collection,
                [new Field("name", FieldType.String), new Field("email", FieldType.String)]));
            }

            var documents = await ReadSqlDocumentsAsync(kind, cancellationToken);
            var indexed = (await typesense.ExportDocuments<EntityDocument>(collection,
                new ExportParameters(), cancellationToken)).ToDictionary(document => document.Id);
            var changed = documents.Where(document => !indexed.TryGetValue(document.Id, out var existing)
                || existing.Name != document.Name || existing.Email != document.Email).ToList();
            if (changed.Count > 0)
            {
                var results = await typesense.ImportDocuments(collection, changed, 40, ImportType.Upsert);
                if (results.Count != changed.Count || results.Any(result => !result.Success))
                    throw new InvalidOperationException($"Entity import failed: {string.Join("; ", results.Where(result => !result.Success).Select(result => result.Error))}");
            }

            var sqlIds = documents.Select(document => document.Id).ToHashSet();
            foreach (var id in indexed.Keys.Where(id => !sqlIds.Contains(id)))
                await typesense.DeleteDocument<EntityDocument>(collection, id);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Typesense {EntityKind} synchronization failed; SQL data remains authoritative.", kind);
            return false;
        }
        finally
        {
            syncLock.Semaphore.Release();
        }
    }

    private Task<List<EntityDocument>> ReadSqlDocumentsAsync(EntityIndexKind kind, CancellationToken cancellationToken) => kind switch
    {
        EntityIndexKind.Authors => context.Authors.AsNoTracking().Select(author => new EntityDocument
        {
            Id = author.Id.ToString(),
            Name = author.FirstName + (author.MiddleName != null ? " " + author.MiddleName : "") + " " + author.LastName,
            Email = author.Email ?? string.Empty
        }).ToListAsync(cancellationToken),
        EntityIndexKind.Keywords => context.Keywords.AsNoTracking().Select(item => new EntityDocument
            { Id = item.Id.ToString(), Name = item.Value }).ToListAsync(cancellationToken),
        EntityIndexKind.Languages => context.Languages.AsNoTracking().Select(item => new EntityDocument
            { Id = item.Id.ToString(), Name = item.Value }).ToListAsync(cancellationToken),
        EntityIndexKind.PublicationTypes => context.PublicationTypes.AsNoTracking().Select(item => new EntityDocument
            { Id = item.Id.ToString(), Name = item.Value }).ToListAsync(cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    internal static string CollectionName(EntityIndexKind kind) => kind switch
    {
        EntityIndexKind.Authors => "admin_authors",
        EntityIndexKind.Keywords => "admin_keywords",
        EntityIndexKind.Languages => "admin_languages",
        EntityIndexKind.PublicationTypes => "admin_publication_types",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed class EntityDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
