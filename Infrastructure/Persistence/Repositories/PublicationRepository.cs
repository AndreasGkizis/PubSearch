using Dapper;
using Microsoft.Data.SqlClient;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.Infrastructure.Constants;
using ResearchPublications.Infrastructure.Persistence;
using System.Data;

namespace ResearchPublications.Infrastructure.Persistence.Repositories;

public class PublicationRepository(DapperContext context) : IPublicationRepository
{
    public async Task<Publication?> GetByIdAsync(int id)
    {
        using var conn = context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            StoredProcedures.GetPublicationById,
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        var pub = await multi.ReadFirstOrDefaultAsync<Publication>();
        if (pub is null) return null;

        var authors = (await multi.ReadAsync<Author>()).ToList();
        pub.Authors = authors;
        return pub;
    }

    public async Task<(IEnumerable<Publication> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        using var conn = context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            StoredProcedures.GetAllPublications,
            new { Page = page, PageSize = pageSize },
            commandType: CommandType.StoredProcedure);

        var totalCount = await multi.ReadFirstAsync<int>();
        var rows = (await multi.ReadAsync<PublicationRow>()).ToList();

        var publications = rows.Select(r => new Publication
        {
            Id = r.Id,
            Title = r.Title,
            Abstract = r.Abstract,
            Body = r.Body,
            Keywords = r.Keywords,
            Year = r.Year,
            DOI = r.DOI,
            CitationCount = r.CitationCount,
            PdfFileName = r.PdfFileName,
            CreatedAt = r.CreatedAt,
            LastModified = r.LastModified,
            Authors = string.IsNullOrWhiteSpace(r.AuthorNames)
                ? []
                : r.AuthorNames.Split(", ").Select(n => new Author { FullName = n }).ToList()
        });

        return (publications, totalCount);
    }

    public async Task<int> CreateAsync(Publication publication)
    {
        using var conn = (SqlConnection)context.CreateConnection();
        await conn.OpenAsync();

        var authorsTable = BuildAuthorTable(publication.Authors);

        var result = await conn.QueryFirstAsync<int>(
            StoredProcedures.CreatePublication,
            new
            {
                publication.Title,
                publication.Abstract,
                publication.Body,
                publication.Keywords,
                publication.Year,
                publication.DOI,
                publication.CitationCount,
                publication.PdfFileName,
                Authors = authorsTable.AsTableValuedParameter(TableTypes.AuthorTableType)
            },
            commandType: CommandType.StoredProcedure);

        return result;
    }

    public async Task UpdateAsync(Publication publication)
    {
        using var conn = (SqlConnection)context.CreateConnection();
        await conn.OpenAsync();

        var authorsTable = BuildAuthorTable(publication.Authors);

        await conn.ExecuteAsync(
            StoredProcedures.UpdatePublication,
            new
            {
                publication.Id,
                publication.Title,
                publication.Abstract,
                publication.Body,
                publication.Keywords,
                publication.Year,
                publication.DOI,
                publication.CitationCount,
                publication.PdfFileName,
                Authors = authorsTable.AsTableValuedParameter(TableTypes.AuthorTableType)
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = context.CreateConnection();
        await conn.ExecuteAsync(
            StoredProcedures.DeletePublication,
            new { Id = id },
            commandType: CommandType.StoredProcedure);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static DataTable BuildAuthorTable(IReadOnlyList<Author> authors)
    {
        var table = new DataTable();
        table.Columns.Add("FullName", typeof(string));
        table.Columns.Add("Email",    typeof(string));

        foreach (var author in authors)
            table.Rows.Add(author.FullName, author.Email as object ?? DBNull.Value);

        return table;
    }

    // Projection for the denormalised GetAll query
    private sealed class PublicationRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Abstract { get; init; }
        public string? Body { get; init; }
        public string? Keywords { get; init; }
        public int? Year { get; init; }
        public string? DOI { get; init; }
        public int CitationCount { get; init; }
        public string? PdfFileName { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastModified { get; init; }
        public string? AuthorNames { get; init; }
    }
}
