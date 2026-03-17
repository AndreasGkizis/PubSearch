namespace ResearchPublications.Infrastructure.Settings;

public class SqlSettings
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
    public string DbName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string FormattedConnectionString =>
        $"Server={Server},{Port};Database={DbName};User Id={UserId};Password={Password};TrustServerCertificate=True";
}
