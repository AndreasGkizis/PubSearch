using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Infrastructure.Persistence;
using Typesense;

namespace ResearchPublications.Infrastructure.Search;

internal sealed class TypesensePublicationIndexService(
    ITypesenseClient typesense,
    AppDbCntx context,
    SearchIndexSyncLock syncLock,
    ILogger<TypesensePublicationIndexService> logger) : ITypesensePublicationIndexService
{
    private const string CollectionName = "publications";

    public async Task<SearchIndexReconciliationResult> SynchronizeFromSqlAsync(CancellationToken cancellationToken = default)
    {
        await syncLock.Semaphore.WaitAsync(cancellationToken);
        try
        {
            return await SynchronizeFromSqlCoreAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Typesense synchronization from SQL failed. SQL data remains authoritative.");
            return Failed(ex);
        }
        finally
        {
            syncLock.Semaphore.Release();
        }
    }

    public async Task<SearchIndexReconciliationResult> RebuildAsync(CancellationToken cancellationToken = default)
    {
        await syncLock.Semaphore.WaitAsync(cancellationToken);
        try
        {
            var deleted = 0;
            try
            {
                var collection = await typesense.RetrieveCollection(CollectionName, cancellationToken);
                deleted = collection.NumberOfDocuments;
                await typesense.DeleteCollection(CollectionName, compactStore: true);
            }
            catch (TypesenseApiNotFoundException)
            {
                // The collection will be created below.
            }

            var result = await SynchronizeFromSqlCoreAsync(cancellationToken);
            return result with { Deleted = deleted };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Typesense index rebuild failed. SQL data remains authoritative.");
            return Failed(ex);
        }
        finally
        {
            syncLock.Semaphore.Release();
        }
    }

    private async Task<SearchIndexReconciliationResult> SynchronizeFromSqlCoreAsync(CancellationToken cancellationToken)
    {
        await EnsureCollectionExistsAsync(cancellationToken);

        var publications = await context.Publications
            .AsNoTracking()
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .Include(p => p.Languages)
            .Include(p => p.PublicationTypes)
            .ToListAsync(cancellationToken);

        var sqlDocuments = publications.Select(ToDocument).ToDictionary(document => document.Id);
        var indexedDocuments = (await typesense.ExportDocuments<SearchIndexStateDocument>(
                CollectionName,
                new ExportParameters { IncludeFields = "id,content_hash" },
                cancellationToken))
            .ToDictionary(document => document.Id);

        var addedDocuments = sqlDocuments.Values
            .Where(document => !indexedDocuments.ContainsKey(document.Id))
            .ToList();
        var updatedDocuments = sqlDocuments.Values
            .Where(document => indexedDocuments.TryGetValue(document.Id, out var indexed)
                && !string.Equals(indexed.ContentHash, document.ContentHash, StringComparison.Ordinal))
            .ToList();
        var unchanged = sqlDocuments.Count - addedDocuments.Count - updatedDocuments.Count;
        var importCandidates = addedDocuments.Concat(updatedDocuments).ToList();
        var added = 0;
        var updated = 0;
        var errors = new List<string>();

        if (importCandidates.Count > 0)
        {
            var importResults = await typesense.ImportDocuments(
                CollectionName, importCandidates, 40, ImportType.Upsert);

            for (var i = 0; i < importResults.Count; i++)
            {
                if (importResults[i].Success)
                {
                    if (i < addedDocuments.Count) added++;
                    else updated++;
                }
                else
                {
                    errors.Add(importResults[i].Error ?? $"Document {importCandidates[i].Id} failed to import.");
                }
            }
        }

        var deleted = 0;
        foreach (var indexed in indexedDocuments.Values.Where(document => !sqlDocuments.ContainsKey(document.Id)))
        {
            await typesense.DeleteDocument<PublicationDocument>(CollectionName, indexed.Id);
            deleted++;
        }

        if (errors.Count > 0)
        {
            var message = string.Join(" ", errors);
            logger.LogWarning("Typesense synchronization from SQL completed with import failures: {Error}", message);
            return new(false, added, updated, deleted, unchanged, message);
        }

        logger.LogInformation(
            "Typesense synchronization from SQL complete: {Added} added, {Updated} updated, {Deleted} deleted, {Unchanged} unchanged.",
            added, updated, deleted, unchanged);
        return new(true, added, updated, deleted, unchanged);
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await typesense.RetrieveCollection(CollectionName, cancellationToken);
        }
        catch (TypesenseApiNotFoundException)
        {
            await typesense.CreateCollection(CreateSchema());
        }
    }

    private static Schema CreateSchema() => new(
        CollectionName,
        [
            new Field("title", FieldType.String, facet: false),
            new Field("abstract", FieldType.String, facet: false, optional: true),
            new Field("body", FieldType.String, facet: false, optional: true, index: true),
            new Field("authors", FieldType.StringArray, facet: true),
            new Field("keywords", FieldType.StringArray, facet: true),
            new Field("languages", FieldType.StringArray, facet: true),
            new Field("publication_types", FieldType.StringArray, facet: true),
            new Field("year", FieldType.Int32, facet: true, optional: true),
            new Field("doi", FieldType.String, facet: false, optional: true, index: false),
            new Field("pdf_file_name", FieldType.String, facet: false, optional: true, index: false),
            new Field("last_modified_timestamp", FieldType.Int64, facet: false),
            new Field("content_hash", FieldType.String, facet: false, optional: true, index: false),
        ]);

    private static PublicationDocument ToDocument(Publication publication)
    {
        var document = new PublicationDocument
        {
            Id = publication.Id.ToString(),
            Title = publication.Title,
            Abstract = publication.Abstract ?? string.Empty,
            Body = publication.Body ?? string.Empty,
            Authors = publication.Authors.Select(FormatAuthorName).Order(StringComparer.Ordinal).ToArray(),
            Keywords = publication.Keywords.Select(item => item.Value).Order(StringComparer.Ordinal).ToArray(),
            Languages = publication.Languages.Select(item => item.Value).Order(StringComparer.Ordinal).ToArray(),
            PublicationTypes = publication.PublicationTypes.Select(item => item.Value).Order(StringComparer.Ordinal).ToArray(),
            Year = publication.Year ?? 0,
            Doi = publication.DOI ?? string.Empty,
            PdfFileName = publication.PdfFileName ?? string.Empty,
            LastModifiedTimestamp = new DateTimeOffset(publication.LastModified).ToUnixTimeSeconds()
        };

        document.ContentHash = CalculateContentHash(document);
        return document;
    }

    private static string FormatAuthorName(Author author) => string.IsNullOrWhiteSpace(author.MiddleName)
        ? $"{author.FirstName} {author.LastName}".Trim()
        : $"{author.FirstName} {author.MiddleName} {author.LastName}".Trim();

    private static string CalculateContentHash(PublicationDocument document)
    {
        var content = JsonSerializer.Serialize(new
        {
            document.Id,
            document.Title,
            document.Abstract,
            document.Body,
            document.Authors,
            document.Keywords,
            document.Languages,
            document.PublicationTypes,
            document.Year,
            document.Doi,
            document.PdfFileName,
            document.LastModifiedTimestamp
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }

    private static SearchIndexReconciliationResult Failed(Exception ex) => new(false, 0, 0, 0, 0, ex.Message);

    private sealed class SearchIndexStateDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("content_hash")]
        public string ContentHash { get; set; } = string.Empty;
    }
}
