using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Infrastructure.Persistence;
using Typesense;

namespace ResearchPublications.Infrastructure.Search;

public class TypesenseIndexingService(
    ITypesenseClient typesense,
    AppDbCntx context,
    ILogger<TypesenseIndexingService> logger) : ITypesenseIndexingService
{
    private const string CollectionName = "publications";

    public async Task EnsureCollectionExistsAsync()
    {
        try
        {
            await typesense.RetrieveCollection(CollectionName);
            logger.LogInformation("Typesense collection '{Collection}' already exists.", CollectionName);
        }
        catch (TypesenseApiNotFoundException)
        {
            var schema = new Schema(
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
                ]);

            await typesense.CreateCollection(schema);
            logger.LogInformation("Created Typesense collection '{Collection}'.", CollectionName);
        }
    }

    public async Task IndexAllPublicationsAsync()
    {
        var publications = await context.Publications
            .AsNoTracking()
            .Include(p => p.Authors)
            .Include(p => p.Keywords)
            .Include(p => p.Languages)
            .Include(p => p.PublicationTypes)
            .ToListAsync();

        if (publications.Count == 0)
            return;

        var documents = publications.Select(ToDocument).ToList();

        var results = await typesense.ImportDocuments(CollectionName, documents, 40, ImportType.Upsert);
        var failures = results.Where(r => !r.Success).ToList();

        if (failures.Count > 0)
            logger.LogWarning("Failed to index {Count} publications into Typesense.", failures.Count);

        logger.LogInformation("Indexed {Count} publications into Typesense.", publications.Count - failures.Count);
    }

    public async Task IndexPublicationAsync(Publication publication)
    {
        var document = ToDocument(publication);
        await typesense.UpsertDocument(CollectionName, document);
    }

    public async Task RemovePublicationAsync(int id)
    {
        try
        {
            await typesense.DeleteDocument<PublicationDocument>(CollectionName, id.ToString());
        }
        catch (TypesenseApiNotFoundException)
        {
            // Document already removed, nothing to do.
        }
    }

    private static PublicationDocument ToDocument(Publication p) => new()
    {
        Id = p.Id.ToString(),
        Title = p.Title,
        Abstract = p.Abstract ?? string.Empty,
        Body = p.Body ?? string.Empty,
        Authors = p.Authors
            .Select(a => string.IsNullOrWhiteSpace(a.MiddleName)
                ? $"{a.FirstName} {a.LastName}".Trim()
                : $"{a.FirstName} {a.MiddleName} {a.LastName}".Trim())
            .ToArray(),
        Keywords = p.Keywords.Select(k => k.Value).ToArray(),
        Languages = p.Languages.Select(l => l.Value).ToArray(),
        PublicationTypes = p.PublicationTypes.Select(pt => pt.Value).ToArray(),
        Year = p.Year ?? 0,
        Doi = p.DOI ?? string.Empty,
        PdfFileName = p.PdfFileName ?? string.Empty,
        LastModifiedTimestamp = new DateTimeOffset(p.LastModified).ToUnixTimeSeconds()
    };
}
