using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ResearchPublications.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time to create a <see cref="AppDbCntx"/> instance
/// without needing the full host to run. Run from the repo root:
/// <code>dotnet ef migrations add &lt;Name&gt; --project Infrastructure --startup-project API --output-dir Persistence/Migrations</code>
/// All migrations must live in <c>Infrastructure/Persistence/Migrations/</c> — always include the --output-dir flag.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbCntx>
{
    public AppDbCntx CreateDbContext(string[] args)
    {
        // Override with EFCORE_CONNECTION_STRING env var, or fall back to the local dev default.
        var connectionString =
            Environment.GetEnvironmentVariable("EFCORE_CONNECTION_STRING")
            ?? "Server=localhost,1433;Database=ResearchPublications;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbCntx>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbCntx(optionsBuilder.Options);
    }
}
