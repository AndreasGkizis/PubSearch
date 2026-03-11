using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResearchPublications.Infrastructure.Constants;
using System.Reflection;

namespace ResearchPublications.Infrastructure.Persistence;

/// <summary>
/// Runs on application start-up to ensure the database, schema, stored procedures
/// and initial seed data are all in place. All statements are idempotent.
/// SQL is loaded from embedded .sql files under Persistence/Scripts/.
/// </summary>
public class DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
{
    private static readonly Assembly _asm = Assembly.GetExecutingAssembly();

    // ── Public entry point ────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        var connectionString = configuration.GetConnectionString(ConfigKeys.DefaultConnection)
            ?? throw new InvalidOperationException($"Connection string '{ConfigKeys.DefaultConnection}' is not configured.");

        await EnsureDatabaseAsync(connectionString);

        var dbConnectionString = SwapDatabase(connectionString, ConfigKeys.DatabaseName);
        await EnsureSchemaAsync(dbConnectionString);
        await EnsureStoredProceduresAsync(dbConnectionString);
        await SeedIfEmptyAsync(dbConnectionString);

        logger.LogInformation("Database initialisation complete.");
    }

    // ── Steps ─────────────────────────────────────────────────────────────

    private async Task EnsureDatabaseAsync(string connectionString)
    {
        var masterCs = SwapDatabase(connectionString, "master");
        await using var conn = new SqlConnection(masterCs);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{ConfigKeys.DatabaseName}')
                CREATE DATABASE [{ConfigKeys.DatabaseName}];
            """;
        await cmd.ExecuteNonQueryAsync();
        logger.LogInformation("Database '{Database}' verified.", ConfigKeys.DatabaseName);
    }

    private async Task EnsureSchemaAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        foreach (var name in new[]
        {
            "001_AuthorTableType",
            "002_Authors",
            "003_Publications",
            "004_PublicationAuthors"
        })
        {
            await ExecuteSqlResourceAsync(conn, $"Schema.{name}.sql");
        }

        try
        {
            await ExecuteSqlResourceAsync(conn, "Schema.005_FullTextIndex.sql");
            logger.LogInformation("Full-text search index verified.");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 7609 || ex.Number == 9940)
        {
            logger.LogWarning("Full-Text Search is not available — search will fall back to LIKE queries.");
        }

        logger.LogInformation("Schema verified.");
    }

    private async Task EnsureStoredProceduresAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        foreach (var sp in new[]
        {
            "sp_GetPublicationById",
            "sp_GetAllPublications",
            "sp_CreatePublication",
            "sp_UpdatePublication",
            "sp_DeletePublication",
            "sp_SearchPublications",
            "sp_GetAllAuthors",
            "sp_GetAllKeywords"
        })
        {
            await ExecuteSqlResourceAsync(conn, $"StoredProcedures.{sp}.sql");
        }

        logger.LogInformation("Stored procedures verified.");
    }

    private async Task SeedIfEmptyAsync(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Publications;";
        var count = (int)(await checkCmd.ExecuteScalarAsync())!;

        if (count > 0)
        {
            logger.LogInformation("Seed skipped — {Count} publication(s) already exist.", count);
            return;
        }

        await ExecuteSqlResourceAsync(conn, "Seed.SeedData.sql");
        logger.LogInformation("Seed data inserted.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads an embedded SQL resource by its short name (relative to the Scripts folder),
    /// strips SSMS-only directives (USE … / GO), splits on GO batch separators,
    /// and executes each batch in sequence.
    /// </summary>
    private static async Task ExecuteSqlResourceAsync(SqlConnection conn, string relName)
    {
        var fullName = $"ResearchPublications.Infrastructure.Persistence.Scripts.{relName}";
        await using var stream = _asm.GetManifestResourceStream(fullName)
            ?? throw new InvalidOperationException($"Embedded SQL resource not found: '{fullName}'");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync();

        // Split on GO batch separators (SSMS convention); skip USE … lines.
        var batches = sql
            .Split(["\nGO", "\r\nGO"], StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0 && !b.StartsWith("USE ", StringComparison.OrdinalIgnoreCase));

        foreach (var batch in batches)
            await ExecuteBatchAsync(conn, batch);
    }

    private static async Task ExecuteBatchAsync(SqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Replaces the Initial Catalog in the connection string.</summary>
    private static string SwapDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }
}
