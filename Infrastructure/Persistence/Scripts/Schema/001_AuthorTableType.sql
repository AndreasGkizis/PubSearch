IF TYPE_ID('dbo.AuthorTableType') IS NULL
BEGIN
    CREATE TYPE dbo.AuthorTableType AS TABLE (
        FullName NVARCHAR(200) NOT NULL,
        Email    NVARCHAR(200) NULL
    );
END
