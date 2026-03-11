USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_GetAllKeywords
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT LTRIM(RTRIM(value)) AS Keyword
    FROM Publications
    CROSS APPLY STRING_SPLIT(Keywords, ',')
    WHERE Keywords IS NOT NULL AND LTRIM(RTRIM(value)) != ''
    ORDER BY Keyword;
END
GO
