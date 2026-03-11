USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_SearchPublications
    @Query    NVARCHAR(500),
    @YearFrom INT           = NULL,
    @YearTo   INT           = NULL,
    @Authors  NVARCHAR(MAX) = NULL,
    @Keywords NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Use full-text search only when the FTS index exists on Publications.
    -- CONTAINSTABLE is placed inside dynamic SQL so SQL Server does not
    -- validate it at compile time when FTS is not installed.
    IF EXISTS (
        SELECT 1 FROM sys.fulltext_indexes fi
        JOIN sys.objects o ON fi.object_id = o.object_id
        WHERE o.name = 'Publications')
    BEGIN
        DECLARE @FtsQuery    NVARCHAR(600)  = '"' + REPLACE(LTRIM(RTRIM(@Query)), ' ', '" OR "') + '"';
        DECLARE @YearFromVal INT            = @YearFrom;
        DECLARE @YearToVal   INT            = @YearTo;
        DECLARE @KeywordsVal NVARCHAR(MAX)  = @Keywords;
        DECLARE @AuthorsVal  NVARCHAR(MAX)  = @Authors;

        DECLARE @sql NVARCHAR(MAX) = N'
            SELECT p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName,
                   ct.[RANK] AS Rank,
                   STUFF((
                       SELECT '', '' + a.FullName
                       FROM PublicationAuthors pa2
                       JOIN Authors a ON pa2.AuthorId = a.Id
                       WHERE pa2.PublicationId = p.Id
                       FOR XML PATH(''''), TYPE
                   ).value(''.'', ''NVARCHAR(MAX)''), 1, 2, '''') AS AuthorNames
            FROM Publications p
            INNER JOIN CONTAINSTABLE(Publications, (Title, Abstract, Keywords, Body), @fts) ct ON p.Id = ct.[KEY]
            WHERE (@yfrom IS NULL OR p.Year >= @yfrom)
              AND (@yto   IS NULL OR p.Year <= @yto)
              AND (@kws   IS NULL OR (
                    SELECT COUNT(*) FROM STRING_SPLIT(@kws, '','')
                    WHERE LTRIM(RTRIM(value)) != ''''
                      AND p.Keywords LIKE ''%'' + LTRIM(RTRIM(value)) + ''%''
                  ) = (SELECT COUNT(*) FROM STRING_SPLIT(@kws, '','') WHERE LTRIM(RTRIM(value)) != ''''))
              AND (@aus   IS NULL OR (
                    SELECT COUNT(DISTINCT LTRIM(RTRIM(s.value)))
                    FROM STRING_SPLIT(@aus, '','') s
                    WHERE LTRIM(RTRIM(s.value)) != ''''
                      AND EXISTS (
                          SELECT 1 FROM PublicationAuthors pa3
                          JOIN Authors a3 ON pa3.AuthorId = a3.Id
                          WHERE pa3.PublicationId = p.Id AND a3.FullName = LTRIM(RTRIM(s.value))
                      )
                  ) = (SELECT COUNT(DISTINCT LTRIM(RTRIM(value))) FROM STRING_SPLIT(@aus, '','') WHERE LTRIM(RTRIM(value)) != ''''))
            GROUP BY p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName, ct.[RANK]
            ORDER BY ct.[RANK] DESC;';

        EXEC sp_executesql @sql,
            N'@fts NVARCHAR(600), @yfrom INT, @yto INT, @kws NVARCHAR(MAX), @aus NVARCHAR(MAX)',
            @fts = @FtsQuery, @yfrom = @YearFromVal, @yto = @YearToVal,
            @kws = @KeywordsVal, @aus = @AuthorsVal;
        RETURN;
    END

    -- Fallback: LIKE-based search when FTS is not available
    SELECT
        p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName,
        0.0 AS Rank,
        STUFF((
            SELECT ', ' + a.FullName
            FROM PublicationAuthors pa2
            JOIN Authors a ON pa2.AuthorId = a.Id
            WHERE pa2.PublicationId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AuthorNames
    FROM Publications p
    WHERE (  p.Title    LIKE '%' + @Query + '%'
          OR p.Abstract LIKE '%' + @Query + '%'
          OR p.Keywords LIKE '%' + @Query + '%'
          OR p.Body     LIKE '%' + @Query + '%')
      AND (@YearFrom IS NULL OR p.Year >= @YearFrom)
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
                  SELECT 1 FROM PublicationAuthors pa3
                  JOIN Authors a3 ON pa3.AuthorId = a3.Id
                  WHERE pa3.PublicationId = p.Id AND a3.FullName = LTRIM(RTRIM(s.value))
              )
          ) = (SELECT COUNT(DISTINCT LTRIM(RTRIM(value))) FROM STRING_SPLIT(@Authors, ',') WHERE LTRIM(RTRIM(value)) != ''))
    GROUP BY p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName
    ORDER BY p.Id DESC;
END
GO
