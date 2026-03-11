using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ResearchPublications.Infrastructure.Constants;
using System.Data;

namespace ResearchPublications.Infrastructure.Persistence;

public class DapperContext(IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString(ConfigKeys.DefaultConnection)
        ?? throw new InvalidOperationException($"Connection string '{ConfigKeys.DefaultConnection}' is not configured.");

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
