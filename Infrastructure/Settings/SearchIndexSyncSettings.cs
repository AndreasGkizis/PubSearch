namespace ResearchPublications.Infrastructure.Settings;

public sealed class SearchIndexSyncSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 5;
}
