using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ResearchPublications.Infrastructure.Persistence;

public class DbSeeder(AppDbCntx context, ILogger<DbSeeder> logger)
{
    public async Task SeedAsync()
    {
        if (await context.Publications.AnyAsync())
        {
            logger.LogInformation("Seed skipped — data already exists.");
            return;
        }

        // Add seed data here when the database is ready.
        logger.LogInformation("No seed data defined. Skipping.");
    }
}
