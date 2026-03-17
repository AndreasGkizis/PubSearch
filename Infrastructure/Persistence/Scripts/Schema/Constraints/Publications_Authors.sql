IF OBJECT_ID('dbo.PublicationAuthors', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PublicationAuthors (
        PublicationId INT NOT NULL,
        AuthorId      INT NOT NULL,
        PRIMARY KEY (PublicationId, AuthorId),
        FOREIGN KEY (PublicationId) REFERENCES dbo.Publications(Id) ON DELETE CASCADE,
        FOREIGN KEY (AuthorId)      REFERENCES dbo.Authors(Id)      ON DELETE CASCADE
    );
END