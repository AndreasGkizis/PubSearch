USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_DeletePublication
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    -- PublicationAuthors rows are removed automatically by ON DELETE CASCADE
    DELETE FROM Publications WHERE Id = @Id;
END
GO
