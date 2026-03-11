IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'PublicationsCatalog')
    CREATE FULLTEXT CATALOG PublicationsCatalog AS DEFAULT;

IF NOT EXISTS (
    SELECT 1 FROM sys.fulltext_indexes fi
    JOIN sys.objects o ON fi.object_id = o.object_id
    WHERE o.name = 'Publications')
BEGIN
    DECLARE @pkName NVARCHAR(200) =
        (SELECT name FROM sys.indexes
         WHERE object_id = OBJECT_ID('Publications') AND is_primary_key = 1);

    EXEC('CREATE FULLTEXT INDEX ON Publications(Title, Abstract, Keywords, Body)
          KEY INDEX [' + @pkName + '] ON PublicationsCatalog');
END
