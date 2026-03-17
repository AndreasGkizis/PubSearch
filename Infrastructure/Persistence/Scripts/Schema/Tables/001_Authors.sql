IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Authors')
BEGIN
    CREATE TABLE dbo.Authors (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName  NVARCHAR(100) NOT NULL,
        Email     NVARCHAR(200) NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
