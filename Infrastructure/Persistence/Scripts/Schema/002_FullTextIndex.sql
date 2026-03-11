USE ResearchPublications;
GO

-- Full-text catalog
IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'PublicationsCatalog')
BEGIN
    CREATE FULLTEXT CATALOG PublicationsCatalog AS DEFAULT;
END
GO

-- Full-text index on Publications
-- Requires the table's PRIMARY KEY index 'PK__Publicat...' or any unique, single-column, non-nullable index.
IF NOT EXISTS (
    SELECT * FROM sys.fulltext_indexes fi
    JOIN sys.objects o ON fi.object_id = o.object_id
    WHERE o.name = 'Publications'
)
BEGIN
    CREATE FULLTEXT INDEX ON Publications(Title, Abstract, Keywords, Body)
        KEY INDEX PK__Publicat__3214EC070000000000000000  -- replaced at runtime; use sp_help to get real name
        ON PublicationsCatalog;
END
GO

-- NOTE: The KEY INDEX clause must reference the actual PRIMARY KEY constraint name.
-- Run: SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('Publications') AND is_primary_key = 1
-- Then replace the KEY INDEX value above, or use the dynamic version below:

-- Dynamic version (run if the static name above fails):
/*
DECLARE @pkName NVARCHAR(200) = (
    SELECT name FROM sys.indexes
    WHERE object_id = OBJECT_ID('Publications') AND is_primary_key = 1
);
DECLARE @sql NVARCHAR(MAX) = N'
CREATE FULLTEXT INDEX ON Publications(Title, Abstract, Keywords, Body)
    KEY INDEX ' + QUOTENAME(@pkName) + N'
    ON PublicationsCatalog;';
EXEC sp_executesql @sql;
*/
