USE ResearchPublications;
GO

CREATE OR ALTER PROCEDURE sp_UpdatePublication
    @Id           INT,
    @Title        NVARCHAR(500),
    @Abstract     NVARCHAR(MAX)  = NULL,
    @Body         NVARCHAR(MAX)  = NULL,
    @Keywords     NVARCHAR(1000) = NULL,
    @Year         INT            = NULL,
    @DOI          NVARCHAR(200)  = NULL,
    @CitationCount INT           = 0,
    @PdfFileName  NVARCHAR(500)  = NULL,
    @Authors      dbo.AuthorTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        UPDATE Publications
        SET
            Title         = @Title,
            Abstract      = @Abstract,
            Body          = @Body,
            Keywords      = @Keywords,
            Year          = @Year,
            DOI           = @DOI,
            CitationCount = @CitationCount,
            PdfFileName   = @PdfFileName,
            LastModified  = GETUTCDATE()
        WHERE Id = @Id;

        -- Re-sync authors: remove existing links then re-insert
        DELETE FROM PublicationAuthors WHERE PublicationId = @Id;

        DECLARE @FullName NVARCHAR(200), @Email NVARCHAR(200), @AuthorId INT;

        DECLARE authorCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT FullName, Email FROM @Authors;

        OPEN authorCursor;
        FETCH NEXT FROM authorCursor INTO @FullName, @Email;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @AuthorId = Id FROM Authors WHERE FullName = @FullName;

            IF @AuthorId IS NULL
            BEGIN
                INSERT INTO Authors (FullName, Email) VALUES (@FullName, @Email);
                SET @AuthorId = SCOPE_IDENTITY();
            END

            IF NOT EXISTS (SELECT 1 FROM PublicationAuthors WHERE PublicationId = @Id AND AuthorId = @AuthorId)
                INSERT INTO PublicationAuthors (PublicationId, AuthorId) VALUES (@Id, @AuthorId);

            SET @AuthorId = NULL;
            FETCH NEXT FROM authorCursor INTO @FullName, @Email;
        END

        CLOSE authorCursor;
        DEALLOCATE authorCursor;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
