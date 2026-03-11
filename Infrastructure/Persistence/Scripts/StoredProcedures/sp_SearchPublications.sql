USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_SearchPublications
    @Query   NVARCHAR(500),
    @Year    INT            = NULL,
    @Author  NVARCHAR(200)  = NULL,
    @Keyword NVARCHAR(200)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Sanitise query: wrap each word in double-quotes for CONTAINSTABLE
    DECLARE @FtsQuery NVARCHAR(600) = '"' + REPLACE(LTRIM(RTRIM(@Query)), ' ', '" OR "') + '"';

    -- ── Attempt FTS search ────────────────────────────────────────────────
    IF (SELECT COUNT(*) FROM CONTAINSTABLE(Publications, (Title, Abstract, Keywords, Body), @FtsQuery)) > 0
    BEGIN
        SELECT
            p.Id,
            p.Title,
            p.Abstract,
            p.Keywords,
            p.Year,
            p.PdfFileName,
            ct.[RANK]                                   AS Rank,
            STUFF((
                SELECT ', ' + a.FullName
                FROM PublicationAuthors pa2
                JOIN Authors a ON pa2.AuthorId = a.Id
                WHERE pa2.PublicationId = p.Id
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'), 1, 2, '')    AS AuthorNames
        FROM Publications p
        INNER JOIN CONTAINSTABLE(Publications, (Title, Abstract, Keywords, Body), @FtsQuery) ct
            ON p.Id = ct.[KEY]
        LEFT JOIN PublicationAuthors pa ON pa.PublicationId = p.Id
        LEFT JOIN Authors a             ON a.Id = pa.AuthorId
        WHERE
            (@Year    IS NULL OR p.Year = @Year)
            AND (@Keyword IS NULL OR p.Keywords LIKE '%' + @Keyword + '%')
            AND (@Author  IS NULL OR EXISTS (
                    SELECT 1 FROM PublicationAuthors pa3
                    JOIN Authors a3 ON pa3.AuthorId = a3.Id
                    WHERE pa3.PublicationId = p.Id AND a3.FullName LIKE '%' + @Author + '%'))
        GROUP BY p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName, ct.[RANK]
        ORDER BY ct.[RANK] DESC;

        RETURN;
    END

    -- ── LIKE fallback (no FTS match or FTS not configured) ─────────────────
    SELECT
        p.Id,
        p.Title,
        p.Abstract,
        p.Keywords,
        p.Year,
        p.PdfFileName,
        0.0                                             AS Rank,
        STUFF((
            SELECT ', ' + a.FullName
            FROM PublicationAuthors pa2
            JOIN Authors a ON pa2.AuthorId = a.Id
            WHERE pa2.PublicationId = p.Id
            FOR XML PATH(''), TYPE
        ).value('.', 'NVARCHAR(MAX)'), 1, 2, '')        AS AuthorNames
    FROM Publications p
    LEFT JOIN PublicationAuthors pa ON pa.PublicationId = p.Id
    LEFT JOIN Authors a             ON a.Id = pa.AuthorId
    WHERE
        (  p.Title    LIKE '%' + @Query + '%'
        OR p.Abstract LIKE '%' + @Query + '%'
        OR p.Keywords LIKE '%' + @Query + '%'
        OR p.Body     LIKE '%' + @Query + '%')
        AND (@Year    IS NULL OR p.Year = @Year)
        AND (@Keyword IS NULL OR p.Keywords LIKE '%' + @Keyword + '%')
        AND (@Author  IS NULL OR EXISTS (
                SELECT 1 FROM PublicationAuthors pa3
                JOIN Authors a3 ON pa3.AuthorId = a3.Id
                WHERE pa3.PublicationId = p.Id AND a3.FullName LIKE '%' + @Author + '%'))
    GROUP BY p.Id, p.Title, p.Abstract, p.Keywords, p.Year, p.PdfFileName
    ORDER BY p.Id DESC;
END
GO
