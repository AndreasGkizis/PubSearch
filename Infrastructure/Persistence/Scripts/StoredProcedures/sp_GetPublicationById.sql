USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_GetPublicationById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: publication row
    SELECT
        Id, Title, Abstract, Body, Keywords, Year, DOI,
        PdfFileName, CreatedAt, LastModified
    FROM Publications
    WHERE Id = @Id;

    -- Result set 2: authors for this publication
    SELECT a.Id, a.FullName, a.Email
    FROM Authors a
    JOIN PublicationAuthors pa ON pa.AuthorId = a.Id
    WHERE pa.PublicationId = @Id;
END
GO
