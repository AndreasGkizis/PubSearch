IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PublicationAuthors')
CREATE TABLE PublicationAuthors (
    PublicationId INT NOT NULL REFERENCES Publications(Id) ON DELETE CASCADE,
    AuthorId      INT NOT NULL REFERENCES Authors(Id)      ON DELETE CASCADE,
    PRIMARY KEY (PublicationId, AuthorId)
);
