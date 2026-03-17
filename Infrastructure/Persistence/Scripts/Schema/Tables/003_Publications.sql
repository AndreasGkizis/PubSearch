IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Publications')
BEGIN
    CREATE TABLE dbo.Publications (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title       NVARCHAR(500) NOT NULL,
        Abstract    NVARCHAR(MAX) NULL,
        Body        NVARCHAR(MAX) NULL,
        [Year]      INT NULL,
        DOI         NVARCHAR(100) NULL,
        PdfFileName NVARCHAR(255) NULL,
        PdfPath     NVARCHAR(255) NULL,
        CreatedAt   DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt   DATETIME2 DEFAULT GETUTCDATE()
    );
END