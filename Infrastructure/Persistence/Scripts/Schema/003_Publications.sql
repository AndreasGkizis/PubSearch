IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Publications')
CREATE TABLE Publications (
    Id            INT            PRIMARY KEY IDENTITY,
    Title         NVARCHAR(500)  NOT NULL,
    Abstract      NVARCHAR(MAX)  NULL,
    Body          NVARCHAR(MAX)  NULL,
    Keywords      NVARCHAR(1000) NULL,
    Year          INT            NULL,
    DOI           NVARCHAR(200)  NULL,
    CitationCount INT            NOT NULL DEFAULT 0,
    PdfFileName   NVARCHAR(500)  NULL,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    LastModified  DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
