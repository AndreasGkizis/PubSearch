-- Run this script on the target SQL Server instance.
-- Creates the ResearchPublications database and all tables.

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ResearchPublications')
BEGIN
    CREATE DATABASE ResearchPublications;
END
GO

USE ResearchPublications;
GO

CREATE TABLE Authors (
    Id       INT           PRIMARY KEY IDENTITY,
    FullName NVARCHAR(200) NOT NULL,
    Email    NVARCHAR(200) NULL
);
GO

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
GO

CREATE TABLE PublicationAuthors (
    PublicationId INT NOT NULL REFERENCES Publications(Id) ON DELETE CASCADE,
    AuthorId      INT NOT NULL REFERENCES Authors(Id)      ON DELETE CASCADE,
    PRIMARY KEY (PublicationId, AuthorId)
);
GO
