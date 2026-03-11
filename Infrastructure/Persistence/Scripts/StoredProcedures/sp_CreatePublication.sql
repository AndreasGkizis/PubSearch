USE ResearchPublications;
GO

-- Helper type for passing author lists
-- Drop and re-create so the procedure can be updated cleanly
IF TYPE_ID('dbo.AuthorTableType') IS NULL
BEGIN
    CREATE TYPE dbo.AuthorTableType AS TABLE (
        FullName NVARCHAR(200) NOT NULL,
        Email    NVARCHAR(200) NULL
    );
END
GO

CREATE OR ALTER PROCEDURE sp_CreatePublication
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
        -- Insert publication
        INSERT INTO Publications (Title, Abstract, Body, Keywords, Year, DOI, CitationCount, PdfFileName)
        VALUES (@Title, @Abstract, @Body, @Keywords, @Year, @DOI, @CitationCount, @PdfFileName);

        DECLARE @NewId INT = SCOPE_IDENTITY();

        -- Upsert authors and link them
        DECLARE @FullName NVARCHAR(200), @Email NVARCHAR(200), @AuthorId INT;

        DECLARE authorCursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT FullName, Email FROM @Authors;

        OPEN authorCursor;
        FETCH NEXT FROM authorCursor INTO @FullName, @Email;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Find existing author by name or insert
            SELECT @AuthorId = Id FROM Authors WHERE FullName = @FullName;

            IF @AuthorId IS NULL
            BEGIN
                INSERT INTO Authors (FullName, Email) VALUES (@FullName, @Email);
                SET @AuthorId = SCOPE_IDENTITY();
            END

            -- Link to publication (ignore duplicate)
            IF NOT EXISTS (SELECT 1 FROM PublicationAuthors WHERE PublicationId = @NewId AND AuthorId = @AuthorId)
                INSERT INTO PublicationAuthors (PublicationId, AuthorId) VALUES (@NewId, @AuthorId);

            SET @AuthorId = NULL;
            FETCH NEXT FROM authorCursor INTO @FullName, @Email;
        END

        CLOSE authorCursor;
        DEALLOCATE authorCursor;

        COMMIT TRANSACTION;

        SELECT @NewId AS Id;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
