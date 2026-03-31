USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_GetAllPublications
    @Page     INT           = 1,
    @PageSize INT           = 20,
    @YearFrom INT           = NULL,
    @YearTo   INT           = NULL,
    @Authors  NVARCHAR(MAX) = NULL,
    @Keywords NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    -- Build filtered ID set once; reused for both COUNT and data queries.
    SELECT p.Id
    INTO #FilteredIds
    FROM Publications p
    WHERE (@YearFrom IS NULL OR p.Year >= @YearFrom)
      AND (@YearTo   IS NULL OR p.Year <= @YearTo)
      AND (@Keywords IS NULL OR (
            SELECT COUNT(*) FROM STRING_SPLIT(@Keywords, ',')
            WHERE LTRIM(RTRIM(value)) != ''
              AND p.Keywords LIKE '%' + LTRIM(RTRIM(value)) + '%'
          ) = (SELECT COUNT(*) FROM STRING_SPLIT(@Keywords, ',') WHERE LTRIM(RTRIM(value)) != ''))
      AND (@Authors IS NULL OR (
            SELECT COUNT(DISTINCT LTRIM(RTRIM(s.value)))
            FROM STRING_SPLIT(@Authors, ',') s
            WHERE LTRIM(RTRIM(s.value)) != ''
              AND EXISTS (
                  SELECT 1 FROM PublicationAuthors pa4
                  JOIN Authors a4 ON pa4.AuthorId = a4.Id
                  WHERE pa4.PublicationId = p.Id AND a4.FullName = LTRIM(RTRIM(s.value))
              )
          ) = (SELECT COUNT(DISTINCT LTRIM(RTRIM(value))) FROM STRING_SPLIT(@Authors, ',') WHERE LTRIM(RTRIM(value)) != ''));

    SELECT COUNT(*) AS TotalCount FROM #FilteredIds;

    SELECT
        p.Id, p.Title, p.Abstract, p.Body, p.Keywords, p.Year, p.DOI,
        p.PdfFileName, p.CreatedAt, p.LastModified,
        STUFF((
            SELECT ', ' + a.FullName
            FROM PublicationAuthors pa2
            JOIN Authors a ON pa2.AuthorId = a.Id
            WHERE pa2.PublicationId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AuthorNames
    FROM Publications p
    INNER JOIN #FilteredIds fi ON p.Id = fi.Id
    ORDER BY p.Id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    DROP TABLE #FilteredIds;
END
GO
