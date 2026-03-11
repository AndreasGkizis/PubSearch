namespace ResearchPublications.Infrastructure.Constants;

public static class StoredProcedures
{
    public const string GetPublicationById  = "sp_GetPublicationById";
    public const string GetAllPublications  = "sp_GetAllPublications";
    public const string CreatePublication   = "sp_CreatePublication";
    public const string UpdatePublication   = "sp_UpdatePublication";
    public const string DeletePublication   = "sp_DeletePublication";
    public const string SearchPublications  = "sp_SearchPublications";
    public const string GetAllAuthors       = "sp_GetAllAuthors";
    public const string GetAllKeywords      = "sp_GetAllKeywords";
}

public static class TableTypes
{
    public const string AuthorTableType = "dbo.AuthorTableType";
}

public static class ConfigKeys
{
    public const string DatabaseName = "ResearchPublications";
    public const string DefaultConnection = "DefaultConnection";
    public const string PdfStoragePath    = "PdfStorage:Path";
}
