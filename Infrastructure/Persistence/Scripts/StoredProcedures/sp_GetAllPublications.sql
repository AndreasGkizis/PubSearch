USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_GetAllPublications
    @Page     INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    -- Total count for pagination metadata
    SELECT COUNT(*) AS TotalCount FROM Publications;

    -- Paginated publication rows with aggregated author names
    SELECT
        p.Id,
        p.Title,
        p.Abstract,
        p.Body,
        p.Keywords,
        p.Year,
        p.DOI,
        p.CitationCount,
        p.PdfFileName,
        p.CreatedAt,
        p.LastModified,
        STUFF((
            SELECT ', ' + a.FullName
            FROM PublicationAuthors pa2
            JOIN Authors a ON pa2.AuthorId = a.Id
            WHERE pa2.PublicationId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AuthorNames
    FROM Publications p
    ORDER BY p.Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
