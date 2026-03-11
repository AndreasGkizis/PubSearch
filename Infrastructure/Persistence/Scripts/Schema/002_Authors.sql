IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Authors')
CREATE TABLE Authors (
    Id       INT           PRIMARY KEY IDENTITY,
    FullName NVARCHAR(200) NOT NULL,
    Email    NVARCHAR(200) NULL
);
