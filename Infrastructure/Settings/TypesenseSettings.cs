namespace ResearchPublications.Infrastructure.Settings;

public class TypesenseSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8108;
    public string Protocol { get; set; } = "http";
    public string ApiKey { get; set; } = string.Empty;
}
