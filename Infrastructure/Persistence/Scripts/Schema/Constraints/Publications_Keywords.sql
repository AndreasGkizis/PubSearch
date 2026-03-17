IF OBJECT_ID('dbo.PublicationKeywords', 'U') IS NULL BEGIN
CREATE TABLE
    dbo.PublicationKeywords (
        PublicationId INT NOT NULL,
        KeywordId INT NOT NULL,
        PRIMARY KEY (PublicationId, KeywordId),
        FOREIGN KEY (PublicationId) REFERENCES dbo.Publications (Id) ON DELETE CASCADE,
        FOREIGN KEY (KeywordId) REFERENCES dbo.Keywords (Id) ON DELETE CASCADE
    );

END